using UnityEngine;
using UnityEngine.EventSystems;

public class DragFire : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform targetPoint; // Rod end position
    public float snapDistance = 50f;
    public HeatTheMetal gameManager;

    private bool isLocked = false; // Prevent dragging after success
    private Vector3 startPosition; // NEW: Store starting position
    private bool startPositionStored = false; // NEW: Track if we've stored the position

    void Start()
    {
        // NEW: Store the starting position when the game starts
        if (!startPositionStored)
        {
            startPosition = transform.position;
            startPositionStored = true;
            Debug.Log($"DragFire: Stored start position: {startPosition}");
        }
    }

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
            // Add offset so fire sits below the rod
            Vector3 snapPos = targetPoint.position + new Vector3(0, -140f, 0);
            transform.position = snapPos;

            isLocked = true; // Lock it so it can't be moved again
            gameManager.FirePlacedSuccess();
        }
    }

    // NEW: Method to reset the fire to its original position
    public void ResetFire(Vector3 resetPosition)
    {
        transform.position = resetPosition;
        isLocked = false; // Allow dragging again
        Debug.Log($"DragFire: Reset to position {resetPosition}, unlocked for dragging");
    }

    // NEW: Alternative reset method that uses stored start position
    public void ResetFireToStart()
    {
        if (startPositionStored)
        {
            ResetFire(startPosition);
        }
        else
        {
            Debug.LogWarning("DragFire: Start position not stored, cannot reset");
        }
    }

    // NEW: Method to get the stored start position (useful for debugging)
    public Vector3 GetStartPosition()
    {
        return startPosition;
    }

    // NEW: Method to check if the fire is currently locked
    public bool IsLocked()
    {
        return isLocked;
    }
}
