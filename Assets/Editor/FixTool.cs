#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Persuasion;
using Persuasion.Core;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// gameData 배선 누락 문제를 두 겹으로 해결한다.
    /// (1) GameData.asset 을 Resources 로 이동해 런타임 폴백 로드가 가능하게 하고,
    /// (2) 현재 열린 씬의 PersuasionManager.gameData 를 직접 재배선한다.
    /// 메뉴: Persuasion > 5) Fix GameData Link
    /// </summary>
    public static class FixTool
    {
        [MenuItem("Persuasion/5) Fix GameData Link")]
        public static void Fix()
        {
            const string src = "Assets/Data/GameData.asset";
            const string dst = "Assets/Resources/GameData.asset";

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            // GameData 를 Resources 로 이동 (아직 없을 때만)
            if (AssetDatabase.LoadAssetAtPath<GameData>(dst) == null &&
                AssetDatabase.LoadAssetAtPath<GameData>(src) != null)
            {
                string err = AssetDatabase.MoveAsset(src, dst);
                if (!string.IsNullOrEmpty(err))
                    Debug.LogWarning("[Persuasion] GameData 이동 경고: " + err);
            }
            AssetDatabase.Refresh();

            var gd = AssetDatabase.LoadAssetAtPath<GameData>(dst) ??
                     AssetDatabase.LoadAssetAtPath<GameData>(src);
            if (gd == null) { Debug.LogError("[Persuasion] GameData를 찾지 못함. 먼저 '1) Create Game Data' 실행."); return; }

            // 열린 씬의 매니저에 직접 재배선
            var mgr = Object.FindFirstObjectByType<PersuasionManager>();
            if (mgr != null)
            {
                var so = new SerializedObject(mgr);
                var p = so.FindProperty("gameData");
                if (p != null)
                {
                    p.objectReferenceValue = gd;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mgr);
                }
                var check = new SerializedObject(mgr).FindProperty("gameData").objectReferenceValue;
                Debug.Log("[Persuasion] 씬 매니저 gameData 직접 재배선 결과: " + (check != null));

                var scene = mgr.gameObject.scene;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                Debug.Log("[Persuasion] 열린 씬에 PersuasionManager 없음 — Resources 폴백으로 동작합니다.");
            }

            Debug.Log("[Persuasion] Fix 완료 ▶ 이제 Play 하면 배경 초상화와 상단 제목이 표시됩니다.");
        }
    }
}
#endif
