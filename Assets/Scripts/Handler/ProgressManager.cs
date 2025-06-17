using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public DatabaseManager dbManager; // Assign in Inspector
    public CategoryLocker categoryLocker; // Optional: to recheck and update buttons
    public LessonLocker lessonLocker;
    public int currentUserId = 1;

    public void UnlockCategory(int categoryId)
    {
        dbManager.UnlockCategoryForUser(currentUserId, categoryId);

        // Optional: Immediately refresh UI
        categoryLocker.RefreshCategoryLocks(); // re-call Start to recheck unlocks (simple but works)
    }

    public void UnlockLesson(int lessonId)
    {
        dbManager.UnlockLessonForUser(currentUserId, lessonId);

        // Optional: Immediately refresh UI
        lessonLocker.RefreshLessonLocks(); // re-call Start to recheck unlocks (simple but works)
    }
}
