using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class EditUsernameUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInputField;
    public GameObject editUsernamePanel;

    [Header("Modals")]
    public GameObject warningModal;
    public Text warningText;
    public GameObject successModal;
    public Text successText;

    [Header("Database")]
    public DatabaseManager dbManager;

    public AudioClip errorSound;
    public AudioClip successSound;

    public void OpenPanel()
    {
        editUsernamePanel.SetActive(true);
        usernameInputField.text = "";
    }

    public void ClosePanel()
    {
        editUsernamePanel.SetActive(false);
    }

    public void OnSubmit()
    {
        string newUsername = usernameInputField.text.Trim();

        // === Validation ===
        if (string.IsNullOrEmpty(newUsername))
        {
            ShowWarning("Username cannot be empty!");
            return;
        }

        if (newUsername.Length > 9) // 🔹 require exactly 8 characters
        {
            ShowWarning("Username must be 8 characters or less.");
            return;
        }

        if (!Regex.IsMatch(newUsername, "^[a-zA-Z0-9 ]+$"))
        {
            ShowWarning("Only letters, numbers, and spaces are allowed.");
            return;
        }

        // ✅ Save new username
        dbManager.UpdateUser(newUsername);
        ShowSuccess("Username updated successfully!");
    }

    private void ShowWarning(string message)
    {
        AudioManager.Instance.PlaySFX(errorSound);
        if (warningText != null) warningText.text = message;
        warningModal.SetActive(true);
        usernameInputField.text = "";
    }

    public void CloseWarning()
    {
        warningModal.SetActive(false);
    }

    private void ShowSuccess(string message)
    {
        AudioManager.Instance.PlaySFX(successSound);
        if (successText != null) successText.text = message;
        successModal.SetActive(true);
        usernameInputField.text = "";
    }

    public void CloseSuccess()
    {
        successModal.SetActive(false);
        ClosePanel();
    }
}
