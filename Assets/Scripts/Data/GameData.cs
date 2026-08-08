using UnityEngine;

namespace Persuasion
{
    /// <summary>
    /// 게임 전체 콘텐츠(9단계)와 공통 판정 기준을 담는 ScriptableObject.
    /// Assets > Create > Persuasion > Game Data 로 생성, 또는
    /// 메뉴 Persuasion > 1) Create Game Data (9 Stages) 로 실제 콘텐츠 채워서 자동 생성.
    /// </summary>
    [CreateAssetMenu(fileName = "GameData", menuName = "Persuasion/Game Data")]
    public class GameData : ScriptableObject
    {
        public string gameTitle = "설득의 기술";

        [Tooltip("StageOrder 기준으로 정렬되어 있어야 함 (0~8)")]
        public StageData[] stages;

        [Header("판정 기준")]
        public int maxTurns = 20;

        [Tooltip("설득도 0%에서 감점 발화가 이 횟수 이상 연속되면 실패")]
        public int zeroStreakFailThreshold = 3;

        [Tooltip("설득도와 무관하게 감점 발화가 이 횟수 이상 연속되면 실패")]
        public int negativeStreakFailThreshold = 5;

        [Tooltip("설득도가 이 값 이상이면 LLM이 대사와 별개로 힌트를 함께 반환하도록 요청")]
        public int hintUnlockThreshold = 50;
    }
}
