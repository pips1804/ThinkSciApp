using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class CreateAccountUI : MonoBehaviour
{
    public TMP_InputField InputFName, InputMName, InputLName;
    public GameObject CreateAccountPanel;
    public DatabaseManager dbManager;

    void Start()
    {
        // Wait until user data is actually in the database before redirecting
        if (dbManager.HasUser())
        {
            // Redirect only if there is valid user data
            SceneManager.LoadScene("MainScene");
        }
        else
        {
            // Show account creation panel
            CreateAccountPanel.SetActive(true);
        }
    }


    public void OnSubmit()
    {

        Debug.Log("Submit button clicked!");

        string firstName = InputFName.text;
        string middleName = InputMName.text;
        string lastName = InputLName.text;

        dbManager.SaveUser(firstName, middleName, lastName);

        SceneManager.LoadScene("MainScene");
    }
}
