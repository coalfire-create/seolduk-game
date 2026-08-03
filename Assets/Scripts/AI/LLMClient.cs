using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Persuasion.Core;

namespace Persuasion.AI
{
    /// <summary>
    /// LLM API 래퍼 (OpenAI Chat Completions 호환).
    /// - 프로덕션 WebGL: 백엔드 프록시(proxyEndpoint)로 호출 → API 키가 클라이언트에 포함되지 않음.
    /// - 개발(에디터/스탠드얼론): OpenAI 직접 호출. 키 우선순위:
    ///     1) LLM_API_KEY 환경변수
    ///     2) 프로젝트 루트 apikey.local.txt (StreamingAssets에 두면 배포에 포함되므로 금지)
    /// </summary>
    public class LLMClient : MonoBehaviour
    {
        // 스탠드얼론(Win/Mac 실행파일) 기본 프록시 — 키가 실행파일에 안 들어가도록 서버 프록시로 호출
        private const string DefaultStandaloneProxy = "https://seolduk-game.vercel.app/api/chat";

        [Header("프로덕션(WebGL): 백엔드 프록시로 호출 — API 키가 클라이언트에 포함되지 않음")]
        [Tooltip("게임과 같은 도메인에 배포된 프록시 경로. netlify.toml의 /api/chat 리다이렉트와 일치.")]
        [SerializeField] private string proxyEndpoint = "/api/chat";

        [Header("스탠드얼론(Win/Mac 실행파일): 이 절대 URL 프록시로 호출 — API 키 미포함")]
        [SerializeField] private string standaloneProxyUrl = DefaultStandaloneProxy;

        [Header("개발(에디터 전용): OpenAI 직접 호출")]
        [SerializeField] private string apiEndpoint = "https://api.openai.com/v1/chat/completions";
        [SerializeField] private string model = "gpt-4o-mini";

        // 1일 최대 API 호출 횟수 (클라이언트 측 소프트 제한 — UX용. 실제 비용 강제는 서버 프록시가 담당)
        private const int DailyLimit = 50;

        private string _cachedApiKey;
        private bool   _apiKeyLoaded;
        private bool   _apiKeyReady;   // 키/프록시 준비 신호
        private Coroutine _activeRequestCoroutine;

