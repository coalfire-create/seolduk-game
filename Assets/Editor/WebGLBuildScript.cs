#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Persuasion.EditorTools
{
    public static class WebGLBuildScript
    {
        private const string ScenePath  = "Assets/Scenes/PlayScene.unity";
        private const string OutputPath = "WebGL-Build";

        [MenuItem("Persuasion/9) Build WebGL")]
        public static void BuildWebGL()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "..", ScenePath)))
            {
                Debug.LogError("[Persuasion] PlayScene 없음. 먼저 '4) Build Playable Scene' 실행.");
                return;
            }

            OptimizeTexturesForWebGL();
            OptimizeAudioForWebGL();

            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.emscriptenArgs = "-s ALLOW_MEMORY_GROWTH=1 -s INITIAL_MEMORY=536870912";

            var options = new BuildPlayerOptions
            {
                scenes           = new[] { ScenePath },
                locationPathName = outDir,
                target           = BuildTarget.WebGL,
                options          = BuildOptions.None,
            };

            var report  = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[Persuasion] WebGL 빌드 완료 → {outDir}  ({summary.totalSize / 1024 / 1024} MB)");
            else
                Debug.LogError($"[Persuasion] WebGL 빌드 실패: {summary.result}");
        }

        static void OptimizeTexturesForWebGL()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var settings = importer.GetPlatformTextureSettings("WebGL");
                bool changed = false;

                if (!settings.overridden || settings.maxTextureSize > 1024)
                {
                    settings.overridden    = true;
                    settings.maxTextureSize = 1024;
                    settings.format        = TextureImporterFormat.Automatic;
                    changed = true;
                }

                if (changed)
                {
                    importer.SetPlatformTextureSettings(settings);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    count++;
                }
            }

            if (count > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[Persuasion] 텍스처 {count}개 1024px 제한 적용 완료");
            }
        }

        static void OptimizeAudioForWebGL()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;

                var settings = importer.GetOverrideSampleSettings("WebGL");
                bool changed = false;

                if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    changed = true;
                }
                if (settings.quality > 0.5f)
                {
                    settings.quality = 0.4f;
                    changed = true;
                }
                if (settings.loadType != AudioClipLoadType.CompressedInMemory)
                {
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    changed = true;
                }

                if (changed)
                {
                    importer.SetOverrideSampleSettings("WebGL", settings);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    count++;
                    Debug.Log($"[Persuasion] 오디오 최적화: {Path.GetFileName(path)}");
                }
            }

            if (count > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[Persuasion] 오디오 {count}개 Vorbis 압축 적용 완료");
            }
        }

        public static void BatchBuild()
        {
            BuildWebGL();
        }

        public static void FullBatchBuild()
        {
            SceneBuilderTool.Build();
            BuildWebGL();
        }
    }
}
#endif
