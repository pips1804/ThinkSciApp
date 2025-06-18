using UnityEngine;

public class BreathingAnimation : MonoBehaviour
{
    public float bounceAmount = 0.05f;       // Size of bounce
    public float bounceSpeed = 2f;           // Speed of bounce

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Use Mathf.Sin to create a rhythmic bounce
        float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;

        // Jolly squish/stretch: scale X and Y in opposite directions
        float scaleX = originalScale.x + bounce;
        float scaleY = originalScale.y - bounce;

        transform.localScale = new Vector3(scaleX, scaleY, originalScale.z);
    }
}
