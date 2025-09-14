using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LessonLocker : MonoBehaviour
{
    public DatabaseManager dbManager;
    public int currentUserId = 1;

    // Manually assign each lesson button by Lesson ID in the inspector
    public Button[] lessonButtons; // Index 0 = Lesson ID 1, Index 1 = Lesson ID 2, etc.

    void Start()
    {
        RefreshLessonLocks();
    }

    public void RefreshLessonLocks()
    {
        List<LessonUnlockData> unlockData = dbManager.GetLessonUnlockData(currentUserId);

        foreach (var data in unlockData)
        {
            int index = data.LessonID - 1; // array index starts at 0
            if (index >= 0 && index < lessonButtons.Length)
            {
                Button lessonButton = lessonButtons[index];

                // Check if user can unlock based on required items
                bool canUnlock = dbManager.CanUnlockLesson(currentUserId, data.LessonID);

                if (data.IsUnlocked == 1 && canUnlock)
                    UnlockLesson(lessonButton);
                else
                    LockLesson(lessonButton);
            }
        }
    }

    public void LockLesson(Button button)
    {
        button.interactable = false;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.9f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(true);
    }

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

        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(false);
    }
}
