using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingPet : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas; // Reference to your Canvas

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Optional: Add behavior when dragging starts
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        float screenWidth = canvas.GetComponent<RectTransform>().sizeDelta.x;

        // Snap to left or right
        if (anchoredPosition.x < 0)
        {
            anchoredPosition.x = -screenWidth / 2 + rectTransform.sizeDelta.x / 2;
        }
        else
        {
            anchoredPosition.x = screenWidth / 2 - rectTransform.sizeDelta.x / 2;
        }

        rectTransform.anchoredPosition = anchoredPosition;
    }
}
