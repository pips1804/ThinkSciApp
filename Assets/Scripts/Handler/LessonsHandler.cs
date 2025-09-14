using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LessonLocker : MonoBehaviour
{
    public DatabaseManager dbManager;
    public int currentUserId = 1;

    [Header("Unlock Modals")]
    public GameObject unlockConfirmPanel;
    public Text unlockConfirmText;
    public Button unlockYesButton;
    public Button unlockNoButton;

    public GameObject unlockResultPanel;
    public Text unlockResultText;
    public Text unlockResultHeaderText;
    public Button unlockResultCloseButton;


    // Manually assign each lesson button by Lesson ID in the inspector
    public Button[] lessonButtons; // Index 0 = Lesson ID 1, Index 1 = Lesson ID 2, etc.

    void Start()
    {
        RefreshLessonLocks();
        unlockConfirmPanel.SetActive(false);
        unlockResultPanel.SetActive(false);
        unlockNoButton.onClick.AddListener(() => unlockConfirmPanel.SetActive(false));
        unlockResultCloseButton.onClick.AddListener(() => unlockResultPanel.SetActive(false));

        for (int i = 0; i < lessonButtons.Length; i++)
        {
            int lessonId = i + 1; // button index 0 = LessonID 1
            lessonButtons[i].onClick.AddListener(() => TryUnlockLesson(lessonId));
        }
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
                if (data.IsUnlocked == 1)
                    UnlockLesson(lessonButton);
                else
                    LockLesson(lessonButton);
            }
        }
    }

    public void LockLesson(Button button)
    {
        // Keep the button clickable (don’t disable it!)
        button.interactable = true;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.7f; // faded look
        canvasGroup.interactable = true;   // still clickable
        canvasGroup.blocksRaycasts = true;

        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(true);

        // 🔹 Disable PanelSwitcher so it won’t swap panels
        PanelSwitcher ps = button.GetComponent<PanelSwitcher>();
        if (ps != null)
            ps.enabled = false;

        // 🔹 Redirect click to TryUnlockLesson instead
        button.onClick.RemoveAllListeners();
        int lessonId = System.Array.IndexOf(lessonButtons, button) + 1;
        button.onClick.AddListener(() => TryUnlockLesson(lessonId));
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

        // Enable PanelSwitcher
        PanelSwitcher ps = button.GetComponent<PanelSwitcher>();
        if (ps != null)
            ps.enabled = true;

        // ⚡ Replace inspector click with code-driven click
        button.onClick.RemoveAllListeners();

        int lessonId = System.Array.IndexOf(lessonButtons, button) + 1;
        button.onClick.AddListener(() =>
        {
            ps.ActivatePanel(); // explicitly switch now
        });
    }

    public void TryUnlockLesson(int lessonId)
    {
        int? requiredItemId = dbManager.GetRequiredCollectibleForLesson(lessonId);
        string itemName = requiredItemId.HasValue ? dbManager.GetItemName(requiredItemId.Value) : "None";

        // Show confirmation modal
        unlockConfirmText.text = requiredItemId.HasValue
            ? $"Unlock Lesson {lessonId}? Requires {itemName}."
            : $"Unlock Lesson {lessonId}?";

        unlockConfirmPanel.SetActive(true);

        unlockYesButton.onClick.RemoveAllListeners();
        unlockYesButton.onClick.AddListener(() =>
        {
            unlockConfirmPanel.SetActive(false);

            bool hasCollectible = requiredItemId == null || dbManager.HasCollectible(currentUserId, requiredItemId.Value);

            if (hasCollectible)
            {
                dbManager.UnlockLessonForUser(currentUserId, lessonId); // update your User_Lesson_Unlocks table
                unlockResultHeaderText.text = $"Success!";
                unlockResultText.text = $"Lesson {lessonId} unlocked!";

                RefreshLessonLocks();
            }
            else
            {
                unlockResultHeaderText.text = $"Failed!";
                unlockResultText.text = $"Cannot unlock. You need {itemName}.";
            }

            unlockResultPanel.SetActive(true);
        });
    }

}
