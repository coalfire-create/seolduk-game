namespace Persuasion
{
    /// <summary>
    /// 에크만의 6대 기본 감정 + 분류 안 되는 입력을 위한 기본값(Confused) + 황당/어이없음(Bewildered).
    /// Confused = 당황(불안/난처), Bewildered = 황당(어이없음/기가 참) — 캐릭터 반응의 결이 달라 별도 상태로 분리.
    /// </summary>
    public enum EmotionState
    {
        Anger,
        Disgust,
        Fear,
        Joy,
        Sadness,
        Surprise,
        Confused,
        Bewildered
    }
}
