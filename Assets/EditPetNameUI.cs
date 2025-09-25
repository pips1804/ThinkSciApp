using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class EditPetNameUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField petNameInputField;
    public GameObject editPetNamePanel;

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
        editPetNamePanel.SetActive(true);
        petNameInputField.text = "";
    }

    public void ClosePanel()
    {
        editPetNamePanel.SetActive(false);
    }

    public void OnSubmit()
    {
        string newPetName = petNameInputField.text.Trim();

        // === Validation ===
        if (string.IsNullOrEmpty(newPetName))
        {
            ShowWarning("Pet name cannot be empty!");
            return;
        }

        if (newPetName.Length > 12)
        {
            ShowWarning("Pet name must be 12 characters or less.");
            return;
        }

        if (!Regex.IsMatch(newPetName, @"^[a-zA-Z0-9 ]+$"))
        {
            ShowWarning("Only letters, numbers, and spaces are allowed.");
            return;
        }

        // ✅ Save new pet name
        dbManager.SavePetName(newPetName);
        ShowSuccess("Pet name updated successfully!");
    }

    private void ShowWarning(string message)
    {
        AudioManager.Instance.PlaySFX(errorSound);
        if (warningText != null) warningText.text = message;
        warningModal.SetActive(true);
        petNameInputField.text = "";
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
        petNameInputField.text = "";
    }

    public void CloseSuccess()
    {
        successModal.SetActive(false);
        ClosePanel();
    }
}
