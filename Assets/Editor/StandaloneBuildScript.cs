#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// 테스터 배포용 스탠드얼론(Windows/Mac 실행파일) 빌드.
    /// 브라우저(WebGL)에서만 나던 크래시·키보드·오디오 문제를 회피 — 에디터와 동일한 네이티브 환경.
    /// AI 호출은 LLMClient가 배포된 서버 프록시(seolduk-game.vercel.app/api/chat)를 쓰므로 API 키가 실행파일에 포함되지 않는다.
    /// </summary>
    public static class StandaloneBuildScript
    {
        private const string ScenePath = "Assets/Scenes/PlayScene.unity";

        [MenuItem("Persuasion/10) Build Windows (테스터용)")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows", "설득의기술.exe");
        }

        [MenuItem("Persuasion/11) Build Mac (테스터용)")]
        public static void BuildMac()
        {
            Build(BuildTarget.StandaloneOSX, "Builds/Mac", "설득의기술.app");
        }

        private static void Build(BuildTarget target, string outRel, string exeName)
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "..", ScenePath)))
            {
                Debug.LogError("[Persuasion] PlayScene 없음. 먼저 'Persuasion/4) Build Playable Scene' 실행.");
                return;
            }

            // Windows 모듈 미설치 시 크로스 빌드 불가 → 친절한 에러
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                Debug.LogError($"[Persuasion] {target} 빌드 모듈이 설치돼 있지 않습니다. " +
                               "Unity Hub → 설치 → 이 에디터 버전 → Add Modules 에서 " +
                               (target == BuildTarget.StandaloneWindows64 ? "'Windows Build Support (Mono)'" : "'Mac Build Support (Mono)'") +
                               " 를 추가하세요.");
                return;
            }

            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outRel));
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            var options = new BuildPlayerOptions
            {
                scenes           = new[] { ScenePath },
                locationPathName = Path.Combine(outDir, exeName),
                target           = target,
                targetGroup      = BuildTargetGroup.Standalone,
                options          = BuildOptions.None,
            };

            var report  = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[Persuasion] {target} 빌드 완료 → {outDir}  ({summary.totalSize / 1024 / 1024} MB)\n" +
                          "이 폴더 전체를 zip으로 압축해 테스터에게 전달하거나 itch.io에 업로드하세요.");
            else
                Debug.LogError($"[Persuasion] {target} 빌드 실패: {summary.result}");
        }
    }
}
#endif
