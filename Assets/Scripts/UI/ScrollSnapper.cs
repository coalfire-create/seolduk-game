using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Persuasion.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollSnapper : MonoBehaviour, IEndDragHandler, IBeginDragHandler
    {
        private ScrollRect scrollRect;
        private RectTransform content;
        private float[] snapPositions;
        
        [SerializeField] private float snapSpeed = 10f;
        [SerializeField] private float swipeThreshold = 50f;
        
        private int currentTargetIndex = 0;
        private bool isSnapping = false;
        private bool isDragging = false;
        private float startDragPosition;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            content = scrollRect.content;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
        }

        private void Start()
        {
            CalculateSnapPositions();
        }

        private void CalculateSnapPositions()
        {
            if (content.childCount == 0) return;
            
            // Force rebuild just in case layout isn't ready
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            snapPositions = new float[content.childCount];
            float contentWidth = content.rect.width;
            float viewportWidth = scrollRect.viewport.rect.width;
            float maxScrollPosition = contentWidth - viewportWidth;
            
            if (contentWidth <= viewportWidth || maxScrollPosition <= 0)
            {
                // No scrolling needed
                for (int i = 0; i < snapPositions.Length; i++) snapPositions[i] = 0;
                return;
            }

            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform child = content.GetChild(i) as RectTransform;
                // Calculate normalized scroll position for this child to be centered
                float childCenter = child.anchoredPosition.x + (child.rect.width * 0.5f);
                float viewportCenter = viewportWidth * 0.5f;
                float requiredScrollPosition = childCenter - viewportCenter;
                
                // Clamp to [0, 1]
                float normalizedPosition = requiredScrollPosition / maxScrollPosition;
                snapPositions[i] = Mathf.Clamp01(normalizedPosition);
            }
        }

        private void Update()
        {
            if (isSnapping && !isDragging)
            {
                if (snapPositions == null || snapPositions.Length == 0) return;
                
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                    scrollRect.horizontalNormalizedPosition, 
                    snapPositions[currentTargetIndex], 
                    Time.deltaTime * snapSpeed
                );
                
                if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - snapPositions[currentTargetIndex]) < 0.001f)
                {
                    scrollRect.horizontalNormalizedPosition = snapPositions[currentTargetIndex];
                    isSnapping = false;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            isSnapping = false;
            startDragPosition = eventData.position.x;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            if (snapPositions == null || snapPositions.Length == 0) return;
            
            float dragDistance = eventData.position.x - startDragPosition;
            
            if (Mathf.Abs(dragDistance) > swipeThreshold)
            {
                if (dragDistance < 0) // Swiped left, go to next
                    currentTargetIndex = Mathf.Min(currentTargetIndex + 1, snapPositions.Length - 1);
                else // Swiped right, go to previous
                    currentTargetIndex = Mathf.Max(currentTargetIndex - 1, 0);
            }
            else
            {
                // Find nearest
                float currentPos = scrollRect.horizontalNormalizedPosition;
                float minDistance = float.MaxValue;
                int nearestIndex = 0;
                
                for (int i = 0; i < snapPositions.Length; i++)
                {
                    float dist = Mathf.Abs(currentPos - snapPositions[i]);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestIndex = i;
                    }
                }
                currentTargetIndex = nearestIndex;
            }
            
            isSnapping = true;
            scrollRect.velocity = Vector2.zero; // Stop standard inertia
        }
    }
}
