using UnityEngine;
using TMPro;

public class MainSceneUI : MonoBehaviour
{
    public TMP_Text welcomeText; // Text object to show the name
    public DatabaseManager dbManager; // Reference to the DatabaseManager

    void Start()
    {
        // Fetch user data from the database
        var (firstName, middleName, lastName) = dbManager.GetUser();

        // Display the user's full name in the welcome text
        welcomeText.text = $"Welcome, {firstName} {middleName} {lastName}";
    }
}
