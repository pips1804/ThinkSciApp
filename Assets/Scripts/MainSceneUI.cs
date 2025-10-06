using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class MainSceneUI : MonoBehaviour
{
    [Header("UI References")]
    public Text welcomeText;
    public DatabaseManager dbManager;
    public BadgeUIManager badgeUIManager;
    public Text playerCoinCount;
    public Text playerEnergyCount;
    public Text energyTimerText;
    public Slider healthSlider;
    public Slider damageSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Button storageButton;
    public Button scoresButton;
    public Button badgesButton;
    public GameObject badgesNewDot;
    public GameObject storageNewDot;
    public GameObject scoresNewDot;

    [Header("Energy Button Control")]
    public List<Button> energyButtons; // Assign buttons in the inspector

    public int userID = 1;

    private int currentEnergy; // Track energy for checks
    private DateTime lastEnergyUpdate;
    private Coroutine energyRegenCoroutine;
    void Awake()
    {
        dbManager.UpdateUserEnergy(userID);
        AudioManager.Instance.RegisterBgmSlider(bgmSlider);
        AudioManager.Instance.RegisterSfxSlider(sfxSlider);
    }

    private void OnEnable()
    {
        DatabaseManager.OnUserDataChanged += UpdateUI;
        UpdateUI();

        if (energyRegenCoroutine != null)
            StopCoroutine(energyRegenCoroutine);
        energyRegenCoroutine = StartCoroutine(EnergyTimerLoop());
    }

    private void OnDisable()
    {
        DatabaseManager.OnUserDataChanged -= UpdateUI;
        UpdateUI();
    }

    public void UpdateUI()
    {
        var (userName, coin, energy) = dbManager.GetUser();
        currentEnergy = energy; // Save current energy

        if (welcomeText != null)
            welcomeText.text = $"{userName}!";
        if (playerEnergyCount != null)
            playerEnergyCount.text = $"{energy}";
        if (playerCoinCount != null)
            playerCoinCount.text = $"{coin}";

        UpdateButtonStates();
        UpdateStorageNotification();
        UpdateScoresNotification();
        UpdateBadgesNotification();
        badgeUIManager.RefreshUI();
    }

    private void UpdateButtonStates()
    {
        bool canUse = currentEnergy > 3; // Condition check
        foreach (Button btn in energyButtons)
        {
            if (btn != null)
                btn.interactable = canUse;
        }
    }

    public void AddEnergy(int amount)
    {
        dbManager.AddEnergy(userID, amount);
        UpdateUI();
    }

    public void SpendEnergy(int amount)
    {
        dbManager.SpendEnergy(userID, amount);
        UpdateUI();
    }

    public void UpdateStorageNotification()
    {
        List<ItemData> items = dbManager.GetUserItems(userID);

        // Check if any item has IsNew == true
        bool hasNewItem = items.Any(i => i.IsNew);

        // Show or hide the red dot
        storageNewDot.SetActive(hasNewItem);
    }

    public void UpdateScoresNotification()
    {
        bool hasNewScore = false;

        for (int quizId = 1; quizId <= 14; quizId++)
        {
            var scores = dbManager.GetScoresByQuiz(quizId, userID);
            if (scores.Any(s => s.IsNew))
            {
                hasNewScore = true;
                break; // we can stop once we know there is at least one new score
            }
        }

        scoresNewDot.SetActive(hasNewScore);
    }

    public void UpdateBadgesNotification()
    {
        List<Badge> badges = dbManager.GetUserBadges(userID);

        // Any badge unlocked but not yet claimed?
        bool hasUnclaimed = badges.Any(b => b.IsUnlocked && !b.IsClaimed);

        badgesNewDot.SetActive(hasUnclaimed);
    }
    private IEnumerator EnergyTimerLoop()
    {
        while (true)
        {
            // Get current data from DB
            (currentEnergy, lastEnergyUpdate) = dbManager.GetUserEnergyInfo(userID);

            // Update UI energy
            if (playerEnergyCount != null)
                playerEnergyCount.text = $"{currentEnergy}";

            // Handle countdown logic
            if (currentEnergy >= 30)
            {
                if (energyTimerText != null)
                    energyTimerText.text = "Full Energy";
            }
            else
            {
                DateTime nextRefill = lastEnergyUpdate.AddHours(1);
                TimeSpan timeUntilNext = nextRefill - DateTime.UtcNow;

                // If time passed, update immediately
                if (timeUntilNext.TotalSeconds <= 0)
                {
                    dbManager.UpdateUserEnergy(userID);
                    UpdateUI();
                }
                else
                {
                    int mins = Mathf.FloorToInt((float)timeUntilNext.TotalMinutes);
                    int secs = Mathf.FloorToInt((float)(timeUntilNext.TotalSeconds % 60));

                    if (energyTimerText != null)
                        energyTimerText.text = $"{mins:D2}m {secs:D2}s";
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
