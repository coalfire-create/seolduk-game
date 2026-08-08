using System;
using UnityEngine;
using UnityEngine.Events;
using Persuasion.AI;
using Persuasion.Core;

namespace Persuasion.Character
{
    [Serializable] public class EmotionChangedEvent : UnityEvent<EmotionState> { }

    /// <summary>
    /// 캐릭터(Ready Player Me, Mixamo 등 아무 리그나) 오브젝트에 붙이는 컴포넌트.
    /// PersuasionManager의 NPC 응답을 구독해서 감정/제스처를 실시간으로 노출한다.
    ///
    /// 사용법:
    /// - Animator를 쓴다면: OnEmotionChanged(UnityEvent)에 Animator.SetInteger 등을 인스펙터에서 바로 연결
    /// - 블렌드셰이프를 쓴다면: 이 스크립트를 상속하거나 OnEmotionChanged를 구독하는 별도 스크립트에서
    ///   SkinnedMeshRenderer.SetBlendShapeWeight(...)로 감정별 표정 가중치를 보간
    /// </summary>
    public class CharacterEmotionDisplay : MonoBehaviour
    {
        public EmotionState CurrentEmotion { get; private set; } = EmotionState.Confused;
        public string CurrentGesture { get; private set; }

        [Tooltip("감정이 바뀔 때마다 호출됨. 인스펙터에서 Animator/블렌드셰이프 로직 연결.")]
        public EmotionChangedEvent OnEmotionChanged;

        private void OnEnable()
        {
            if (PersuasionManager.Instance != null)
                PersuasionManager.Instance.OnNPCReplied += HandleNPCReplied;
        }

        private void OnDisable()
        {
            if (PersuasionManager.Instance != null)
                PersuasionManager.Instance.OnNPCReplied -= HandleNPCReplied;
        }

        private void HandleNPCReplied(NPCResponse response)
        {
            if (Enum.TryParse(response.emotion, true, out EmotionState parsed))
                CurrentEmotion = parsed;
            else
                CurrentEmotion = EmotionState.Confused;

            CurrentGesture = response.gesture;
            OnEmotionChanged?.Invoke(CurrentEmotion);
        }
    }
}
