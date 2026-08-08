#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Persuasion;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// Assets/Art/Characters/<stageId>/<Emotion>.png 들을 Sprite로 설정하고,
    /// StagePortraitLibrary 애셋을 자동 생성/갱신한다.
    /// 메뉴: Persuasion > 2) Build Portrait Library
    /// </summary>
    public static class PortraitSetupTool
    {
        private const string RootFolder = "Assets/Art/Characters";
        private const string LibPath = "Assets/Data/StagePortraitLibrary.asset";

        [MenuItem("Persuasion/2) Build Portrait Library")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                Debug.LogError("폴더가 없습니다: " + RootFolder);
                return;
            }

            // 1) 모든 텍스처를 Sprite로 임포트 설정
            string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { RootFolder });
            int converted = 0;
            foreach (var g in texGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.mipmapEnabled = false;
                    imp.SaveAndReimport();
                    converted++;
                }
            }

            // 2) 라이브러리 생성/갱신
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            var lib = AssetDatabase.LoadAssetAtPath<StagePortraitLibrary>(LibPath);
            bool isNew = lib == null;
            if (isNew) lib = ScriptableObject.CreateInstance<StagePortraitLibrary>();

            var sets = new List<StagePortraitSet>();
            foreach (var dir in AssetDatabase.GetSubFolders(RootFolder))
            {
                string stageId = Path.GetFileName(dir);
                // 캐릭터 소개 일러스트: Assets/Art/Transitions/{stageId}_intro.png
                var introSpr = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Transitions/{stageId}_intro.png");
                var set = new StagePortraitSet
                {
                    stageId    = stageId,
                    introSprite = introSpr,
                    baseSprite = Load(dir, "Base"),
                    anger      = Load(dir, "Anger"),
                    disgust    = Load(dir, "Disgust"),
                    fear       = Load(dir, "Fear"),
                    joy        = Load(dir, "Joy"),
                    sadness    = Load(dir, "Sadness"),
                    surprise   = Load(dir, "Surprise"),
                    confused   = Load(dir, "Confused"),
                    bewildered = Load(dir, "Bewildered"),
                };
                sets.Add(set);
            }
            lib.sets = sets.ToArray();

            if (isNew)
                AssetDatabase.CreateAsset(lib, LibPath);
            else
                EditorUtility.SetDirty(lib);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = lib;
            EditorGUIUtility.PingObject(lib);
            Debug.Log($"[Persuasion] Portrait Library 완료 — 스프라이트 변환 {converted}장, 스테이지 세트 {sets.Count}개: {LibPath}");
        }

        private static Sprite Load(string dir, string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{name}.png");
        }
    }
}
#endif
