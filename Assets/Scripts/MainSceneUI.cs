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

    public int userID = 1;

    void Start()
    {
        // Fetch user data from the database
        var (firstName, middleName, lastName, coin) = dbManager.GetUser();
        var(name, baseHealth, baseDamage) = dbManager.GetPetStats(userID);

        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 200;
            healthSlider.value = baseHealth;
        }

        if (damageSlider != null)
        {
            damageSlider.minValue = 0;
            damageSlider.maxValue = 200;
            damageSlider.value = baseDamage;
        }

        // Display the user's full name in the welcome text
        welcomeText.text = $"{firstName}!";
        petName.text = $"{name}";
        petBaseHealth.text = $"{baseHealth}/200";
        petBaseDamage.text = $"{baseDamage}/50";
        playerCoinCount.text = $"{coin}";
    }
}
