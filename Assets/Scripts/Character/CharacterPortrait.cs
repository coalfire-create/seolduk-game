using System;
using UnityEngine;
using UnityEngine.UI;
using Persuasion.AI;
using Persuasion.Core;

namespace Persuasion.Character
{
    /// <summary>
    /// 현재 스테이지의 캐릭터 초상화를 풀스크린 배경 Image에 표시하고,
    /// NPC의 감정 응답에 따라 표정 이미지를 실시간으로 교체한다.
    /// 이미지가 없는 스테이지에서는 배경 Image를 비활성화한다.
    /// </summary>
    public class CharacterPortrait : MonoBehaviour
    {
        [SerializeField] private PersuasionManager manager;
        [SerializeField] private StagePortraitLibrary library;
        [Tooltip("초상화를 그릴 풀스크린 배경 Image")]
        [SerializeField] private Image targetImage;

        private StagePortraitSet currentSet;
        private Sprite _lastEmotionSprite;
        private Coroutine _punchCoroutine;

        private void OnEnable()
        {
            if (manager != null)
            {
                manager.OnNPCReplied += HandleNPCReplied;
                manager.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.OnNPCReplied -= HandleNPCReplied;
                manager.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Playing || phase == GamePhase.StoryBeat)
                LoadStage();
        }

        private void LoadStage()
        {
            var stage = manager != null ? manager.CurrentStage : null;
            currentSet = (stage != null && library != null) ? library.Find(stage.stageId) : null;
            Apply(currentSet != null ? currentSet.baseSprite : null);
        }

        private void HandleNPCReplied(NPCResponse response)
        {
            EmotionState emotion;
            if (!Enum.TryParse(response.emotion, true, out emotion))
                emotion = EmotionState.Confused;

            var newSprite = currentSet != null ? currentSet.Get(emotion) : null;
            bool emotionActuallyChanged = newSprite != null && _lastEmotionSprite != null && newSprite != _lastEmotionSprite;

            Apply(newSprite);
            _lastEmotionSprite = newSprite;

            // 표정이 실제로 바뀐 순간에만 살짝 반응하는 느낌을 준다 (풀스크린 배경이라 항상 1.0 이상으로만 확대)
            if (emotionActuallyChanged && targetImage != null)
            {
                bool sharpReaction = emotion == EmotionState.Anger || emotion == EmotionState.Disgust
                                   || emotion == EmotionState.Surprise || emotion == EmotionState.Bewildered;
                if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
                _punchCoroutine = StartCoroutine(PunchPortrait(targetImage.rectTransform, sharpReaction));
            }
        }

        private System.Collections.IEnumerator PunchPortrait(RectTransform rt, bool sharp)
        {
            if (rt == null) yield break;
            Vector3 origScale = rt.localScale;
            Vector3 origPos   = rt.localPosition;
            float dur = sharp ? 0.22f : 0.32f;
            float maxScale = sharp ? 1.045f : 1.025f;
            float shakeMag = sharp ? 5f : 0f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float wave = Mathf.Sin(k * Mathf.PI); // 0 → 1 → 0
                float s = 1f + wave * (maxScale - 1f);
                rt.localScale = origScale * s;
                if (shakeMag > 0f)
                {
                    float decay = 1f - k;
                    rt.localPosition = origPos + new Vector3(UnityEngine.Random.Range(-shakeMag, shakeMag) * decay, 0f, 0f);
                }
                yield return null;
            }
            rt.localScale = origScale;
            rt.localPosition = origPos;
            _punchCoroutine = null;
        }

        private void Apply(Sprite sprite)
        {
            if (targetImage == null) return;
            targetImage.sprite = sprite;
            targetImage.enabled = (sprite != null);

            if (sprite != null)
            {
                var fitter = targetImage.GetComponent<AspectRatioFitter>();
                if (fitter != null && sprite.rect.height > 0f)
                    fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
        }
    }
}
