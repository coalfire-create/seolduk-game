using System.Runtime.InteropServices;
using UnityEngine;

namespace Persuasion.UI
{
    /// <summary>
    /// Web Share API (WebGL) 또는 클립보드 복사 래퍼.
    /// </summary>
    public static class ShareManager
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void Share_Try(string title, string text, string url);
#endif

        public static void Share(string title, string text, string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Share_Try(title, text, url);
#else
            GUIUtility.systemCopyBuffer = text + "\n" + url;
            Debug.Log($"[ShareManager] Copied to clipboard: {text}\n{url}");
#endif
        }
    }
}
