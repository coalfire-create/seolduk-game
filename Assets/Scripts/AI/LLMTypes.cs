using System;

namespace Persuasion.AI
{
    [Serializable]
    public class ChatTurn
    {
        public string role; // "player" 또는 "npc"
        public string text;
    }

    /// <summary>
    /// LLM이 response_format=json_object 로 강제 반환하는 스키마와 1:1 대응.
    /// JsonUtility로 바로 파싱되도록 필드명을 스키마와 동일하게 맞춤.
    /// </summary>
    [Serializable]
    public class NPCResponse
    {
        public string dialogue;
        public string emotion;          // Anger|Disgust|Fear|Joy|Sadness|Surprise|Confused|Bewildered
        public string gesture;          // 자유 텍스트: 팔짱 끼기/손 떨림/책상 두드림 등
        public int persuasionDelta;
        public string hint;             // 설득도 임계치 이상일 때만 채워짐 (캐릭터 대사 아님, UI 힌트 영역용)
        public string unlocked_evidence; // 특정 조건을 만족하여 획득할 증거 ID (없으면 빈 문자열)
        public bool goalAchieved;       // 캐릭터가 목표 정보를 실제로 자백/실토/공개하면 true → 즉시 클리어
    }
}
