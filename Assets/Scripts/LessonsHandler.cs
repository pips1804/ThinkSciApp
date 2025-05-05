using UnityEngine;
using UnityEngine.UI;

public class LessonLocker : MonoBehaviour
{
    // Category One Lessons
    public Button catOneLessonTwoButton;
    public Button catOneLessonThreeButton;
    public Button catOneLessonFourButton;

    // Category Two Lessons
    public Button catTwoLessonTwoButton;
    public Button catTwoLessonThreeButton;
    public Button catTwoLessonFourButton;

    // Category Three Lessons
    public Button catThreeLessonTwoButton;
    public Button catThreeLessonThreeButton;
    public Button catThreeLessonFourButton;

    // Category Four Lessons
    public Button catFourLessonTwoButton;
    public Button catFourLessonThreeButton;
    public Button catFourLessonFourButton;
    public Button catFourLessonFiveButton;

    void Start()
    {
        // Initially lock lessons
        // For Category One
        LockLesson(catOneLessonTwoButton);
        LockLesson(catOneLessonThreeButton);
        LockLesson(catOneLessonFourButton);

        // For Category Two
        LockLesson(catTwoLessonTwoButton);
        LockLesson(catTwoLessonThreeButton);
        LockLesson(catTwoLessonFourButton);

        // For Category Three
        LockLesson(catThreeLessonTwoButton);
        LockLesson(catThreeLessonThreeButton);
        LockLesson(catThreeLessonFourButton);

        // For Category Four
        LockLesson(catFourLessonTwoButton);
        LockLesson(catFourLessonThreeButton);
        LockLesson(catFourLessonFourButton);
        LockLesson(catFourLessonFiveButton);
    }

    // Lock a category
    public void LockLesson(Button button)
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
    public void UnlockLesson(Button button)
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
