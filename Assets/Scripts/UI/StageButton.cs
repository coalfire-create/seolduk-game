using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Persuasion.UI
{
    /// <summary>
    /// 스테이지 선택 그리드의 버튼 한 칸. 썸네일(캐릭터 기본 이미지) + 제목 + 클릭 콜백.
    /// PersuasionUI가 런타임에 스테이지 수만큼 생성한다.
    /// </summary>
    public class StageButton : MonoBehaviour
    {
        [SerializeField] private Image thumbnail;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text chapterText;
        [SerializeField] private Button button;
        [Tooltip("잠김 상태에서 표시할 어두운 오버레이(자물쇠)")]
        [SerializeField] private GameObject lockOverlay;

        public void Setup(string chapter, string title, Sprite thumb, bool unlocked, Action onClick)
        {
            if (chapterText != null) chapterText.text = chapter;
            if (titleText != null) titleText.text = title;

            if (thumbnail != null)
            {
                thumbnail.sprite = thumb;
                thumbnail.enabled = (thumb != null);
            }

            if (lockOverlay != null) lockOverlay.SetActive(!unlocked);

            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                if (unlocked) button.onClick.AddListener(() => onClick?.Invoke());
            }
        }
    }
}
