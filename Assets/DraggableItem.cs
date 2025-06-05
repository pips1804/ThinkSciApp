using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;

    public string hatSlotName = "HatSlot";
    public string shadesSlotName = "ShadesSlot";
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
        Debug.Log("Begin Drag");
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
            Debug.LogError("Player not found!");
            return;
        }

        string selectedSlotName = isHat ? hatSlotName : shadesSlotName;
        Transform targetSlot = player.transform.Find(selectedSlotName);
        if (targetSlot == null)
        {
            Debug.LogError("Target slot not found: " + selectedSlotName);
            return;
        }

        RectTransform slotRect = targetSlot.GetComponent<RectTransform>();
        if (slotRect == null)
        {
            Debug.LogError("Target slot does not have a RectTransform!");
            return;
        }

        Vector2 slotScreenPos = slotRect.position;

        float dist = Vector2.Distance(eventData.position, slotScreenPos);
        Debug.Log($"Drop Pos: {eventData.position} | Slot: {slotScreenPos} | Dist: {dist}");

        if (dist < 100f)
        {
            // Snap to slot
            transform.SetParent(targetSlot, false);
            GetComponent<RectTransform>().localPosition = Vector3.zero;

            if (isHat)
            {
                PlayerEquipData.Instance.isHatEquipped = true;
                PlayerEquipData.Instance.equippedHatPrefab = gameObject;
            }
            else
            {
                PlayerEquipData.Instance.isGlassesEquipped = true;
                PlayerEquipData.Instance.equippedShadesPrefab = gameObject;
            }

        }
        else
        {
            // Return to original
            rectTransform.anchoredPosition = originalPosition;
            Debug.Log("Too far from slot. Returning.");
        }
    }
}
