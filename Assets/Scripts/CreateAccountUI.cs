using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class CreateAccountUI : MonoBehaviour
{
    public TMP_InputField InputFName, InputMName, InputLName;
    public GameObject CreateAccountPanel;
    public GameObject LoadingScreen;
    public Slider LoadingSlider;
    public DatabaseManager dbManager;

    private void Start()
    {
        StartCoroutine(PlaySplashScreen());
    }

    IEnumerator PlaySplashScreen()
    {
        LoadingScreen.SetActive(true);
        LoadingSlider.value = 0f;

        float timer = 0f;
        float splashDuration = 2f; // 2 seconds for splash animation

        while (timer < splashDuration)
        {
            timer += Time.deltaTime;
            LoadingSlider.value = timer / splashDuration;
            yield return null;
        }

        LoadingSlider.value = 1f; // Set to 1 directly when splash ends
        yield return new WaitForSeconds(0.5f); // nice tiny pause

        if (dbManager.HasUser())
        {
            StartCoroutine(LoadSceneAsync("MainScene"));
        }
        else
        {
            LoadingScreen.SetActive(false); // Hide loading
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

        StartCoroutine(LoadSceneAsync("MainScene"));
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

            // Only update the slider value in this method
            LoadingSlider.value = Mathf.MoveTowards(LoadingSlider.value, progress, Time.deltaTime * 0.5f);

            if (LoadingSlider.value >= 0.99f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
