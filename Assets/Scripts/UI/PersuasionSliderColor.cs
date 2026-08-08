using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Persuasion.UI
{
    /// <summary>
    /// 설득도 슬라이더 값에 따라 Fill 색을 동적으로 변경하고 수치가 부드럽게 보간(Lerp)되도록 연출한다.
    /// 낮음(0) → 레드, 중간(50) → 앰버 골드, 높음(100) → 밝은 에메랄드 골드
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class PersuasionSliderColor : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text percentText;
        [SerializeField] private float lerpSpeed = 6f;

        static readonly Color Low  = new Color(0.85f, 0.20f, 0.20f, 1f); // Red
        static readonly Color Mid  = new Color(0.92f, 0.60f, 0.18f, 1f); // Amber
        static readonly Color High = new Color(0.25f, 0.85f, 0.45f, 1f); // Emerald Gold

        private Slider _slider;
        private float _targetNormalizedValue;
        private float _currentNormalizedValue;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            if (_slider != null)
            {
                _targetNormalizedValue = _slider.normalizedValue;
                _currentNormalizedValue = _targetNormalizedValue;
            }
        }

        private void Update()
        {
            if (_slider == null) return;

            // 슬라이더 값이 외부(Manager)에서 변경되었을 때 둔탁하게 끊기지 않고 부드럽게 연출
            float actualTarget = _slider.normalizedValue;
            _currentNormalizedValue = Mathf.Lerp(_currentNormalizedValue, actualTarget, Time.unscaledDeltaTime * lerpSpeed);

            if (fill != null)
            {
                fill.color = _currentNormalizedValue < 0.5f
                    ? Color.Lerp(Low, Mid, _currentNormalizedValue * 2f)
                    : Color.Lerp(Mid, High, (_currentNormalizedValue - 0.5f) * 2f);
            }

            if (percentText != null)
            {
                int displayPercent = Mathf.RoundToInt(_currentNormalizedValue * 100f);
                percentText.text = $"설득도 : {displayPercent}%";
            }
        }

        public void SetTargetPercentText(TMP_Text textComponent)
        {
            percentText = textComponent;
        }
    }
}
