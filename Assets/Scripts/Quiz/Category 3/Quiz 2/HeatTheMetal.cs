using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class Question
{
    public string questionText;
    public string[] choices;
    public int correctAnswerIndex;
}

public class HeatTheMetal : MonoBehaviour
{
    [Header("Panels")]
    public GameObject introPanel;
    public GameObject gamePanel;
    public GameObject quizPanel;
    public GameObject resultPanel;

    [Header("Scenario 2 Panels")]
    public GameObject warmRoomPanel;
    public GameObject messagePanel;
    public Text messageText;

    [Header("Scenario 1 Elements")]
    public GameObject heatTheMetalPanel;
    public Image heatEffect;
    public float heatingTime = 2f;

    [Header("Scenario 2 Elements")]
    public GameObject warmAirArrows;
    public GameObject coolAirArrows;
    public float moveDistance = 30f;
    public float moveSpeed = 1f;

    [Header("Scenario 3 Elements")]
    public GameObject solarPanelPanel;
    public Slider rotationSlider;
    public RectTransform solarPanelImage;
    public Image sunlightBeam;
    public float targetRotation = 45f;
    public float rotationTolerance = 5f;
    public float holdTimeRequired = 1f;
    public float beamFadeSpeed = 1f;
    public float targetXPosition;
    public float positionTolerance = 10f;

    [Header("Quiz Elements")]
    public Text questionTextUI;
    public Button[] answerButtons;
    public Text[] answerButtonTexts;
    public Question[] questionsScenario1;
    public Question[] questionsScenario2;
    public Question[] questionsScenario3;
    public Image petImage;
    public Sprite thinkingPetSprite;
    public Sprite ideaPetSprite;

    [Header("Animation Settings")]
    public float fadeDuration = 0.5f;
    public AnimationCurve fadeAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Button Colors")]
    public Color correctButtonColor = Color.green;
    public Color incorrectButtonColor = Color.red;
    public Color defaultButtonColor = Color.white;

    [Header("Dialogue Reference")]
    public Dialogues dialogueManager;

    private int currentQuestionIndex = 0;
    private bool isHeating = false;
    private float heatProgress = 0f;
    private bool inScenario2 = false;
    private Coroutine arrowAnim;

    private float correctHoldTime = 0f;
    private bool scenario3Active = false;
    private bool scenario3Completed = false;

    // Store original button colors
    private Color[] originalButtonColors;
    private bool buttonsInteractable = true;

    void Start()
    {
        ShowIntro();
        warmRoomPanel.SetActive(false);
        warmAirArrows.SetActive(false);
        coolAirArrows.SetActive(false);

        // Store original button colors
        StoreOriginalButtonColors();
    }

