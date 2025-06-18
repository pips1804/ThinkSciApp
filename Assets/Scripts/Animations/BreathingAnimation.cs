using UnityEngine;

public class BreathingAnimation : MonoBehaviour
{
    public float bounceAmount = 0.05f;       // How much the UI bounces
    public float bounceSpeed = 2f;           // Speed of the breathing cycle
    public float verticalJumpAmount = 5f;    // UI jump height in units (pixels)

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector2 originalAnchoredPos;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        originalAnchoredPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;

        // Jolly bounce: squish/stretch X and Y
        float scaleX = originalScale.x + bounce;
        float scaleY = originalScale.y - bounce;
        rectTransform.localScale = new Vector3(scaleX, scaleY, originalScale.z);

        // Slight vertical hop
        float jump = Mathf.Sin(Time.time * bounceSpeed) * verticalJumpAmount;
        rectTransform.anchoredPosition = new Vector2(originalAnchoredPos.x, originalAnchoredPos.y + jump);
    }
}
