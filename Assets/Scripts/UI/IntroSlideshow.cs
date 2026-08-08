using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Persuasion.UI
{
    /// <summary>
    /// 오프닝(인트로) 배경을 여러 장으로 부드럽게 크로스페이드하는 슬라이드쇼.
    /// layerA(하단) = 현재 표시, layerB(상단) = 전환용 오버레이. 은은한 켄번즈 줌 포함.
    /// 인트로 패널이 활성화될 때 자동 시작/정지.
    /// </summary>
    public class IntroSlideshow : MonoBehaviour
    {
        [SerializeField] private Image layerA;
        [SerializeField] private Image layerB;
        [SerializeField] private Sprite[] slides;
        [SerializeField] private float holdSeconds = 4.5f;
        [SerializeField] private float fadeSeconds = 1.8f;
        [SerializeField] private float zoomAmount  = 0.07f;   // 슬라이드당 서서히 확대되는 정도

        private Coroutine _loop;

        private void OnEnable()
        {
            if (layerA == null || slides == null || slides.Length == 0) return;

            layerA.sprite  = slides[0];
            layerA.enabled = true;
            SetAlpha(layerA, 1f);
            ResetZoom(layerA);
            if (layerB != null) { SetAlpha(layerB, 0f); ResetZoom(layerB); }

            if (_loop != null) StopCoroutine(_loop);
            _loop = StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            if (_loop != null) { StopCoroutine(_loop); _loop = null; }
        }

        private IEnumerator Loop()
        {
            // 한 장뿐이면 줌만 반복
            if (slides.Length < 2 || layerB == null)
            {
                while (true)
                {
                    float t0 = 0f;
                    ResetZoom(layerA);
                    while (t0 < holdSeconds + fadeSeconds)
                    {
                        t0 += Time.unscaledDeltaTime;
                        ApplyZoom(layerA, t0 / (holdSeconds + fadeSeconds));
                        yield return null;
                    }
                }
            }

            int idx = 0;
            float life = holdSeconds + fadeSeconds;
            while (true)
            {
                // 1) 현재 장 유지 + 서서히 확대
                float t = 0f;
                ResetZoom(layerA);
                while (t < holdSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    ApplyZoom(layerA, t / life);
                    yield return null;
                }

                // 2) 다음 장을 상단 레이어(B)에 올리고 페이드 인
                idx = (idx + 1) % slides.Length;
                layerB.sprite = slides[idx];
                layerB.enabled = true;
                SetAlpha(layerB, 0f);
                ResetZoom(layerB);

                float f = 0f;
                while (f < fadeSeconds)
                {
                    f += Time.unscaledDeltaTime;
                    SetAlpha(layerB, Mathf.Clamp01(f / fadeSeconds));
                    ApplyZoom(layerA, (holdSeconds + f) / life);
                    yield return null;
                }
                SetAlpha(layerB, 1f);

                // 3) 하단(A)으로 확정 이관 후 상단(B) 숨김 (같은 장이라 이음새 없음)
                layerA.sprite = slides[idx];
                ResetZoom(layerA);
                SetAlpha(layerA, 1f);
                SetAlpha(layerB, 0f);
            }
        }

        private void ApplyZoom(Image img, float p)
        {
            if (img == null) return;
            float s = 1f + zoomAmount * Mathf.Clamp01(p);
            img.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        private void ResetZoom(Image img)
        {
            if (img != null) img.rectTransform.localScale = Vector3.one;
        }

        private void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color; c.a = a; img.color = c;
        }
    }
}
