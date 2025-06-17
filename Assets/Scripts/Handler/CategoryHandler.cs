using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class CategoryLocker : MonoBehaviour
{
    public DatabaseManager dbManager;
    public int currentUserId = 1; // Replace with actual user ID

    public Button categoryOneButton;
    public Button categoryTwoButton;
    public Button categoryThreeButton;
    public Button categoryFourButton;

    void Start()
    {
        RefreshCategoryLocks();
    }

    void ApplyUnlock(Button button, int isUnlocked)
    {
        if (isUnlocked == 1)
            UnlockCategory(button);
        else
            LockCategory(button);
    }


    public void LockCategory(Button button)
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

        Transform lockIcon = button.transform.Find("LockIcon");
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(false);
    }

    public void RefreshCategoryLocks()
    {
        List<CategoryUnlockData> unlockData = dbManager.GetCategoryUnlockData(currentUserId);

        foreach (var data in unlockData)
        {
            switch (data.CategoryID)
            {
                case 1:
                    ApplyUnlock(categoryOneButton, data.IsUnlocked);
                    break;
                case 2:
                    ApplyUnlock(categoryTwoButton, data.IsUnlocked);
                    break;
                case 3:
                    ApplyUnlock(categoryThreeButton, data.IsUnlocked);
                    break;
                case 4:
                    ApplyUnlock(categoryFourButton, data.IsUnlocked);
                    break;
            }
        }
    }

}
