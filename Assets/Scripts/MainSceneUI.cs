using UnityEngine;
using UnityEngine.UI;

public class MainSceneUI : MonoBehaviour
{
    public Text welcomeText; // Text object to show the name
    public DatabaseManager dbManager; // Reference to the DatabaseManager
    public Text playerCoinCount; // Text object to show the name
    public Text playerEnergyCount; // Text object to show the name
    public Slider healthSlider;
    public Slider damageSlider;

    public Slider bgmSlider;
    public Slider sfxSlider;

    public int userID = 1;

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
        if (welcomeText != null)
            welcomeText.text = $"{firstName}!";
        if (playerEnergyCount != null)
            playerEnergyCount.text = $"{energy}";
        if (playerCoinCount != null)
            playerCoinCount.text = $"{coin}";
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
