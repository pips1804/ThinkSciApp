using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MainSceneUI : MonoBehaviour
{
    public Text welcomeText; // Text object to show the name
    public DatabaseManager dbManager; // Reference to the DatabaseManager

    void Start()
    {
        // Fetch user data from the database
        var (firstName, middleName, lastName) = dbManager.GetUser();

        // Display the user's full name in the welcome text
        welcomeText.text = $"{firstName}!";
    }
}
