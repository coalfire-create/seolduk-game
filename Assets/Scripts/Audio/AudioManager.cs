using System.Collections;
using UnityEngine;
using Persuasion.Core;

namespace Persuasion.Audio
{
    /// <summary>
    /// 게임 진행 상황(GamePhase)에 맞춰 BGM을 재생하고 UI SFX를 처리한다.
    /// - 오프닝/스테이지 선택: 오프닝 테마
    /// - 도입컷/전환컷(StoryBeat): 해당 스테이지 도입 사운드
    /// - 진행(Playing): 해당 스테이지 BGM
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private PersuasionManager manager;
        [SerializeField] private AudioLibrary library;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.85f;
        [SerializeField] private float crossfadeSeconds = 0.7f;

        [Header("효과음 (SceneBuilderTool에서 배선)")]
        [SerializeField] private AudioClip typingSfx;     // 나래이션 타이핑(키보드) 소리 — 루프
        [SerializeField] private AudioClip carBrakeSfx;   // 1-1 도입 차 브레이크 원샷
        [SerializeField] private AudioClip persuasionUpSfx;    // 설득도 상승 원샷
        [SerializeField] private AudioClip persuasionDownSfx;  // 설득도 하락 원샷
        [SerializeField] private AudioClip clearStingerSfx;    // 스테이지 클리어 팡파레
        [SerializeField] private AudioClip failStingerSfx;     // 스테이지 실패 사운드
        [SerializeField] private AudioClip buttonClickSfx;     // UI 버튼 클릭 원샷

        private AudioSource _a;
        private AudioSource _b;
        private AudioSource _active;
        private AudioSource _voice;      // 캐릭터 음성 효과음 전용
        private AudioSource _sfx;        // UI 효과음 전용
        private AudioSource _typing;     // 나래이션 타이핑 루프 전용
        private AudioClip _current;
        private Coroutine _fade;

        // 채널별 사용자 볼륨(0~1). PlayerPrefs 저장.
        private float _musicMul = 1f, _sfxMul = 1f, _voiceMul = 1f;
        private const float SfxBase = 0.7f, TypingBase = 0.45f;
        public float MusicVolume01 => _musicMul;
        public float SfxVolume01   => _sfxMul;
        public float VoiceVolume01 => _voiceMul;
        private float EffectiveMusic => musicVolume * _musicMul;

        private void Awake()
        {
            Instance = this;
            if (library == null) library = Resources.Load<AudioLibrary>("AudioLibrary");

            _musicMul = PlayerPrefs.GetFloat("vol_music", 1f);
            _sfxMul   = PlayerPrefs.GetFloat("vol_sfx",   1f);
            _voiceMul = PlayerPrefs.GetFloat("vol_voice", 1f);

            _a = gameObject.AddComponent<AudioSource>();
            _b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { _a, _b })
            {
                s.loop = true;          // 끊김 없이 반복
                s.playOnAwake = false;
                s.volume = 0f;
                s.spatialBlend = 0f;    // 2D
                s.ignoreListenerPause = true;
            }
            _active = _a;

            _voice = gameObject.AddComponent<AudioSource>();
            _voice.loop = true;          // 대사가 길면 짧은 음성을 반복해 말하는 길이만큼 지속
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
            _voice.volume = voiceVolume * _voiceMul;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.loop = false;
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;
            _sfx.volume = SfxBase * _sfxMul;

