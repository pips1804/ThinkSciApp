using UnityEngine;
using UnityEngine.UI;

public class MainSceneUI : MonoBehaviour
{
    public Text welcomeText; // Text object to show the name
    public DatabaseManager dbManager; // Reference to the DatabaseManager
    public Text petName; // Text object to show the name
    public Text petBaseHealth; // Text object to show the name
    public Text petBaseDamage; // Text object to show the name
    public Text playerCoinCount; // Text object to show the name
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

        UpdateUI();
    }

    public void UpdateUI()
    {
        var (firstName, middleName, lastName, coin) = dbManager.GetUser();
        var (name, baseHealth, baseDamage) = dbManager.GetPetStats(userID);

        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 200;
            healthSlider.value = baseHealth;
        }

        if (damageSlider != null)
        {
            damageSlider.minValue = 0;
            damageSlider.maxValue = 50;
            damageSlider.value = baseDamage;
        }

        if (welcomeText != null)
            welcomeText.text = $"{firstName}!";
        if (petName != null)
            petName.text = name;
        if (petBaseHealth != null)
            petBaseHealth.text = $"{baseHealth}/200";
        if (petBaseDamage != null)
            petBaseDamage.text = $"{baseDamage}/50";
        if (playerCoinCount != null)
            playerCoinCount.text = $"{coin}";

    }

}
