using UnityEngine;
using UnityEngine.UI;

public class CategoryLocker : MonoBehaviour
{
    public Button categoryTwoButton;
    public Button categoryThreeButton;
    public Button categoryFourButton;

    void Start()
    {
        // Initially lock categories
        LockCategory(categoryTwoButton);
        LockCategory(categoryThreeButton);
        LockCategory(categoryFourButton);
    }

    // Lock a category
    public void LockCategory(Button button)
    {
        button.interactable = false;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.9f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Optional: Show lock icon
        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(true);
    }

    //  Unlock a category
    public void UnlockCategory(Button button)
    {
        button.interactable = true;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Optional: Hide lock icon
        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(false);
    }
}
