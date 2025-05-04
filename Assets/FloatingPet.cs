using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FloatingPet : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public static FloatingPet Instance;
    private RectTransform rectTransform;
    private Canvas canvas; // Reference to your Canvas

    public RectTransform hintBubble; // Bubble RectTransform
    public Image bubbleImage; // The UI Image component
    public Sprite bubbleRightSprite; // Bubble tail pointing right
    public Sprite bubbleLeftSprite;  // Bubble tail pointing left

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

        UpdateBubbleSide();
    }

    void UpdateBubbleSide()
    {
        float screenWidth = canvas.GetComponent<RectTransform>().sizeDelta.x;
        float petX = rectTransform.anchoredPosition.x;

        if (petX < 0)
        {
            // Pet is on left  Bubble on right
            hintBubble.pivot = new Vector2(0, 0.5f);
            hintBubble.anchoredPosition = new Vector2(100, 100);

            bubbleImage.sprite = bubbleRightSprite; // use right-tail bubble
        }
        else
        {
            // Pet is on right  Bubble on left
            hintBubble.pivot = new Vector2(1, 0.5f);
            hintBubble.anchoredPosition = new Vector2(-100, 100);

            bubbleImage.sprite = bubbleLeftSprite; // use left-tail bubble
        }
    }



}
