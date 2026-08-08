using System;
using UnityEngine;

namespace Persuasion
{
    /// <summary>한 스테이지(캐릭터 1명)의 감정별 초상화 세트.</summary>
    [Serializable]
    public class StagePortraitSet
    {
        [Tooltip("StageData.stageId 와 동일해야 함")]
        public string stageId;

        public Sprite introSprite;  // 캐릭터 소개 일러스트 (수사파일 사진, StoryBeat 표지용)
        public Sprite baseSprite;   // 기본 표정 (스테이지 시작 / 감정 이미지 없을 때 폴백)
        public Sprite anger;
        public Sprite disgust;
        public Sprite fear;
        public Sprite joy;
        public Sprite sadness;
        public Sprite surprise;
        public Sprite confused;
        public Sprite bewildered;

        /// <summary>감정에 해당하는 스프라이트. 없으면 기본 표정으로 폴백.</summary>
        public Sprite Get(EmotionState emotion)
        {
            Sprite s = null;
            switch (emotion)
            {
                case EmotionState.Anger:    s = anger;    break;
                case EmotionState.Disgust:  s = disgust;  break;
                case EmotionState.Fear:     s = fear;     break;
                case EmotionState.Joy:      s = joy;      break;
                case EmotionState.Sadness:  s = sadness;  break;
                case EmotionState.Surprise: s = surprise; break;
                case EmotionState.Confused:   s = confused;   break;
                case EmotionState.Bewildered: s = bewildered; break;
            }
            return s != null ? s : baseSprite;
        }
    }

    /// <summary>
    /// 스테이지별 캐릭터 초상화 모음. 메뉴 Persuasion > 2) Build Portrait Library 로 자동 생성.
    /// 이미지가 있는 스테이지만 등록되며, 없는 스테이지는 CharacterPortrait가 배경을 숨긴다.
    /// </summary>
    [CreateAssetMenu(fileName = "StagePortraitLibrary", menuName = "Persuasion/Stage Portrait Library")]
    public class StagePortraitLibrary : ScriptableObject
    {
        public StagePortraitSet[] sets;

        public StagePortraitSet Find(string stageId)
        {
            if (sets == null || string.IsNullOrEmpty(stageId)) return null;
            foreach (var s in sets)
                if (s != null && s.stageId == stageId) return s;
            return null;
        }
    }
}
