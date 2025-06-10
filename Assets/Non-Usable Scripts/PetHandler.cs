using UnityEngine;
using UnityEngine.UI;

public class PetLocker : MonoBehaviour
{
    // Category One Lessons
    public Button MotionPet;
    public Button HeatPet;
    public Button EnergyPet;

    // Category Two Lessons


    void Start()
    {
        // Initially lock lessons
        // For Category One
        LockPet(MotionPet);
        LockPet(HeatPet);
        LockPet(EnergyPet);
    }

    // Lock a category
    public void LockPet(Button button)
    {
        button.interactable = false;

        // Fade the whole button
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.9f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Fade Icon and Name
        FadeChild(button.transform, "Icon", 0.5f);
        FadeChild(button.transform, "Name", 0.5f);

        // Optional: Show lock icon
        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(true);
    }

    public void UnlockPet(Button button)
    {
        button.interactable = true;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Restore Icon and Name opacity
        FadeChild(button.transform, "Icon", 1f);
        FadeChild(button.transform, "Name", 1f);

        // Optional: Hide lock icon
        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(false);
    }

    private void FadeChild(Transform parent, string childName, float alpha)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            // Fade Image
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }

            // Fade Text (UI Text)
            Text text = child.GetComponent<Text>();
            if (text != null)
            {
                Color c = text.color;
                c.a = alpha;
                text.color = c;
            }

            // Fade Text (TMP)
            TMPro.TMP_Text tmp = child.GetComponent<TMPro.TMP_Text>();
            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = alpha;
                tmp.color = c;
            }
        }
    }

    /* 
     * Unlocking a Lesson or Category
     * public CategoryLocker categoryLocker;

        void CheckPlayerProgress() {
            if (playerLevel >= 5) {
                categoryLocker.UnlockCategory(categoryLocker.categoryTwoButton);
            }
        }
     */
}
