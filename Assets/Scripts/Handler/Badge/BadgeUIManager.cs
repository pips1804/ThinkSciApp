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


    void Start()
    {
        LoadBadgesFromDatabase();
        ShowInProgress();

        inProgressButton.onClick.AddListener(ShowInProgress);
        doneButton.onClick.AddListener(ShowDone);
    }

    void OnEnable()
    {
        LoadBadgesFromDatabase();
        ShowInProgress();
    }

    void LoadBadgesFromDatabase()
    {
        allBadges = databaseManager.GetUserBadges(currentUserId);
        Debug.Log("Loaded badges from DB: " + allBadges.Count);
    }

    public void RefreshBadges()
    { 
        databaseManager.CheckAndUnlockBadges(1);
    }

    void ShowInProgress()
    {
        LoadBadgesFromDatabase();
        PopulateBadgeUI(allBadges.FindAll(b => !b.IsUnlocked || (b.IsUnlocked && !b.IsClaimed)));
    }

    void ShowDone()
    {
        LoadBadgesFromDatabase();
        PopulateBadgeUI(allBadges.FindAll(b => b.IsUnlocked && b.IsClaimed));
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
            slider.value = badge.IsUnlocked ? (badge.IsClaimed ? 1f : 0.5f) : 0f;

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
}
