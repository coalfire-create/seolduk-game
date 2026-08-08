using System;
using UnityEngine;

namespace Persuasion
{
    /// <summary>시나리오 1단계. 상대 캐릭터 1명 + 목표 1개로 구성된 대화 세션 하나.</summary>
    [Serializable]
    public class StageData
    {
        public int stageOrder;
        public string chapterId;
        public string chapterTitle;

        [Tooltip("이 스테이지(가 속한 챕터)에서 플레이어가 맡는 역할")]
        public string playerRole;

        public string stageId;
        public string stageTitle;
        public string characterName;

        [TextArea(2, 6)] public string characterPersona;
        [TextArea(3, 8)] public string backgroundNarration;

        [TextArea(3, 8)]
        [Tooltip("도입컷 2단계: 캐릭터 소개 내레이션. 채워지면 장면 내레이션 이후 캐릭터 초상화와 함께 표시.")]
        public string characterNarration;

        [Tooltip("플레이어가 이 스테이지에서 알아내야 하는 것")]
        public string goal;

        [TextArea(2, 4)]
        [Tooltip("스테이지 시작 시 상대(NPC)가 먼저 건네는 첫 대사. 대략적인 상황을 드러내야 함. 비어있으면 sampleLineAt0 사용.")]
        public string openingLine;

        [TextArea(2, 4)] public string sampleLineAt0;
        [TextArea(2, 4)] public string sampleLineAt50;
        [TextArea(2, 4)] public string sampleLineAt100;

        [TextArea(2, 6)]
        [Tooltip("스테이지 클리어 후 다음 스테이지로 넘어가기 전 컷씬 텍스트. 비어있을 수 있음.")]
        public string transitionNarration;

        [TextArea(2, 5)]
        [Tooltip("스테이지 실패 시 NPC 마지막 대사. 결과 화면에 표시.")]
        public string failureDialogue;

        [Tooltip("이 스테이지에서 해금 가능한 증거 리스트")]
        public EvidenceData[] evidences;
    }

    [Serializable]
    public class EvidenceData
    {
        public string evidenceId;
        public string evidenceName;
        [TextArea(2, 4)] public string evidenceDescription;
        [Tooltip("AI가 특정 답변을 할 때 숨겨진 힌트 태그로 이걸 포함하면 해금됨. 예: EVIDENCE_UNLOCK: 블랙박스")]
        public string unlockTag;
    }
}
