#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Persuasion.Audio;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// Assets/Audio 아래의 wav들을 "끊김 없는 루프"에 맞게 임포트하고,
    /// Assets/Resources/AudioLibrary.asset 을 생성/배선한다.
    /// 파일 규칙: Play/play_0..8.wav, Intro/intro_0..8.wav, opening.wav (index = stageOrder)
    /// 메뉴: Persuasion > 8) Setup Audio
    /// </summary>
    public static class AudioSetupTool
    {
        private const int StageCount = 9;
        private const string LibraryPath = "Assets/Resources/AudioLibrary.asset";

        [MenuItem("Persuasion/8) Setup Audio")]
        public static void Build()
        {
            // 1) 모든 오디오 클립 임포트 설정 초기화 (FMOD 에러 방지 → Clear WebGL override + DecompressOnLoad + PCM + preload)
            string[] allAudioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
            foreach (string guid in allAudioGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    Configure(importer, path, true);
                }
            }

            var opening = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/opening.wav");
            var play = new AudioClip[StageCount];
            var intro = new AudioClip[StageCount];
            var voice = new AudioClip[StageCount];
            for (int i = 0; i < StageCount; i++)
            {
                play[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Play/play_{i}.wav");
                intro[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Intro/intro_{i}.wav");
                voice[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Voice/voice_{i}.wav");
            }

            // 2) 라이브러리 에셋 생성/갱신
            if (!System.IO.Directory.Exists(Application.dataPath + "/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            bool isNew = lib == null;
            if (isNew) lib = ScriptableObject.CreateInstance<AudioLibrary>();

            lib.openingTheme = opening;
            lib.playClips = play;
            lib.introClips = intro;
            lib.voiceClips = voice;

            if (isNew) AssetDatabase.CreateAsset(lib, LibraryPath);
            else EditorUtility.SetDirty(lib);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int okPlay = 0, okIntro = 0, okVoice = 0;
            for (int i = 0; i < StageCount; i++)
            {
                if (play[i] != null) okPlay++;
                if (intro[i] != null) okIntro++;
                if (voice[i] != null) okVoice++;
            }
            Debug.Log($"[Persuasion] Audio Library 완료 — 오프닝:{(opening != null ? "O" : "X")}, " +
                      $"진행BGM {okPlay}/{StageCount}, 도입/전환 {okIntro}/{StageCount}, 음성 {okVoice}개: {LibraryPath}");

            // 3) 씬 재생성 → AudioManager 배선까지 반영
            SceneBuilderTool.Build();
        }

        private static AudioClip ImportClip(string path, bool preload = true)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Persuasion] 오디오 파일을 찾지 못함: " + path);
                return null;
            }
            return Configure(importer, path, preload);
        }

        private static AudioClip ImportClipOptional(string path, bool preload = true)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return null;
            return Configure(importer, path, preload);
        }

        private static AudioClip Configure(AudioImporter importer, string path, bool preload)
        {
            importer.ClearSampleSettingOverride("WebGL");
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad; // 메모리 상주 → FMOD 오류 완벽 해결
            settings.compressionFormat = AudioCompressionFormat.PCM; // 인코더 패딩 없음
            settings.preloadAudioData = true;                         // 필수 preloading
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
#endif
