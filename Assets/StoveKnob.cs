using UnityEngine;
using UnityEngine.EventSystems;

public class StoveKnob : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public ParticleManager particleManager; // Reference to your ParticleManager
    public float minAngle = -45f;  // fully left (cool)
    public float maxAngle = 45f;   // fully right (heat)

    private RectTransform rectTransform;
    private float currentAngle;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        ResetToDefaults();
    }

    public void ResetToDefaults()
    {
        // Start at maxAngle (right side = cool) - based on your original logic
        currentAngle = maxAngle;
        rectTransform.localRotation = Quaternion.Euler(0, 0, currentAngle);

        if (particleManager != null)
            particleManager.CoolDown(); // Start in cool state
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 dir = eventData.position - (Vector2)rectTransform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Adjust angle so the knob rotates naturally
        currentAngle = Mathf.Clamp(angle - 90f, minAngle, maxAngle);
        rectTransform.localRotation = Quaternion.Euler(0, 0, currentAngle);

        // Change heat state depending on knob direction
        // Based on your original logic: right side (maxAngle) = cool, left side (minAngle) = heat
        if (currentAngle > (minAngle + maxAngle) / 2f)
        {
            if (particleManager != null) particleManager.CoolDown();
        }
        else
        {
            if (particleManager != null) particleManager.HeatUp();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Snap to closest side when touch is released
        if (currentAngle > (minAngle + maxAngle) / 2f)
        {
            // Snap to cool side (right)
            currentAngle = maxAngle;
            rectTransform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            if (particleManager != null) particleManager.CoolDown();
        }
        else
        {
            // Snap to heat side (left)
            currentAngle = minAngle;
            rectTransform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            if (particleManager != null) particleManager.HeatUp();
        }
    }
}