            _typing = gameObject.AddComponent<AudioSource>();
            _typing.loop = true;              // 타이핑 소리를 글자 나오는 동안 반복
            _typing.playOnAwake = false;
            _typing.spatialBlend = 0f;
            _typing.volume = TypingBase * _sfxMul;
            _typing.ignoreListenerPause = true;
        }

        // ── 채널별 볼륨 조절(설정 UI) ──
        public void SetMusicVolume(float v)
        {
            _musicMul = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("vol_music", _musicMul);
            float eff = EffectiveMusic;
            // 크로스페이드 중에도 두 채널 모두 즉시 반영 (볼륨 감소 방향만)
            if (_a != null) _a.volume = Mathf.Min(_a.volume, eff);
            if (_b != null) _b.volume = Mathf.Min(_b.volume, eff);
            if (_active != null && _fade == null) _active.volume = eff;
        }
        public void SetSfxVolume(float v)
        {
            _sfxMul = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("vol_sfx", _sfxMul);
            if (_sfx != null)    _sfx.volume    = SfxBase * _sfxMul;
            if (_typing != null) _typing.volume = TypingBase * _sfxMul;
        }
        public void SetVoiceVolume(float v)
        {
            _voiceMul = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("vol_voice", _voiceMul);
            if (_voice != null) _voice.volume = voiceVolume * _voiceMul;
        }

        /// <summary>나래이션 타이핑(키보드) 소리 루프 시작. 글자가 한 자씩 나오는 동안 재생.</summary>
        public void StartTypingSound()
        {
            if (typingSfx == null || _typing == null) return;
            if (_typing.isPlaying && _typing.clip == typingSfx) return;
            _typing.clip = typingSfx;
            _typing.Play();
        }

        /// <summary>타이핑 소리 정지. 나래이션 출력 완료/스킵 시 호출.</summary>
        public void StopTypingSound()
        {
            if (_typing != null && _typing.isPlaying) _typing.Stop();
        }

        /// <summary>1-1 도입 등에서 차 브레이크 원샷 재생.</summary>
        public void PlayCarBrake()
        {
            if (carBrakeSfx != null && _sfx != null) _sfx.PlayOneShot(carBrakeSfx, 1f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>해당 스테이지 캐릭터의 음성 효과음을 재생(루프). 대사 출력 시작 시 호출.</summary>
        public void PlayCharacterVoice(int stageIndex)
        {
            if (library == null || _voice == null) return;
            var clip = library.GetVoice(stageIndex);
            if (clip == null) return;
            _voice.clip = clip;
            _voice.volume = voiceVolume * _voiceMul;
            _voice.Play();
        }

        /// <summary>음성 효과음 정지. 대사 출력 완료(또는 중단) 시 호출.</summary>
        public void StopCharacterVoice()
        {
            if (_voice != null)
            {
                _voice.Stop();
                _voice.clip = null;
            }
        }

        /// <summary>UI 버튼 클릭 / 호버 SFX 재생</summary>
        public void PlayButtonClick()
        {
            if (buttonClickSfx != null && _sfx != null) _sfx.PlayOneShot(buttonClickSfx, 0.9f);
        }

        /// <summary>설득도가 오른 순간 재생 (긍정 반응 원샷).</summary>
        public void PlayPersuasionUp()
        {
            if (persuasionUpSfx != null && _sfx != null) _sfx.PlayOneShot(persuasionUpSfx, 1f);
        }

        /// <summary>설득도가 내린 순간 재생 (부정 반응 원샷).</summary>
        public void PlayPersuasionDown()
        {
            if (persuasionDownSfx != null && _sfx != null) _sfx.PlayOneShot(persuasionDownSfx, 1f);
        }

        /// <summary>스테이지 클리어 순간 팡파레.</summary>
        public void PlayClearStinger()
        {
            if (clearStingerSfx != null && _sfx != null) _sfx.PlayOneShot(clearStingerSfx, 1f);
        }

        /// <summary>스테이지 실패 순간 사운드.</summary>
        public void PlayFailStinger()
        {
            if (failStingerSfx != null && _sfx != null) _sfx.PlayOneShot(failStingerSfx, 1f);
        }

        private void OnEnable()
        {
            if (manager != null) manager.OnPhaseChanged += HandlePhase;
        }

        private void OnDisable()
        {
            if (manager != null) manager.OnPhaseChanged -= HandlePhase;
        }

        private void Start()
        {
            HandlePhase(manager != null ? manager.CurrentPhase : GamePhase.Intro);
        }

        private void HandlePhase(GamePhase phase)
        {
            StopCharacterVoice();
            if (library == null || manager == null) return;

            AudioClip target = _current;
            switch (phase)
            {
                case GamePhase.Intro:
                case GamePhase.StageSelect:
                    target = library.openingTheme;
                    break;
                case GamePhase.StoryBeat:
                    target = library.GetIntro(manager.CurrentStageIndex) ?? library.openingTheme;
                    break;
                case GamePhase.Playing:
                    target = library.GetPlay(manager.CurrentStageIndex);
                    break;
                case GamePhase.GameCompleted:
                    target = library.openingTheme;
                    break;
            }
            PlayTrack(target);
        }

        private void PlayTrack(AudioClip clip)
        {
            if (clip == null || clip == _current) return;
            _current = clip;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(clip));
        }

        private IEnumerator Crossfade(AudioClip clip)
        {
            var from = _active;
            var to = (_active == _a) ? _b : _a;

            to.clip = clip;
            to.volume = 0f;
            to.Play();

            float dur = Mathf.Max(0.01f, crossfadeSeconds);
            float fromStart = from.volume;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                to.volume = Mathf.Lerp(0f, EffectiveMusic, k);
                from.volume = Mathf.Min(Mathf.Lerp(fromStart, 0f, k), EffectiveMusic);
                yield return null;
            }

            to.volume = EffectiveMusic;
            from.volume = 0f;
            from.Stop();
            from.clip = null;
            _active = to;
            _fade = null;
        }
        private void Update()
        {
            if (manager == null || _active == null || manager.CurrentPhase != GamePhase.Playing)
            {
                if (_active != null && _active.pitch != 1f)
                    _active.pitch = Mathf.MoveTowards(_active.pitch, 1f, Time.unscaledDeltaTime * 0.5f);
                return;
            }

            float targetPitch = 1.0f;
            
            // 1. 반전/고조 구간 (설득도 50% 이상)
            if (manager.Persuasion >= 50)
            {
                targetPitch = 1.15f;
            }
            
            // 2. 위기 구간 (연속 실패로 인한 게임 오버 직전)
            // GameData에서 허용하는 최대 실패 횟수에 근접했을 때 (예: 1회 남았을 때)
            var data = manager.Data;
            if (data != null)
            {
                bool isDanger = (manager.CurrentZeroStreak >= data.zeroStreakFailThreshold - 1) || 
                                (manager.CurrentNegativeStreak >= data.negativeStreakFailThreshold - 1);
                
                // 극도의 긴장감 (위기 구간이 반전 구간보다 우선)
                if (isDanger)
                {
                    targetPitch = 1.25f;
                }
            }
            
            // 피치 부드럽게 전환
            _active.pitch = Mathf.Lerp(_active.pitch, targetPitch, Time.unscaledDeltaTime * 1.5f);
        }
    }
}
