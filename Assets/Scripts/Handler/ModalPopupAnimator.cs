using UnityEngine;

public class ModalPopupAnimator : MonoBehaviour
{
    public float animationDuration = 0.3f;
    private bool isFirstActivation = true;

    void Start()
    {
            // Prevent animation on scene start if needed
            if (isFirstActivation)
            {
                isFirstActivation = false;
                transform.localScale = Vector3.one;
                return;
            }

            // Start from 0 scale and animate in
            transform.localScale = Vector3.zero;
            LeanTween.scale(gameObject, Vector3.one, animationDuration).setEaseOutBack();

    }
    void OnEnable()
    {
        // Prevent animation on scene start if needed
        if (isFirstActivation)
        {
            isFirstActivation = false;
            transform.localScale = Vector3.one;
            return;
        }

        // Start from 0 scale and animate in
        transform.localScale = Vector3.zero;
        LeanTween.scale(gameObject, Vector3.one, animationDuration).setEaseOutBack();
    }

    void OnDisable()
    {
        // Optional: animate before fully hiding if you control deactivation manually
         LeanTween.scale(gameObject, Vector3.zero, animationDuration).setEaseInBack();
    }
}
