using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainSceneUI : MonoBehaviour
{
    [Header("UI References")]
    public Text welcomeText;
    public DatabaseManager dbManager;
    public Text playerCoinCount;
    public Text playerEnergyCount;
    public Slider healthSlider;
    public Slider damageSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Energy Button Control")]
    public List<Button> energyButtons; // Assign buttons in the inspector

    public int userID = 1;

    private int currentEnergy; // Track energy for checks

    void Awake()
    {
        AudioManager.Instance.RegisterBgmSlider(bgmSlider);
        AudioManager.Instance.RegisterSfxSlider(sfxSlider);
    }

    private void OnEnable()
    {
        DatabaseManager.OnUserDataChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDisable()
    {
        DatabaseManager.OnUserDataChanged -= UpdateUI;
    }

    public void UpdateUI()
    {
        var (firstName, middleName, lastName, coin, energy) = dbManager.GetUser();
        currentEnergy = energy; // Save current energy

        if (welcomeText != null)
            welcomeText.text = $"{firstName}!";
        if (playerEnergyCount != null)
            playerEnergyCount.text = $"{energy}";
        if (playerCoinCount != null)
            playerCoinCount.text = $"{coin}";

        UpdateButtonStates();
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
}
