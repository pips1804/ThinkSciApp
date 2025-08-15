using UnityEngine;
using UnityEngine.EventSystems;

public class DragHeater : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform targetPoint;
    public float snapDistance = 50f;
    public HeatTheMetal gameManager;

    private bool isLocked = false; // Prevent dragging after success

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return; // Stop dragging after placement
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return; // Stop releasing after placement

        float distance = Vector2.Distance(transform.position, targetPoint.position);
        if (distance < snapDistance)
        {
            transform.position = targetPoint.position;
            isLocked = true; // Lock so it can't be moved again
            gameManager.HeaterPlacedSuccess();
        }
    }

    // Optional method to reset position and unlock
    public void ResetHeater(Vector3 startPosition)
    {
        transform.position = startPosition;
        isLocked = false;
    }
}