    void StoreOriginalButtonColors()
    {
        if (answerButtons == null || answerButtons.Length == 0)
        {
            Debug.LogWarning("Answer buttons array is null or empty!");
            return;
        }

        originalButtonColors = new Color[answerButtons.Length];
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                Image buttonImage = answerButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    originalButtonColors[i] = buttonImage.color;
                }
                else
                {
                    Debug.LogWarning($"Button {i} doesn't have an Image component!");
                    originalButtonColors[i] = Color.white; // Default fallback
                }
            }
            else
            {
                Debug.LogWarning($"Button {i} is null!");
                originalButtonColors[i] = Color.white; // Default fallback
            }
        }
    }

    void ShowIntro()
    {
        SetPanelActive(introPanel, true);
        SetPanelActive(gamePanel, false);
        SetPanelActive(quizPanel, false);
        SetPanelActive(resultPanel, false);
        SetPanelActive(messagePanel, false);
        SetPanelActive(warmRoomPanel, false);
        heatEffect.color = new Color(heatEffect.color.r, heatEffect.color.g, heatEffect.color.b, 0);
    }

    public void StartGame()
    {
        StartCoroutine(FadeOutThenAction(introPanel, () =>
        {
            dialogueManager.StartDialogue(0);
            StartCoroutine(WaitForDialogueThen(() =>
            {
                StartCoroutine(FadeInThenAction(gamePanel, () =>
                {
                    isHeating = false;
                    heatProgress = 0f;
                    heatTheMetalPanel.SetActive(true);
                    petImage.gameObject.SetActive(false);
                }));
            }));
        }));
    }

    IEnumerator WaitForDialogueThen(System.Action onFinish)
    {
        while (dialogueManager.dialoguePanel.activeSelf)
            yield return null;
        onFinish?.Invoke();
    }

    void Update()
    {
        if (!inScenario2 && isHeating)
        {
            heatProgress += Time.deltaTime;
            Color c = heatEffect.color;
            c.a = Mathf.Clamp01(heatProgress / heatingTime);
            heatEffect.color = c;

            if (heatProgress >= heatingTime)
            {
                isHeating = false;
                StartCoroutine(StartQuizAfterDelay(3f));
            }
        }

        if (scenario3Active && !scenario3Completed)
        {
            HandleScenario3();
        }
    }

    private IEnumerator StartQuizAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartQuizScenario1();
    }

    void StartQuizScenario1()
    {
        StartCoroutine(FadeOutThenAction(gamePanel, () =>
        {
            StartCoroutine(FadeInThenAction(quizPanel, () =>
            {
                currentQuestionIndex = 0;
                ShowQuestionScenario1();
            }));
        }));
    }

    void ShowQuestionScenario1()
    {
        if (questionsScenario1 == null || questionsScenario1.Length == 0)
        {
            Debug.LogError("Questions Scenario 1 is null or empty!");
            return;
        }

        if (currentQuestionIndex >= questionsScenario1.Length)
        {
            Debug.LogError("Current question index is out of bounds!");
            return;
        }

        Question q = questionsScenario1[currentQuestionIndex];
        questionTextUI.text = q.questionText;

        // Reset button colors and interactability
        ResetButtonColors();
        buttonsInteractable = true;

        // Clear all listeners first
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].onClick.RemoveAllListeners();
            }
        }

        // Set up buttons with choices
        for (int i = 0; i < answerButtons.Length && i < q.choices.Length; i++)
        {
            if (answerButtonTexts[i] != null)
            {
                answerButtonTexts[i].text = q.choices[i];
                Debug.Log($"Button {i} text set to: {q.choices[i]}");
            }
            else
            {
                Debug.LogError($"Answer button text {i} is null!");
            }

            // Create a local copy of the index to avoid closure issues
            int buttonIndex = i;
            answerButtons[i].onClick.AddListener(() =>
            {
                Debug.Log($"Button {buttonIndex} clicked!");
                SelectAnswerScenario1(buttonIndex);
            });
            answerButtons[i].interactable = true;
            answerButtons[i].gameObject.SetActive(true);
        }

        // Hide unused buttons if there are more buttons than choices
        for (int i = q.choices.Length; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectAnswerScenario1(int index)
    {
        if (!buttonsInteractable) return;

        buttonsInteractable = false;
        Question q = questionsScenario1[currentQuestionIndex];
        bool isCorrect = index == q.correctAnswerIndex;

        StartCoroutine(HandleAnswerFeedback(index, q.correctAnswerIndex, isCorrect, () =>
        {
            currentQuestionIndex++;
            if (currentQuestionIndex < questionsScenario1.Length)
                ShowQuestionScenario1();
            else
                EndScenario1();
        }));
    }

    void EndScenario1()
    {
        StartCoroutine(FadeOutThenAction(quizPanel, () =>
        {
            StartCoroutine(FadeInThenAction(resultPanel, () =>
            {
                petImage.gameObject.SetActive(true);
                dialogueManager.StartDialogue(1);
                StartCoroutine(WaitForDialogueThen(() =>
                {
                    StartCoroutine(FadeOutThenAction(resultPanel, () =>
                    {
                        heatTheMetalPanel.SetActive(false);
                        petImage.gameObject.SetActive(false);
                        StartScenario2();
                    }));
                }));
            }));
        }));
    }

    void StartScenario2()
    {
        inScenario2 = true;
        StartCoroutine(FadeInThenAction(gamePanel, () =>
        {
            warmRoomPanel.SetActive(true);
            warmAirArrows.SetActive(false);
            coolAirArrows.SetActive(false);
        }));
    }

    public void FirePlacedSuccess()
    {
        isHeating = true;
    }

    public void HeaterPlacedSuccess()
    {
        warmAirArrows.SetActive(true);
        coolAirArrows.SetActive(true);
        if (arrowAnim != null) StopCoroutine(arrowAnim);
        arrowAnim = StartCoroutine(AnimateArrows());
        StartQuizScenario2();
    }

    private IEnumerator AnimateArrows()
    {
        Vector3 warmStart = warmAirArrows.transform.localPosition;
        Vector3 coolStart = coolAirArrows.transform.localPosition;

        while (true)
        {
            float t = Mathf.PingPong(Time.time * moveSpeed, 1f);
            warmAirArrows.transform.localPosition = warmStart + Vector3.up * Mathf.Lerp(0, moveDistance, t);
            coolAirArrows.transform.localPosition = coolStart + Vector3.down * Mathf.Lerp(0, moveDistance, t);
            yield return null;
        }
    }

    public void StartQuizScenario2()
    {
        StartCoroutine(StartQuizScenario2AfterDelay(5f));
    }

    private IEnumerator StartQuizScenario2AfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        StartCoroutine(FadeOutThenAction(gamePanel, () =>
        {
            StartCoroutine(FadeInThenAction(quizPanel, () =>
            {
                currentQuestionIndex = 0;
                ShowQuestionScenario2();
            }));
        }));
    }

    void ShowQuestionScenario2()
    {
        if (questionsScenario2 == null || questionsScenario2.Length == 0)
        {
            Debug.LogError("Questions Scenario 2 is null or empty!");
            return;
        }

        if (currentQuestionIndex >= questionsScenario2.Length)
        {
            Debug.LogError("Current question index is out of bounds!");
            return;
        }

        Question q = questionsScenario2[currentQuestionIndex];
        questionTextUI.text = q.questionText;

        ResetButtonColors();
        buttonsInteractable = true;

        for (int i = 0; i < answerButtons.Length && i < q.choices.Length; i++)
        {
            if (answerButtonTexts[i] != null)
            {
                answerButtonTexts[i].text = q.choices[i];
                Debug.Log($"Button {i} text set to: {q.choices[i]}");
            }
            else
            {
                Debug.LogError($"Answer button text {i} is null!");
            }

            answerButtons[i].onClick.RemoveAllListeners();
            int index = i;
            answerButtons[i].onClick.AddListener(() => SelectAnswerScenario2(index));
            answerButtons[i].interactable = true;
            answerButtons[i].gameObject.SetActive(true);
        }

        // Hide unused buttons if there are more buttons than choices
        for (int i = q.choices.Length; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(false);
        }
    }

    public void SelectAnswerScenario2(int index)
    {
        if (!buttonsInteractable) return;

        buttonsInteractable = false;
        Question q = questionsScenario2[currentQuestionIndex];
        bool isCorrect = index == q.correctAnswerIndex;

        StartCoroutine(HandleAnswerFeedback(index, q.correctAnswerIndex, isCorrect, () =>
        {
            currentQuestionIndex++;
            if (currentQuestionIndex < questionsScenario2.Length)
                ShowQuestionScenario2();
            else
                EndScenario2();
        }));
    }

    void EndScenario2()
    {
        StartCoroutine(FadeOutThenAction(quizPanel, () =>
        {
            petImage.sprite = ideaPetSprite;
            petImage.rectTransform.sizeDelta = new Vector2(820, 780);
            petImage.gameObject.SetActive(true);
            dialogueManager.StartDialogue(2);
            StartCoroutine(WaitForDialogueThen(() =>
            {
                warmRoomPanel.SetActive(false);
                petImage.gameObject.SetActive(false);
                StartScenario3();
            }));
        }));
    }

    void StartScenario3()
    {
        StartCoroutine(FadeInThenAction(gamePanel, () =>
        {
            solarPanelPanel.SetActive(true);
            sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b, 0);
            correctHoldTime = 0f;
            scenario3Active = true;
            scenario3Completed = false;

            rotationSlider.onValueChanged.RemoveAllListeners();
            rotationSlider.onValueChanged.AddListener(OnSliderChanged);
        }));
    }

    void OnSliderChanged(float value)
    {
        float newX = Mathf.Lerp(-200f, 200f, value);
        Vector3 pos = solarPanelImage.localPosition;
        pos.x = newX;
        solarPanelImage.localPosition = pos;
    }

    void HandleScenario3()
    {
        float currentX = solarPanelImage.localPosition.x;
        float diff = Mathf.Abs(currentX - targetXPosition);

        if (diff <= positionTolerance)
        {
            correctHoldTime += Time.deltaTime;
            sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b,
                Mathf.MoveTowards(sunlightBeam.color.a, 1f, beamFadeSpeed * Time.deltaTime));

            if (correctHoldTime >= holdTimeRequired)
            {
                scenario3Completed = true;
                scenario3Active = false;
                rotationSlider.interactable = false;
                StartQuizScenario3();
            }
        }
        else
        {
            correctHoldTime = 0f;
            sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b,
                Mathf.MoveTowards(sunlightBeam.color.a, 0f, beamFadeSpeed * Time.deltaTime));
        }
    }

    void StartQuizScenario3()
    {
        StartCoroutine(FadeOutThenAction(gamePanel, () =>
        {
            StartCoroutine(FadeInThenAction(quizPanel, () =>
            {
                currentQuestionIndex = 0;
                ShowQuestionScenario3();
            }));
        }));
    }

    void ShowQuestionScenario3()
    {
        if (questionsScenario3 == null || questionsScenario3.Length == 0)
        {
            Debug.LogError("Questions Scenario 3 is null or empty!");
            return;
        }

        if (currentQuestionIndex >= questionsScenario3.Length)
        {
            Debug.LogError("Current question index is out of bounds!");
            return;
        }

        Question q = questionsScenario3[currentQuestionIndex];
        questionTextUI.text = q.questionText;

        ResetButtonColors();
        buttonsInteractable = true;

        // Clear all listeners first
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].onClick.RemoveAllListeners();
            }
        }

        // Set up buttons with choices
        for (int i = 0; i < answerButtons.Length && i < q.choices.Length; i++)
        {
            if (answerButtonTexts[i] != null)
            {
                answerButtonTexts[i].text = q.choices[i];
                Debug.Log($"Button {i} text set to: {q.choices[i]}");
            }
            else
            {
                Debug.LogError($"Answer button text {i} is null!");
            }

            // Create a local copy of the index to avoid closure issues
            int buttonIndex = i;
            answerButtons[i].onClick.AddListener(() =>
            {
                Debug.Log($"Button {buttonIndex} clicked!");
                SelectAnswerScenario3(buttonIndex);
            });
            answerButtons[i].interactable = true;
            answerButtons[i].gameObject.SetActive(true);
        }

        // Hide unused buttons if there are more buttons than choices
        for (int i = q.choices.Length; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SelectAnswerScenario3(int index)
    {
        if (!buttonsInteractable) return;

        buttonsInteractable = false;
        Question q = questionsScenario3[currentQuestionIndex];
        bool isCorrect = index == q.correctAnswerIndex;

        StartCoroutine(HandleAnswerFeedback(index, q.correctAnswerIndex, isCorrect, () =>
        {
            currentQuestionIndex++;
            if (currentQuestionIndex < questionsScenario3.Length)
                ShowQuestionScenario3();
            else
                EndScenario3();
        }));
    }

    void EndScenario3()
    {
        StartCoroutine(FadeOutThenAction(quizPanel, () =>
        {
            StartCoroutine(FadeInThenAction(messagePanel, () =>
            {
                messageText.text = "Excellent! You finished all tasks!";
            }));
        }));
    }

    // Animation Helper Methods
    void SetPanelActive(GameObject panel, bool active)
    {
        panel.SetActive(active);
    }

    IEnumerator FadeInThenAction(GameObject panel, System.Action onComplete = null)
    {
        panel.SetActive(true);
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(panel);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false; // Start with non-interactable

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            canvasGroup.alpha = fadeAnimationCurve.Evaluate(t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true; // Make interactable when fade is complete
        onComplete?.Invoke();
    }

    IEnumerator FadeOutThenAction(GameObject panel, System.Action onComplete = null)
    {
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(panel);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            canvasGroup.alpha = fadeAnimationCurve.Evaluate(1f - t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panel.SetActive(false);
        onComplete?.Invoke();
    }

    CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    // Button Color Feedback Methods
    IEnumerator HandleAnswerFeedback(int selectedIndex, int correctIndex, bool isCorrect, System.Action onComplete)
    {
        Debug.Log($"HandleAnswerFeedback called - Selected: {selectedIndex}, Correct: {correctIndex}, IsCorrect: {isCorrect}");

        // Disable all buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].interactable = false;
            }
        }

        if (isCorrect)
        {
            // Color selected button green, others red
            if (selectedIndex < answerButtons.Length && answerButtons[selectedIndex] != null)
            {
                Image selectedImage = answerButtons[selectedIndex].GetComponent<Image>();
                if (selectedImage != null)
                {
                    selectedImage.color = correctButtonColor;
                    Debug.Log($"Set button {selectedIndex} to green (correct)");
                }
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (i != selectedIndex && answerButtons[i] != null && answerButtons[i].gameObject.activeInHierarchy)
                {
                    Image buttonImage = answerButtons[i].GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = incorrectButtonColor;
                        Debug.Log($"Set button {i} to red (not selected)");
                    }
                }
            }
        }
        else
        {
            // Color selected button red
            if (selectedIndex < answerButtons.Length && answerButtons[selectedIndex] != null)
            {
                Image selectedImage = answerButtons[selectedIndex].GetComponent<Image>();
                if (selectedImage != null)
                {
                    selectedImage.color = incorrectButtonColor;
                    Debug.Log($"Set button {selectedIndex} to red (wrong choice)");
                }
            }

            // Color correct answer green
            if (correctIndex < answerButtons.Length && answerButtons[correctIndex] != null)
            {
                Image correctImage = answerButtons[correctIndex].GetComponent<Image>();
                if (correctImage != null)
                {
                    correctImage.color = correctButtonColor;
                    Debug.Log($"Set button {correctIndex} to green (correct answer)");
                }
            }

            // Color remaining buttons red
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (i != selectedIndex && i != correctIndex && answerButtons[i] != null && answerButtons[i].gameObject.activeInHierarchy)
                {
                    Image buttonImage = answerButtons[i].GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = incorrectButtonColor;
                        Debug.Log($"Set button {i} to red (other option)");
                    }
                }
            }
        }

        // Wait for 2 seconds to show the feedback
        yield return new WaitForSeconds(2f);

        onComplete?.Invoke();
    }

    void ResetButtonColors()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null && i < originalButtonColors.Length)
            {
                SetButtonColor(answerButtons[i], originalButtonColors[i]);
            }
        }
    }


    void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;

        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.pressedColor = color;
        colors.selectedColor = color;
        colors.disabledColor = color;
        btn.colors = colors;
    }
}
