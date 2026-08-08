using UnityEngine;

namespace Persuasion.Audio
{
    /// <summary>
    /// 스테이지 순서(stageOrder = CurrentStageIndex)별 BGM을 담는 라이브러리.
    /// AudioSetupTool 로 자동 생성/배선된다.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Persuasion/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Tooltip("오프닝 / 스테이지 선택 화면 테마")]
        public AudioClip openingTheme;

        [Tooltip("각 스테이지 도입컷/전환컷 사운드 (index = stageOrder)")]
        public AudioClip[] introClips;

        [Tooltip("각 스테이지 진행 중 BGM (index = stageOrder)")]
        public AudioClip[] playClips;

        [Tooltip("각 스테이지 캐릭터가 말할 때 재생되는 음성 효과음 (index = stageOrder, 없으면 null)")]
        public AudioClip[] voiceClips;

        public AudioClip GetIntro(int stageIndex) => Get(introClips, stageIndex);
        public AudioClip GetPlay(int stageIndex) => Get(playClips, stageIndex);
        public AudioClip GetVoice(int stageIndex) => Get(voiceClips, stageIndex);

        private static AudioClip Get(AudioClip[] arr, int i)
            => (arr != null && i >= 0 && i < arr.Length) ? arr[i] : null;
    }
}
