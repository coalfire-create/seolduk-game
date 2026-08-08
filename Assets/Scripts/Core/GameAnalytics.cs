using System.Runtime.InteropServices;
using UnityEngine;

namespace Persuasion.Core
{
    /// <summary>
    /// GA4 이벤트 전송 래퍼. WebGL 빌드에서 gtag() 호출, 에디터에서는 로그만 출력.
    /// index.html에 GA4 스크립트(G-XXXXXXXXXX)가 있어야 동작한다.
    /// </summary>
    public static class GameAnalytics
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GA4_Event(string name, string paramsJson);
#endif

        static void Send(string name, string paramsJson = "{}")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            GA4_Event(name, paramsJson);
#else
            Debug.Log($"[Analytics] {name}: {paramsJson}");
#endif
        }

        public static void LogStageStart(string stageId) =>
            Send("stage_start", $"{{\"stage_id\":\"{Esc(stageId)}\"}}");

        public static void LogStageClear(string stageId, string grade, int turns) =>
            Send("stage_clear", $"{{\"stage_id\":\"{Esc(stageId)}\",\"grade\":\"{grade}\",\"turns\":{turns}}}");

        public static void LogStageFail(string stageId, int turns) =>
            Send("stage_fail", $"{{\"stage_id\":\"{Esc(stageId)}\",\"turns\":{turns}}}");

        public static void LogGameCompleted(int totalTurns) =>
            Send("game_completed", $"{{\"total_turns\":{totalTurns}}}");

        public static void LogRateLimitHit() =>
            Send("rate_limit_hit", "{}");

        static string Esc(string s) => s?.Replace("\"", "\\\"") ?? "";
    }
}
