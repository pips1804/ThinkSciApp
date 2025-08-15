using UnityEngine;
using UnityEngine.EventSystems;

public class DragFire : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform targetPoint; // Rod end position
    public float snapDistance = 50f;
    public HeatTheMetal gameManager;

    private bool isLocked = false; // Prevent dragging after success

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return; // Don't allow dragging after locking
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return; // Don't allow releasing after locking

        float distance = Vector2.Distance(transform.position, targetPoint.position);
        if (distance < snapDistance)
        {
            transform.position = targetPoint.position;
            isLocked = true; // Lock it so it can't be moved again
            gameManager.FirePlacedSuccess();
        }
    }
}
