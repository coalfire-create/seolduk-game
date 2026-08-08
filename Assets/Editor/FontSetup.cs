#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

namespace Persuasion.EditorTools
{
    public static class FontSetup
    {
        private const string TtfPath   = "Assets/Art/Fonts/Pretendard-Bold.ttf";
        private const string AssetPath = "Assets/Art/Fonts/Pretendard-Bold SDF.asset";

        [MenuItem("Persuasion/0) Setup Pretendard Font SDF")]
        public static void CreatePretendardFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null)
            {
                Debug.LogError("[Font] Pretendard-Bold.ttf 없음: " + TtfPath);
                return;
            }

            // 이미 있으면 건너뜀
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) != null)
            {
                Debug.Log("[Font] Pretendard-Bold SDF 이미 존재. 건너뜀.");
                return;
            }

            // Dynamic 모드 — 한국어처럼 글자수가 많은 언어에 최적
            var fa = TMP_FontAsset.CreateFontAsset(font);
            fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(fa, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Font] 생성 완료 (Dynamic): " + AssetPath);
        }

        // 배치 모드 진입점
        public static void BatchCreateFont()
        {
            CreatePretendardFont();
        }
    }
}
#endif
