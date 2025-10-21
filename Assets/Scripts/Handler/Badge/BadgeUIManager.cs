using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BadgeUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform badgeContainer;
    public GameObject badgeCardPrefab;

    public Button inProgressButton;
    public Button doneButton;

    private List<Badge> allBadges = new List<Badge>();
    private int currentUserId = 1;

    public DatabaseManager databaseManager;

    public MainSceneUI mainmenu;
    public AudioClip coin;
    public GameObject noBadgesText;


    void Start()
    {
        RefreshAndShowInProgress();

        inProgressButton.onClick.AddListener(ShowInProgress);
        doneButton.onClick.AddListener(ShowDone);
    }

    void OnEnable()
    {
        RefreshAndShowInProgress();
    }

    void SetTabState(bool showInProgress)
    {
        inProgressButton.interactable = !showInProgress;
        doneButton.interactable = showInProgress;
    }

    void LoadBadgesFromDatabase()
    {
        allBadges = databaseManager.GetUserBadges(currentUserId);
        Debug.Log("Loaded badges from DB: " + allBadges.Count);
    }


    private void RefreshAndShowInProgress()
    {
        LoadBadgesFromDatabase();
        ShowInProgress();
    }

    public void RefreshBadges()
    {
        databaseManager.CheckAndUnlockBadges(1);
    }

    void ShowInProgress()
    {
        LoadBadgesFromDatabase();
        PopulateBadgeUI(allBadges.FindAll(b => !b.IsUnlocked || (b.IsUnlocked && !b.IsClaimed)));
        SetTabState(true); // highlight In Progress tab
    }

    void ShowDone()
    {
        LoadBadgesFromDatabase();
        PopulateBadgeUI(allBadges.FindAll(b => b.IsUnlocked && b.IsClaimed));
        SetTabState(false); // highlight Done tab
    }

    void PopulateBadgeUI(List<Badge> badgeList)
    {
        foreach (Transform child in badgeContainer)
            Destroy(child.gameObject);

        foreach (var badge in badgeList)
        {
            GameObject card = Instantiate(badgeCardPrefab, badgeContainer);

            // Set badge info
            card.transform.Find("NameText").GetComponent<Text>().text = badge.Name;
            card.transform.Find("DescriptionText").GetComponent<Text>().text = badge.Description;

            var slider = card.transform.Find("ProgressBar").GetComponent<Slider>();

            if (badge.TargetProgress > 0)
            {
                slider.value = (float)badge.CurrentProgress / badge.TargetProgress;
            }
            else
            {
                slider.value = 0f;
            }

            // Claimable icon logic
            Transform coinIcon = card.transform.Find("ClaimableIcon");
            bool isClaimable = badge.IsUnlocked && !badge.IsClaimed;
            if (coinIcon != null) coinIcon.gameObject.SetActive(isClaimable);

            // Card button
            var cardButton = card.GetComponent<Button>();
            int badgeID = badge.BadgeID;

            if (isClaimable)
            {
                // Only add listener when badge is ready
                cardButton.onClick.AddListener(() => ClaimBadge(badgeID));
            }
            else
            {
                // Optional: Add a popup saying "Finish task to unlock"
                cardButton.onClick.AddListener(() => Debug.Log("Badge not ready yet"));
            }

            // Optional: always look active
            cardButton.interactable = true;
        }

        if (noBadgesText != null)
            noBadgesText.SetActive(badgeList.Count == 0);
        
        Debug.Log("Badge Count: " + badgeList.Count);
    }

    void ClaimBadge(int badgeID)
    {
        int rewardGold = 50; // Or make it dynamic per badge
        databaseManager.ClaimBadge(currentUserId, badgeID, rewardGold);
        AudioManager.Instance.PlaySFX(coin);
        LoadBadgesFromDatabase();
        ShowDone();
        mainmenu.UpdateUI();
    }

    public void RefreshUI()
    {
        LoadBadgesFromDatabase();
        // Keep the same tab the user was on:
        if (inProgressButton.interactable == false) // means InProgress tab is active
            ShowInProgress();
        else
            ShowDone();
    }
}
