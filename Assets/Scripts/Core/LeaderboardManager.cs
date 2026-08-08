using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace Persuasion.Core
{
    [Serializable] public class LbEntry { public string name; public string grade; public int turns; }
    [Serializable] class LbResponse { public LbEntry[] entries; }

    /// <summary>
    /// 글로벌 리더보드 클라이언트. Netlify Function(/api/leaderboard)과 통신.
    /// WebGL은 현재 페이지 오리진 기준으로 URL 해석, 에디터는 editorBaseUrl(옵션) 사용.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        [SerializeField] private string endpoint = "/api/leaderboard";
        [Tooltip("에디터 테스트용 절대 URL (예: https://site.netlify.app). 비우면 에디터에서 네트워크 생략.")]
        [SerializeField] private string editorBaseUrl = "";

        [Serializable] class SubmitBody { public string name; public string stageId; public string grade; public int turns; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern IntPtr Web_Prompt(string msg, string def);
#endif

        /// <summary>WebGL: window.prompt로 닉네임 입력. 에디터/기타: null 반환(호출측이 폴백).</summary>
        public static string PromptName(string message, string def)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var ptr = Web_Prompt(message, def ?? "");
            return SaveManager.PtrToStringUTF8(ptr);
#else
            return null;
#endif
        }

        public void FetchTop(string stageId, Action<List<LbEntry>> onDone, Action<string> onError)
        {
            StartCoroutine(FetchRoutine(stageId, onDone, onError));
        }

        public void Submit(string playerName, string stageId, string grade, int turns,
                           Action<List<LbEntry>> onDone, Action<string> onError)
        {
            StartCoroutine(SubmitRoutine(playerName, stageId, grade, turns, onDone, onError));
        }

        private string ResolveBase()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (endpoint.StartsWith("http")) return endpoint;
            string page = Application.absoluteURL;
            if (!string.IsNullOrEmpty(page))
            {
                try
                {
                    var uri = new Uri(page);
                    return uri.GetLeftPart(UriPartial.Authority) + (endpoint.StartsWith("/") ? endpoint : "/" + endpoint);
                }
                catch { }
            }
            return endpoint;
#else
            if (!string.IsNullOrEmpty(editorBaseUrl))
                return editorBaseUrl.TrimEnd('/') + (endpoint.StartsWith("/") ? endpoint : "/" + endpoint);
            return null; // 에디터에서 미설정 → 네트워크 생략
#endif
        }

        private IEnumerator FetchRoutine(string stageId, Action<List<LbEntry>> onDone, Action<string> onError)
        {
            string baseUrl = ResolveBase();
            if (string.IsNullOrEmpty(baseUrl)) { onError?.Invoke("리더보드 URL 미설정(에디터)"); yield break; }

            string url = baseUrl + "?stage=" + UnityWebRequest.EscapeURL(stageId);
            using var req = UnityWebRequest.Get(url);
#if !UNITY_WEBGL
            req.timeout = 15;
#endif
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { onError?.Invoke(req.error); yield break; }
            onDone?.Invoke(Parse(req.downloadHandler.text));
        }

        private IEnumerator SubmitRoutine(string playerName, string stageId, string grade, int turns,
                                          Action<List<LbEntry>> onDone, Action<string> onError)
        {
            string baseUrl = ResolveBase();
            if (string.IsNullOrEmpty(baseUrl)) { onError?.Invoke("리더보드 URL 미설정(에디터)"); yield break; }

            var payload = new SubmitBody { name = playerName, stageId = stageId, grade = grade, turns = turns };
            byte[] raw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

            using var req = new UnityWebRequest(baseUrl, "POST");
            req.uploadHandler   = new UploadHandlerRaw(raw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
#if !UNITY_WEBGL
            req.timeout = 15;
#endif
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                string msg = req.responseCode == 429 ? "잠시 후 다시 시도해 주세요." : req.error;
                onError?.Invoke(msg);
                yield break;
            }
            onDone?.Invoke(Parse(req.downloadHandler.text));
        }

        private static List<LbEntry> Parse(string json)
        {
            var list = new List<LbEntry>();
            try
            {
                var r = JsonUtility.FromJson<LbResponse>(json);
                if (r != null && r.entries != null) list.AddRange(r.entries);
            }
            catch { }
            return list;
        }
    }
}
