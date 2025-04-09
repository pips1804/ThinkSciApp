using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VerticalSnapScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public float snapSpeed = 10f;
    public float snapThreshold = 0.05f;

    private float[] cardPositions;
    private bool isDragging = false;

    void Start()
    {
        SetCardPositions();
    }

    void Update()
    {
        if (!isDragging && cardPositions.Length > 0)
        {
            float closestPos = FindClosest(scrollRect.verticalNormalizedPosition);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(
                scrollRect.verticalNormalizedPosition,
                closestPos,
                Time.deltaTime * snapSpeed
            );
        }
    }

    void SetCardPositions()
    {
        int cardCount = contentPanel.childCount;
        cardPositions = new float[cardCount];

        if (cardCount <= 1) return;

        for (int i = 0; i < cardCount; i++)
        {
            float step = 1f / (cardCount - 1); // ensures last one is at 0
            cardPositions[i] = 1f - (step * i);
        }
    }

    float FindClosest(float current)
    {
        float closest = cardPositions[0];
        float minDist = Mathf.Abs(current - closest);

        foreach (float pos in cardPositions)
        {
            float dist = Mathf.Abs(current - pos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = pos;
            }
        }
        return closest;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
}
