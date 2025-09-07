// PetController.cs - Controls individual pet behavior (UI Image only version)
using UnityEngine;
using UnityEngine.UI;

public class PetController : MonoBehaviour
{
    public int forceValue = 1;
    public bool isLeft = true;

    private Image petImage;
    private RectTransform petRect;
    private bool isPulling = false;
    private Vector2 originalAnchoredPosition;
    private Color originalColor;

    private void Start()
    {
        petImage = GetComponent<Image>();
        petRect = GetComponent<RectTransform>();

        if (petRect != null)
        {
            originalAnchoredPosition = petRect.anchoredPosition;
        }

        if (petImage != null)
        {
            originalColor = petImage.color;

            // Flip image if on right side (UI Image version)
            // if (!isLeft)
            // {
            //     Vector3 scale = transform.localScale;
            //     scale.x = -scale.x;
            //     transform.localScale = scale;
            // }
        }
    }

    public void StartPulling()
    {
        isPulling = true;

        // Visual feedback for pulling - UI movement and color change
        StartCoroutine(SimpleUIAnimation());
    }

    public void StopPulling()
    {
        isPulling = false;

        // Reset to original state
        if (petImage != null)
        {
            petImage.color = originalColor;
        }
        if (petRect != null)
        {
            petRect.anchoredPosition = originalAnchoredPosition;
        }
    }

    private System.Collections.IEnumerator SimpleUIAnimation()
    {
        float pullDistance = 5f; // UI units for wiggle animation
        float pullDirection = isLeft ? -1 : 1;
        Color pullingColor = Color.yellow; // Highlight color when pulling

        while (isPulling)
        {
            // Simple back and forth movement using UI anchored position
            if (petRect != null)
            {
                float offset = pullDistance * pullDirection * Mathf.Sin(Time.time * 3f);
                petRect.anchoredPosition = originalAnchoredPosition + Vector2.right * offset;
            }

            // Color lerping for visual effect
            if (petImage != null)
            {
                float colorLerp = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                petImage.color = Color.Lerp(originalColor, pullingColor, colorLerp * 0.3f);
            }

            yield return null;
        }

        // Reset position and color
        if (petRect != null)
        {
            petRect.anchoredPosition = originalAnchoredPosition;
            if (petImage != null)
            {
                petImage.color = originalColor;
            }
        }
    }

}
