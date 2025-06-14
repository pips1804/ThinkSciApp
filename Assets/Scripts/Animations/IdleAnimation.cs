using UnityEngine;
public class IdleAnimation : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float moveDistance = 10f;
    public float scaleSpeed = 1.2f;
    public float scaleAmount = 0.03f;
    public float rotationSpeed = 1.5f;
    public float rotationAngle = 1.5f;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    private bool isIdleActive = true;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPos = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;
    }

    void Update()
    {
        if (!isIdleActive) return;

        // Horizontal idle movement
        float offsetX = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        rectTransform.anchoredPosition = new Vector2(originalAnchoredPos.x + offsetX, originalAnchoredPos.y);

        // Breathing scale
        float scale = 1 + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
        rectTransform.localScale = new Vector3(scale, scale, 1);

        // Wiggle
        float z = Mathf.Sin(Time.time * rotationSpeed) * rotationAngle;
        rectTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, z);
    }

    public void StopIdle()
    {
        isIdleActive = false;
        rectTransform.anchoredPosition = originalAnchoredPos;
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;
    }

    public void StartIdle()
    {
        isIdleActive = true;
    }
}
