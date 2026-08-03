using System;
using UnityEngine;

namespace Persuasion
{
    [Serializable]
    public class StageIntroEntry
    {
        [Tooltip("StageData.stageId 와 동일해야 함")]
        public string stageId;
        public Sprite sprite;
    }

    [Serializable]
    public class StageTransitionEntry
    {
        [Tooltip("직전 스테이지의 StageData.stageId")]
        public string fromStageId;
        [Tooltip("다음 스테이지의 StageData.stageId")]
        public string toStageId;
        public Sprite sprite;
        [Tooltip("전환컷이 2장으로 구성될 때 두 번째 컷(예: 살해 장면). 비어있으면 1장.")]
        public Sprite spriteExtra;
    }

    /// <summary>
    /// 타이틀 화면 + 스테이지별 인트로컷 + 스테이지 간 전환컷 모음.
    /// 메뉴 Persuasion > 5) Build Transition Art Library 로 Assets/Art/Transitions 에서 자동 생성.
    /// </summary>
    [CreateAssetMenu(fileName = "TransitionArtLibrary", menuName = "Persuasion/Transition Art Library")]
    public class TransitionArtLibrary : ScriptableObject
    {
        public Sprite titleScreen;
        public StageIntroEntry[] intros;
        public StageTransitionEntry[] transitions;

        public Sprite GetIntro(string stageId)
        {
            if (intros == null || string.IsNullOrEmpty(stageId)) return null;
            foreach (var e in intros)
                if (e != null && e.stageId == stageId) return e.sprite;
            return null;
        }

        public Sprite GetTransition(string fromStageId, string toStageId)
        {
            return GetTransition(fromStageId, toStageId, 0);
        }

        /// <summary>전환컷 스프라이트를 페이지 인덱스로 조회. page>=1이고 두 번째 컷이 있으면 그것을 반환.</summary>
        public Sprite GetTransition(string fromStageId, string toStageId, int page)
        {
            var e = FindTransition(fromStageId, toStageId);
            if (e == null) return null;
            if (page >= 1 && e.spriteExtra != null) return e.spriteExtra;
            return e.sprite;
        }

        /// <summary>이 전환이 몇 장의 컷으로 구성되는지(1 또는 2).</summary>
        public int GetTransitionPageCount(string fromStageId, string toStageId)
        {
            var e = FindTransition(fromStageId, toStageId);
            if (e == null) return 0;
            return e.spriteExtra != null ? 2 : 1;
        }

        private StageTransitionEntry FindTransition(string fromStageId, string toStageId)
        {
            if (transitions == null || string.IsNullOrEmpty(fromStageId) || string.IsNullOrEmpty(toStageId)) return null;
            foreach (var e in transitions)
                if (e != null && e.fromStageId == fromStageId && e.toStageId == toStageId) return e;
            return null;
        }
    }
}
