using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

namespace Persuasion.UI
{
    /// <summary>
    /// WebGL Windows에서 한글 IME 입력을 처리하는 브리지.
    /// HTML textarea 오버레이를 Unity 입력창 위에 배치해 IME 조합을 수신하고
    /// SendMessage로 Unity TMP_InputField에 전달한다.
    /// </summary>
    public class KoreanInputBridge : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void KorIme_Init(string goName, string onText, string onSubmit);
        [DllImport("__Internal")] static extern void KorIme_Show();
        [DllImport("__Internal")] static extern void KorIme_Hide();
        [DllImport("__Internal")] static extern void KorIme_Clear();
#endif

        TMP_InputField _field;
        System.Action  _onSubmit;

        public bool IsOverlayActive { get; private set; }

        public void Init(TMP_InputField field, System.Action onSubmit)
        {
            _field    = field;
            _onSubmit = onSubmit;
#if UNITY_WEBGL && !UNITY_EDITOR
            KorIme_Init(gameObject.name, nameof(OnTextUpdate), nameof(OnSubmit));
#endif
        }

        public void ShowOverlay()
        {
            if (IsOverlayActive) return;
            IsOverlayActive = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            KorIme_Show();
#endif
        }

        public void HideOverlay()
        {
            IsOverlayActive = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            KorIme_Hide();
#endif
        }

        public void ClearOverlay()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            KorIme_Clear();
#endif
        }

        // ── JS → Unity callbacks (called via SendMessage from jslib) ──

        /// <summary>HTML에서 텍스트가 변경될 때마다 호출. Unity 입력 필드를 동기화.</summary>
        public void OnTextUpdate(string text)
        {
            if (_field == null) return;
            _field.SetTextWithoutNotify(text);
        }

        /// <summary>HTML에서 Enter가 눌렸을 때 호출. 텍스트를 넣고 제출.</summary>
        public void OnSubmit(string text)
        {
            if (_field == null) return;
            _field.SetTextWithoutNotify(text);
            _onSubmit?.Invoke();
        }
    }
}