        private void Awake()
        {
#if !UNITY_EDITOR
            // 모든 플레이어 빌드(WebGL/스탠드얼론): 키를 클라이언트에 두지 않고 백엔드 프록시로 호출한다.
            _apiKeyReady = true;
#else
            // 에디터 전용(개발): 환경변수 → 프로젝트 루트 apikey.local.txt (StreamingAssets에 두지 말 것 — 배포에 포함됨)
            string envKey = Environment.GetEnvironmentVariable("LLM_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                _cachedApiKey = envKey.Trim();
                _apiKeyLoaded = true;
            }
            else
            {
                try
                {
                    string path = System.IO.Path.Combine(Application.dataPath, "..", "apikey.local.txt");
                    if (System.IO.File.Exists(path))
                    {
                        string fileKey = System.IO.File.ReadAllText(path).Trim();
                        if (!string.IsNullOrEmpty(fileKey))
                        {
                            _cachedApiKey = fileKey;
                            _apiKeyLoaded = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[LLMClient] apikey.local.txt 읽기 실패: " + e.Message);
                }
            }
            _apiKeyReady = true;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>프록시 상대 경로를 현재 페이지 오리진 기준 절대 URL로 변환.</summary>
        private string ResolveProxyUrl()
        {
            if (string.IsNullOrEmpty(proxyEndpoint)) return "/api/chat";
            if (proxyEndpoint.StartsWith("http")) return proxyEndpoint;
            string page = Application.absoluteURL;
            if (string.IsNullOrEmpty(page)) return proxyEndpoint;
            try
            {
                var uri = new Uri(page);
                string origin = uri.GetLeftPart(UriPartial.Authority);
                string path = proxyEndpoint.StartsWith("/") ? proxyEndpoint : "/" + proxyEndpoint;
                return origin + path;
            }
            catch { return proxyEndpoint; }
        }
#endif

        public void RequestNPCResponse(string systemPrompt, IReadOnlyList<ChatTurn> conversation,
                                       Action<NPCResponse> onSuccess, Action<string> onError)
        {
            if (_activeRequestCoroutine != null) StopCoroutine(_activeRequestCoroutine);
            _activeRequestCoroutine = StartCoroutine(SendRequest(systemPrompt, conversation, onSuccess, onError));
        }

        /// <summary>진행 중인 LLM 요청 코루틴을 즉시 중단한다. UI 타임아웃 발동 시 호출.</summary>
        public void AbortCurrentRequest()
        {
            if (_activeRequestCoroutine == null) return;
            StopCoroutine(_activeRequestCoroutine);
            _activeRequestCoroutine = null;
        }

        private bool CheckDailyLimit(out string limitError)
        {
            limitError = null;
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string savedDate  = SaveManager.Get("llm_daily_date",  "");
            int    savedCount = savedDate == today ? SaveManager.GetInt("llm_daily_count", 0) : 0;

            if (savedCount >= DailyLimit)
            {
                GameAnalytics.LogRateLimitHit();
                limitError = $"오늘의 AI 응답 한도({DailyLimit}회)에 도달했습니다.\n내일 다시 접속하면 한도가 초기화됩니다.";
                return false;
            }

            SaveManager.Set("llm_daily_date",  today);
            SaveManager.SetInt("llm_daily_count", savedCount + 1);
            return true;
        }

        private IEnumerator SendRequest(string systemPrompt, IReadOnlyList<ChatTurn> conversation,
                                        Action<NPCResponse> onSuccess, Action<string> onError)
        {
            // 키/프록시 준비될 때까지 대기
            while (!_apiKeyReady) yield return null;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL 프록시 모드: 페이지 오리진 기준 상대 경로 → 절대 URL
            string targetUrl = ResolveProxyUrl();
#elif !UNITY_EDITOR
            // 스탠드얼론(Win/Mac) 프록시 모드: 절대 URL 프록시로 호출 (키 미포함)
            string targetUrl = string.IsNullOrEmpty(standaloneProxyUrl) ? DefaultStandaloneProxy : standaloneProxyUrl;
#else
            // 에디터: OpenAI 직접 호출
            if (!_apiKeyLoaded || string.IsNullOrEmpty(_cachedApiKey))
            {
                onError?.Invoke("API 키가 설정되지 않았습니다. LLM_API_KEY 환경변수 또는 프로젝트 루트 apikey.local.txt 를 확인하세요.");
                yield break;
            }
            string targetUrl = apiEndpoint;
#endif

            if (!CheckDailyLimit(out string limitError))
            {
                onError?.Invoke(limitError);
                _activeRequestCoroutine = null;
                yield break;
            }

            string body    = BuildRequestBody(systemPrompt, conversation);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var request = new UnityWebRequest(targetUrl, "POST");
                request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
#if !UNITY_WEBGL
                request.timeout = 20; // WebGL에서는 무시됨 (UI 타임아웃이 대신 처리)
#endif
                request.SetRequestHeader("Content-Type", "application/json");
#if UNITY_EDITOR
                // 에디터 직접 호출 때만 인증 헤더. 플레이어 빌드는 프록시가 서버에서 키 주입.
                request.SetRequestHeader("Authorization", "Bearer " + _cachedApiKey);
#endif

                yield return request.SendWebRequest();

                long   code    = request.responseCode;
                bool   success = request.result == UnityWebRequest.Result.Success;
                string errText = request.error + "\n" + (request.downloadHandler != null ? request.downloadHandler.text : "");
                string payload = success ? request.downloadHandler.text : null;
                request.Dispose();

                if (success)
                {
                    NPCResponse parsed;
                    try { parsed = ParseResponse(payload); }
                    catch (Exception e) { onError?.Invoke("응답 파싱 실패: " + e.Message); _activeRequestCoroutine = null; yield break; }
                    onSuccess?.Invoke(parsed);
                    _activeRequestCoroutine = null;
                    yield break;
                }

                bool retriable = code >= 500 || code == 429 || code == 0;
                if (retriable && attempt < maxAttempts)
                {
                    yield return new WaitForSeconds(attempt);
                    continue;
                }

                onError?.Invoke("LLM 요청 실패: " + errText);
                _activeRequestCoroutine = null;
                yield break;
            }
        }

        private string BuildRequestBody(string systemPrompt, IReadOnlyList<ChatTurn> conversation)
        {
            var messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { role = "system", content = systemPrompt }
            };
            if (conversation != null)
            {
                foreach (var turn in conversation)
                {
                    if (turn == null || string.IsNullOrEmpty(turn.text)) continue;
                    messages.Add(new OpenAIMessage
                    {
                        role    = (turn.role == "player") ? "user" : "assistant",
                        content = turn.text
                    });
                }
            }

            var payload = new OpenAIChatRequest
            {
                model           = model,
                messages        = messages.ToArray(),
                response_format = new OpenAIResponseFormat { type = "json_object" }
            };
            return JsonUtility.ToJson(payload);
        }

        private NPCResponse ParseResponse(string rawJson)
        {
            var wrapper = JsonUtility.FromJson<OpenAIChatResponseWrapper>(rawJson);
            string content = wrapper.choices[0].message.content;
            return JsonUtility.FromJson<NPCResponse>(content);
        }

        [Serializable] private class OpenAIMessage { public string role; public string content; }
        [Serializable] private class OpenAIResponseFormat { public string type; }
        [Serializable]
        private class OpenAIChatRequest
        {
            public string         model;
            public OpenAIMessage[] messages;
            public OpenAIResponseFormat response_format;
        }
        [Serializable] private class OpenAIChoice { public OpenAIMessage message; }
        [Serializable] private class OpenAIChatResponseWrapper { public OpenAIChoice[] choices; }
    }
}
