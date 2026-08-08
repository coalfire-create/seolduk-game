#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// Pretendard-Bold.ttf 로부터 동적(Dynamic) TMP SDF 폰트 에셋을 생성한다.
    /// 동적 모드라 한글 전체 글리프를 런타임에 렌더 → 아틀라스가 작고 모든 글자 지원.
    /// AppleGothic SDF를 폴백으로 연결해 누락 글리프도 안전.
    /// </summary>
    public static class FontAssetBuilder
    {
        private const string TtfPath      = "Assets/Art/Fonts/Pretendard-Bold.ttf";
        private const string OutPath      = "Assets/Art/Fonts/Pretendard-Bold SDF.asset";
        private const string FallbackPath = "Assets/Art/Fonts/AppleGothic SDF.asset";

        [MenuItem("Persuasion/2) Build Pretendard Font")]
        public static void Build()
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (ttf == null)
            {
                Debug.LogError($"[Persuasion] 폰트 원본 없음: {TtfPath}");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath) != null)
                AssetDatabase.DeleteAsset(OutPath);

            // 90px 샘플링, 9px 패딩, SDFAA, 1024 아틀라스, 동적 채움
            var fa = TMP_FontAsset.CreateFontAsset(
                ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);

            if (fa == null)
            {
                Debug.LogError("[Persuasion] TMP 폰트 에셋 생성 실패");
                return;
            }
            fa.name = "Pretendard-Bold SDF";

            AssetDatabase.CreateAsset(fa, OutPath);

            // 머티리얼·아틀라스 텍스처를 서브에셋으로 저장 (안 하면 참조 유실)
            if (fa.material != null)
            {
                fa.material.name = "Pretendard-Bold SDF Material";
                if (!AssetDatabase.Contains(fa.material))
                    AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            if (fa.atlasTextures != null)
            {
                foreach (var tex in fa.atlasTextures)
                {
                    if (tex != null && !AssetDatabase.Contains(tex))
                    {
                        tex.name = "Pretendard-Bold SDF Atlas";
                        AssetDatabase.AddObjectToAsset(tex, fa);
                    }
                }
            }

            // 폴백(누락 글리프 대비)
            var fb = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
            if (fb != null)
                fa.fallbackFontAssetTable = new List<TMP_FontAsset> { fb };

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(OutPath);
            Debug.Log($"[Persuasion] Pretendard SDF 생성 완료 → {OutPath}");
        }
    }
}
#endif
