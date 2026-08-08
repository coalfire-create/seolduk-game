#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// TMP 필수 리소스를 임포트하고, 한글이 렌더링되도록 AppleGothic 기반
    /// 동적(SDF) TMP 폰트 애셋을 생성해 TMP 기본 폰트로 지정한다.
    /// 메뉴: Persuasion > 3) Setup Text (TMP + 한글 폰트)
    /// </summary>
    public static class TextSetupTool
    {
        private const string FontTtfPath   = "Assets/Art/Fonts/AppleGothic.ttf";
        private const string FontAssetPath = "Assets/Art/Fonts/AppleGothic SDF.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Persuasion/3) Setup Text (TMP + 한글 폰트)")]
        public static void SetupText()
        {
            if (File.Exists(TmpSettingsPath))
            {
                CreateKoreanFontAndSetDefault();
            }
            else
            {
                string pkg = FindEssentialPackage();
                if (string.IsNullOrEmpty(pkg))
                {
                    Debug.LogError("[Persuasion] TMP Essential Resources.unitypackage 를 찾지 못했습니다. Window > TextMeshPro > Import TMP Essential Resources 를 수동 실행 후 다시 시도하세요.");
                    return;
                }
                Debug.Log("[Persuasion] TMP 필수 리소스 임포트 중... 완료되면 한글 폰트를 자동 생성합니다.");
                AssetDatabase.importPackageCompleted += OnTmpImported;
                AssetDatabase.ImportPackage(pkg, false);
            }
        }

        private static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            CreateKoreanFontAndSetDefault();
        }

        private static void CreateKoreanFontAndSetDefault()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
            if (sourceFont == null)
            {
                Debug.LogError("[Persuasion] 한글 TTF를 찾지 못했습니다: " + FontTtfPath);
                return;
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                fontAsset.name = "AppleGothic SDF";

                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

                if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                {
                    fontAsset.atlasTextures[0].name = "AppleGothic Atlas";
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
                }
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "AppleGothic Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
                AssetDatabase.SaveAssets();
            }

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                var prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = fontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("[Persuasion] 한글 TMP 폰트 준비 완료: " + FontAssetPath + " (TMP 기본 폰트로 지정됨)");
        }

        private static string FindEssentialPackage()
        {
            // 1) 에디터 내장 경로
            string editorPkg = EditorApplication.applicationContentsPath +
                "/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
            if (File.Exists(editorPkg)) return editorPkg;

            // 2) 프로젝트 PackageCache 검색
            string cacheRoot = Path.Combine(Directory.GetCurrentDirectory(), "Library/PackageCache");
            if (Directory.Exists(cacheRoot))
            {
                var found = Directory.GetFiles(cacheRoot, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
                if (found.Length > 0) return found[0];
            }
            return null;
        }
    }
}
#endif
