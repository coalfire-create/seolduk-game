using System;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Persuasion.Core
{
    /// <summary>
    /// localStorage(WebGL) 또는 PlayerPrefs(에디터/스탠드얼론) 저장소 래퍼.
    /// 씬 오브젝트 없이 호출 가능한 static 유틸리티.
    /// </summary>
    public static class SaveManager
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern System.IntPtr SaveGet(string key);
        [DllImport("__Internal")] static extern void SaveSet(string key, string value);
        [DllImport("__Internal")] static extern void SaveRemove(string key);
#endif

        /// <summary>널 종료 UTF-8 C 문자열을 안전하게 디코드 (한글 등 멀티바이트 대응, 모든 API 레벨 호환).</summary>
        public static string PtrToStringUTF8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            if (len == 0) return string.Empty;
            var bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string Get(string key, string defaultVal = "")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var ptr = SaveGet(key);
            var val = PtrToStringUTF8(ptr);
            return string.IsNullOrEmpty(val) ? defaultVal : val;
#else
            return PlayerPrefs.GetString(key, defaultVal);
#endif
        }

        public static void Set(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveSet(key, value);
#else
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
#endif
        }

        public static void Remove(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveRemove(key);
#else
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
#endif
        }

        public static int GetInt(string key, int defaultVal = 0)
        {
            if (int.TryParse(Get(key, defaultVal.ToString()), out int v)) return v;
            return defaultVal;
        }

        public static void SetInt(string key, int value) => Set(key, value.ToString());
    }
}
