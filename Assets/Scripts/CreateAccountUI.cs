using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

public class CreateAccountUI : MonoBehaviour
{
    public TMP_InputField InputUsername;
    public GameObject CreateAccountPanel;
    public GameObject LoadingScreen;
    public Slider LoadingSlider;
    public DatabaseManager dbManager;
    public GameObject WarningPanel;
    public Text WarningText;
    public GameObject SuccessModal;

    public GameObject PetIntroPanel;
    public PetIntroManager petIntroManager;

    private void Start()
    {
        StartCoroutine(PlaySplashScreen());
    }

    IEnumerator PlaySplashScreen()
    {
        LoadingScreen.SetActive(true);
        LoadingSlider.value = 0f;

        float timer = 0f;
        float splashDuration = 2f;

        while (timer < splashDuration)
        {
            timer += Time.deltaTime;
            LoadingSlider.value = timer / splashDuration;
            yield return null;
        }

        LoadingSlider.value = 1f;
        yield return new WaitForSeconds(0.5f);

        if (dbManager.HasUser() && !dbManager.IsDefaultUser())
        {
            StartCoroutine(LoadSceneAsync("MainScene"));
        }
        else
        {
            LoadingScreen.SetActive(false);
            CreateAccountPanel.SetActive(true);
        }
    }

    public void OnSubmit()
    {
        Debug.Log("Submit button clicked!");

        string username = InputUsername.text.Trim();

        // === Validation ===
        if (string.IsNullOrEmpty(username))
        {
            ShowWarning("Username cannot be empty!");
            return;
        }

        if (username.Length != 8) // require exactly 8 characters
        {
            ShowWarning("Username must be exactly 8 characters.");
            return;
        }

        if (!Regex.IsMatch(username, "^[a-zA-Z0-9]+$"))
        {
            ShowWarning("Only letters and numbers are allowed.");
            return;
        }

        WarningPanel.SetActive(false);

        // ✅ Save username (adjust UpdateUser if needed)
        dbManager.UpdateUser(username);

        ShowSuccessModal();
    }

    private void ShowWarning(string message)
    {
        if (WarningText != null) WarningText.text = message;
        WarningPanel.SetActive(true);
    }

    public void CloseWarning()
    {
        WarningPanel.SetActive(false);
    }

    public void ShowSuccessModal()
    {
        SuccessModal.SetActive(true);
    }

    public void CloseSuccessModal()
    {
        SuccessModal.SetActive(false);
        CreateAccountPanel.SetActive(false);
        PetIntroPanel.SetActive(true);
        petIntroManager.StartIntro();
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        LoadingScreen.SetActive(true);
        CreateAccountPanel.SetActive(false);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            LoadingSlider.value = Mathf.MoveTowards(LoadingSlider.value, progress, Time.deltaTime * 0.5f);

            if (LoadingSlider.value >= 0.99f)
                operation.allowSceneActivation = true;

            yield return null;
        }
    }
}
