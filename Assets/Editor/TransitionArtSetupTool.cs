#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Persuasion;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// Assets/Art/Transitions 안의 title_screen.png / {stageId}_intro.png /
    /// {fromId}_to_{toId}_transition[_변형].png 들을 Sprite로 설정하고
    /// TransitionArtLibrary 애셋을 자동 생성/갱신한다.
    /// 메뉴: Persuasion > 5) Build Transition Art Library
    /// </summary>
    public static class TransitionArtSetupTool
    {
        private const string RootFolder = "Assets/Art/Transitions";
        private const string LibPath = "Assets/Data/TransitionArtLibrary.asset";

        private static readonly Regex TransitionPattern =
            new Regex(@"^(?<from>.+)__to__(?<to>.+)_transition(?:_(?<variant>\w+))?$", RegexOptions.Compiled);
        private static readonly Regex IntroPattern =
            new Regex(@"^(?<stage>.+)_intro$", RegexOptions.Compiled);
        private static readonly Regex CharacterPattern =
            new Regex(@"^(?<stage>.+)_character$", RegexOptions.Compiled);

        [MenuItem("Persuasion/5) Build Transition Art Library")]
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

            var lib = AssetDatabase.LoadAssetAtPath<TransitionArtLibrary>(LibPath);
            bool isNew = lib == null;
            if (isNew) lib = ScriptableObject.CreateInstance<TransitionArtLibrary>();

            var introMap = new Dictionary<string, StageIntroEntry>();
            var transitionMap = new Dictionary<string, StageTransitionEntry>();
            Sprite titleScreen = null;

            var files = new List<string>(texGuids);
            files.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

            foreach (var g in files)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                string name = Path.GetFileNameWithoutExtension(path);

                if (name == "title_screen")
                {
                    titleScreen = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    continue;
                }

                var transMatch = TransitionPattern.Match(name);
                if (transMatch.Success)
                {
                    string from = transMatch.Groups["from"].Value;
                    string to   = transMatch.Groups["to"].Value;
                    bool isVariant = transMatch.Groups["variant"].Success;
                    string key = from + "->" + to;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (!transitionMap.TryGetValue(key, out var tEntry))
                    {
                        tEntry = new StageTransitionEntry { fromStageId = from, toStageId = to };
                        transitionMap[key] = tEntry;
                    }
                    if (isVariant) tEntry.spriteExtra = sprite;
                    else           tEntry.sprite      = sprite;
                    continue;
                }

                var introMatch = IntroPattern.Match(name);
                if (introMatch.Success)
                {
                    string stageId = introMatch.Groups["stage"].Value;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (!introMap.TryGetValue(stageId, out var iEntry))
                    {
                        iEntry = new StageIntroEntry { stageId = stageId };
                        introMap[stageId] = iEntry;
                    }
                    iEntry.sprite = sprite;
                    continue;
                }

                var charMatch = CharacterPattern.Match(name);
                if (charMatch.Success)
                {
                    string stageId = charMatch.Groups["stage"].Value;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (!introMap.TryGetValue(stageId, out var iEntry))
                    {
                        iEntry = new StageIntroEntry { stageId = stageId };
                        introMap[stageId] = iEntry;
                    }
                    iEntry.characterSprite = sprite;
                    continue;
                }

                Debug.LogWarning("[Persuasion] 파일명 패턴과 매칭되지 않음(무시): " + path);
            }

            lib.titleScreen = titleScreen;
            lib.intros = new List<StageIntroEntry>(introMap.Values).ToArray();
            var transitionsList = new List<StageTransitionEntry>(transitionMap.Values);
            lib.transitions = transitionsList.ToArray();

            if (isNew)
                AssetDatabase.CreateAsset(lib, LibPath);
            else
                EditorUtility.SetDirty(lib);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = lib;
            EditorGUIUtility.PingObject(lib);
            Debug.Log($"[Persuasion] Transition Art Library 완료 — 스프라이트 변환 {converted}장, 인트로 {introMap.Count}개, 전환 {transitionsList.Count}개, 타이틀 {(titleScreen != null ? "있음" : "없음")}: {LibPath}");
        }
    }
}
#endif
