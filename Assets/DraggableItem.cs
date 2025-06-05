using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;

    public string targetSlotName = "HatSlot";
    public bool isHat = true;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError(" Player not found!");
            return;
        }

        Transform targetSlot = player.transform.Find(targetSlotName);
        if (targetSlot == null)
        {
            Debug.LogError(" Target slot not found: " + targetSlotName);
            return;
        }

        Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, targetSlot.position);
        float dist = Vector2.Distance(eventData.position, slotScreenPos);
        Debug.Log($" Drop Pos: {eventData.position} | Slot: {slotScreenPos} | Dist: {dist}");

        if (dist < 100f)
        {
            // Snap to slot
            transform.SetParent(targetSlot, false);
            GetComponent<RectTransform>().localPosition = Vector3.zero;

            if (isHat)
                PlayerEquipData.Instance.isHatEquipped = true;
            else
                PlayerEquipData.Instance.isGlassesEquipped = true;

            Debug.Log(" Equipped: " + gameObject.name);
        }
        else
        {
            // Return to original
            rectTransform.anchoredPosition = originalPosition;
            Debug.Log(" Too far from slot. Returning.");
        }
    }




}
