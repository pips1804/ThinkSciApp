using UnityEngine;
using UnityEngine.EventSystems;

public class DragHeater : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform targetPoint;
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
            Debug.Log($"DragHeater: Stored start position: {startPosition}");
        }
    }

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

    // UPDATED: Enhanced reset method with better logging
    public void ResetHeater(Vector3 resetPosition)
    {
        transform.position = resetPosition;
        isLocked = false; // Allow dragging again
        Debug.Log($"DragHeater: Reset to position {resetPosition}, unlocked for dragging");
    }

    // NEW: Alternative reset method that uses stored start position
    public void ResetHeaterToStart()
    {
        if (startPositionStored)
        {
            ResetHeater(startPosition);
        }
        else
        {
            Debug.LogWarning("DragHeater: Start position not stored, cannot reset");
        }
    }

    // NEW: Method to get the stored start position (useful for debugging)
    public Vector3 GetStartPosition()
    {
        return startPosition;
    }

    // NEW: Method to check if the heater is currently locked
    public bool IsLocked()
    {
        return isLocked;
    }
}
