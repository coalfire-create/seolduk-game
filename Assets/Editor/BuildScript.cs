#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// 커맨드라인(batchmode)에서도 실행 가능한 빌드 스크립트.
    /// 에디터 메뉴로도, 터미널에서 -executeMethod 로도 호출 가능.
    /// 예)
    ///   /Applications/Unity/Hub/Editor/<버전>/Unity.app/Contents/MacOS/Unity \
    ///     -batchmode -quit -nographics \
    ///     -projectPath "/Users/thomas/Desktop/게임 프로젝트/Game/My project" \
    ///     -executeMethod Persuasion.EditorTools.BuildScript.BuildMac -logFile -
    /// </summary>
    public static class BuildScript
    {
        private const string ScenePath = "Assets/Scenes/PlayScene.unity";

        [MenuItem("Persuasion/6) Build Mac App")]
        public static void BuildMac()
        {
            if (!RunBuild(BuildTarget.StandaloneOSX, "Builds/Mac/PersuasionGame.app"))
                EditorApplication.Exit(1);
        }

        [MenuItem("Persuasion/7) Build Windows App")]
        public static void BuildWindows()
        {
            if (!RunBuild(BuildTarget.StandaloneWindows64, "Builds/Windows/PersuasionGame.exe"))
                EditorApplication.Exit(1);
        }

        /// <summary>
        /// 씬 재생성 → Mac 빌드 → Windows 빌드를 한 번에 실행하는 통합 진입점.
        /// 커맨드라인 배치모드에서 -executeMethod Persuasion.EditorTools.BuildScript.BuildAll 로 호출.
        /// </summary>
        public static void BuildAll()
        {
            Debug.Log("[Persuasion] ===== BuildAll 시작: 씬 재생성 =====");
            SceneBuilderTool.Build();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Persuasion] ===== Mac 빌드 =====");
            bool macOk = RunBuild(BuildTarget.StandaloneOSX, "Builds/Mac/PersuasionGame.app");

            Debug.Log("[Persuasion] ===== Windows 빌드 =====");
            bool winOk = RunBuild(BuildTarget.StandaloneWindows64, "Builds/Windows/PersuasionGame.exe");

            Debug.Log("[Persuasion] ===== BuildAll 완료: Mac=" + macOk + ", Windows=" + winOk + " =====");
            if (!macOk || !winOk)
                EditorApplication.Exit(1);
        }

        private static bool RunBuild(BuildTarget target, string locationPathName)
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("[Persuasion] " + ScenePath + " 가 없습니다. 먼저 '4) Build Playable Scene' 을 실행하세요.");
                return false;
            }

            // 프로젝트 루트 기준 절대경로로 변환
            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            string fullPath = System.IO.Path.Combine(projectRoot, locationPathName);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = fullPath,
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var result = report.summary.result;
            Debug.Log("[Persuasion] 빌드 결과: " + result + " → " + fullPath +
                      " (에러 " + report.summary.totalErrors + "개, 크기 " + report.summary.totalSize + " bytes)");

            return result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        }
    }
}
#endif
