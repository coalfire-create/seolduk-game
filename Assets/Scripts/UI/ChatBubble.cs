using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Persuasion.UI
{
    /// <summary>
    /// 채팅 말풍선 프리팹용 컴포넌트. playerBubblePrefab / npcBubblePrefab 둘 다 적용.
    /// 팝인 애니메이션(Ease Out Overshoot) / NPC 대기용 타이핑 인디케이터 / 한 글자씩 나오는 타자기 효과 지원.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ChatBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyText;

        private CanvasGroup _cg;
        private Coroutine _typing;
        private Coroutine _indicator;

        private CanvasGroup Group
        {
            get
            {
                if (_cg == null) _cg = GetComponent<CanvasGroup>();
                if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
                return _cg;
            }
        }

        /// <summary>즉시 텍스트 설정(애니메이션 없음). 진행 중인 타자기/인디케이터는 정리한다.</summary>
        public void SetText(string text)
        {
            StopAll();
            if (bodyText != null)
            {
                bodyText.maxVisibleCharacters = int.MaxValue;
                bodyText.text = text;
            }
        }

        /// <summary>말풍선이 스케일+페이드로 바운스있게 나타나는 팝인.</summary>
        public void PlayPopIn(float duration = 0.22f)
        {
            if (isActiveAndEnabled) StartCoroutine(PopInRoutine(duration));
        }

        private IEnumerator PopInRoutine(float duration)
        {
            var rt = (RectTransform)transform;
            var g = Group;
            g.alpha = 0f;
            rt.localScale = new Vector3(0.90f, 0.90f, 1f);

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                // Ease out back / overshoot
                float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.05f;
                g.alpha = Mathf.SmoothStep(0f, 1f, k);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            g.alpha = 1f;
            rt.localScale = Vector3.one;
        }

        /// <summary>NPC 응답 대기용 타이핑 인디케이터(“●” “● ●” “● ● ●”)를 반복 표시.</summary>
        public void StartTypingIndicator()
        {
            StopAll();
            if (isActiveAndEnabled) _indicator = StartCoroutine(IndicatorRoutine());
        }

        private IEnumerator IndicatorRoutine()
        {
            string[] frames = { "●", "●  ●", "●  ●  ●" };
            int i = 0;
            while (true)
            {
                if (bodyText != null) bodyText.text = frames[i % frames.Length];
                i++;
                yield return new WaitForSecondsRealtime(0.30f);
            }
        }

        /// <summary>
        /// 한 글자씩 나타나는 타자기 효과로 텍스트 출력.
        /// onStep은 글자마다 호출(스크롤 유지용), onDone은 완료 시 1회 호출.
        /// </summary>
        public void SetTextTyped(string full, float perChar, Action onStep, Action onDone)
        {
            StopAll();
            if (isActiveAndEnabled)
                _typing = StartCoroutine(TypeRoutine(full ?? string.Empty, perChar, onStep, onDone));
            else
            {
                SetText(full);
                onDone?.Invoke();
            }
        }

        private IEnumerator TypeRoutine(string full, float perChar, Action onStep, Action onDone)
        {
            if (bodyText == null) { onDone?.Invoke(); yield break; }

            // 전체 텍스트를 먼저 세팅해 말풍선 크기(높이)를 확정한 뒤, maxVisibleCharacters로 한 글자씩 공개.
            bodyText.text = full;
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;
            bodyText.maxVisibleCharacters = 0;

            int shown = 0;
            while (shown <= total)
            {
                bodyText.maxVisibleCharacters = shown;
                onStep?.Invoke();
                shown++;
                yield return new WaitForSecondsRealtime(perChar);
            }

            bodyText.maxVisibleCharacters = int.MaxValue;
            _typing = null;
            onDone?.Invoke();
        }

        private void StopAll()
        {
            if (_typing != null) { StopCoroutine(_typing); _typing = null; }
            if (_indicator != null) { StopCoroutine(_indicator); _indicator = null; }
            if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
        }
    }
}
