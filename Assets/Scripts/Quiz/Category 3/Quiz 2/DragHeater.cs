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
            // move it slightly higher (adjust 30f as needed)
            Vector3 snapPos = targetPoint.position + new Vector3(0, 90f, 0);
            transform.position = snapPos;

            isLocked = true; // Lock so it can't be moved again
            gameManager.HeaterPlacedSuccess();
        }

    }

    public void ResetHeater(Vector3 startPosition)
    {
        transform.position = startPosition;
        isLocked = false;
    }
}
