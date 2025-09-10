using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

[System.Serializable]
public class Question
{
    public int id;
    public string questionText;
    public string[] choices;
    public int correctAnswerIndex;
}

// DIAGNOSTIC: Helper component to track which button was actually clicked
public class ButtonClickTracker : MonoBehaviour
{
    public int buttonIndex;
    public HeatTheMetal quizManager;

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            // Clear existing listeners and add our tracker
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
            Debug.Log($"ButtonClickTracker attached to button {buttonIndex}");
        }
    }

    void OnClick()
    {
        Debug.Log($"ButtonClickTracker: Physical button {buttonIndex} clicked on GameObject {gameObject.name}");
        if (quizManager != null)
        {
            quizManager.OnButtonClicked(buttonIndex, gameObject);
        }
    }
}

public class HeatTheMetal : MonoBehaviour
{
    [Header("Panels")]
    public GameObject gamePanel;
    public GameObject quizPanel;

    [Header("Pass/Fail Modals")]
    public GameObject passedModal;
    public GameObject failedModal;
    public Text passedScoreText;
    public Text failedScoreText;
    public Button retryButton;
    public Button continueButton;
    public Button retakeQuizButton; // NEW: Add this button to the failed modal

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
    public float targetXPosition;
    public float positionTolerance = 10f;
    public float holdTimeRequired = 1f;
    public float beamFadeSpeed = 1f;

    [Header("Quiz Elements")]
    public Text questionTextUI;
    public Button[] answerButtons;
    public Text[] answerButtonTexts;
    public Image[] buttonVisualImages;

    [Header("Quiz Timer & Score")]
    public Text timerText;
    public Text scoreText;
    public float questionTimeLimit = 30f;
    public Image timerFillImage;

    [Header("Button Colors")]
    public Color correctButtonColor = Color.green;
    public Color incorrectButtonColor = Color.red;
    public Color defaultButtonColor = Color.white;

    [Header("References")]
    public Dialogues dialogueManager;
    public DatabaseManager dbManager;

    [Header("Draggable Items - NEW")]
    public DragFire dragFireScript;
    public DragHeater dragHeaterScript;

    // Private variables
    private MultipleChoice.MultipleChoiceQuestions[] questionsScenario1;
    private MultipleChoice.MultipleChoiceQuestions[] questionsScenario2;
    private MultipleChoice.MultipleChoiceQuestions[] questionsScenario3;    

    private int currentQuestionIndex = 0;
    private bool isHeating = false;
    private float heatProgress = 0f;
    private bool inScenario2 = false;
    private Coroutine arrowAnim;

    private float correctHoldTime = 0f;
    private bool scenario3Active = false;
    private bool scenario3Completed = false;

    private Color[] originalButtonColors;
    private bool buttonsInteractable = true;

    // Quiz Timer and Score Variables
    private float currentQuestionTimer = 0f;
    private bool questionTimerActive = false;
    private Coroutine questionTimerCoroutine;
    private int currentScenarioScore = 0;
    private int totalQuestionsAnswered = 0;
    private int currentScenario = 1;
    private const float PASSING_PERCENTAGE = 70f;

    // Overall game score tracking
    private int totalCorrectAnswers = 0;
    private int totalQuestionsInGame = 0;

    // NEW: Store original positions for reset
    private Vector3 originalFirePosition;
    private Vector3 originalHeaterPosition;
    private bool originalPositionsStored = false;

    [Header("Sound Effects")]
    public AudioClip passed;
    public AudioClip failed;
    public AudioClip correct;
    public AudioClip wrong;

    void Start()
    {
        LoadQuestions();
        StoreOriginalButtonColors();
        StoreOriginalPositions(); // NEW: Store initial positions
        ShowIntro();
        InitializeScenarioElements();
        InitializeModals();
        SetupButtonListeners();
        StartGame();
    }

    // NEW: Store original positions of draggable items
    void StoreOriginalPositions()
    {
        if (!originalPositionsStored)
        {
            if (dragFireScript != null)
            {
                originalFirePosition = dragFireScript.transform.position;
                Debug.Log($"Stored original fire position: {originalFirePosition}");
            }

            if (dragHeaterScript != null)
            {
                originalHeaterPosition = dragHeaterScript.transform.position;
                Debug.Log($"Stored original heater position: {originalHeaterPosition}");
            }

            originalPositionsStored = true;
        }
    }

    // NEW: Reset all draggable items to their original positions
    void ResetDraggableItems()
    {
        Debug.Log("Resetting draggable items to original positions");

        if (dragFireScript != null)
        {
            dragFireScript.ResetFire(originalFirePosition);
            Debug.Log($"Reset fire to position: {originalFirePosition}");
        }

        if (dragHeaterScript != null)
        {
            dragHeaterScript.ResetHeater(originalHeaterPosition);
            Debug.Log($"Reset heater to position: {originalHeaterPosition}");
        }
    }

    // NEW: Complete game reset functionality
    public void RetakeEntireQuiz()
    {
        Debug.Log("Retaking entire quiz - full reset");
        LoadQuestions();
        // Hide all modals
        if (passedModal != null) passedModal.SetActive(false);
        if (failedModal != null) failedModal.SetActive(false);

        // Stop all coroutines
        StopAllCoroutines();

        // Reset draggable items
        ResetDraggableItems();

        // Reset all game state
        ResetAllGameState();

        // Start from the beginning
        ShowIntro();
        StartGame();
    }

    // NEW: Reset all game state variables
    void ResetAllGameState()
    {
        // Reset scenario progress
        currentScenario = 1;
        currentQuestionIndex = 0;
        currentScenarioScore = 0;
        totalQuestionsAnswered = 0;
        totalCorrectAnswers = 0;
        totalQuestionsInGame = 0;

        // Reset scenario 1 state
        isHeating = false;
        heatProgress = 0f;

        // Reset scenario 2 state
        inScenario2 = false;
        if (arrowAnim != null)
        {
            StopCoroutine(arrowAnim);
            arrowAnim = null;
        }

        // Reset scenario 3 state
        correctHoldTime = 0f;
        scenario3Active = false;
        scenario3Completed = false;

        // Reset quiz state
        buttonsInteractable = true;
        StopQuestionTimer();

        // Reset UI elements
        if (heatEffect != null)
            heatEffect.color = new Color(heatEffect.color.r, heatEffect.color.g, heatEffect.color.b, 0);

        if (sunlightBeam != null)
            sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b, 0);

        if (rotationSlider != null)
        {
            rotationSlider.value = 0.5f; // Reset to middle position
            rotationSlider.interactable = true;
        }

        // Reset arrow positions and visibility
        if (warmAirArrows != null) warmAirArrows.SetActive(false);
        if (coolAirArrows != null) coolAirArrows.SetActive(false);

        UpdateScoreDisplay();
        Debug.Log("All game state reset complete");
    }

    void LoadQuestions()
    {
        if (dbManager != null)
        {
            questionsScenario1 = dbManager.GetRandomUnusedQuestions(9, "Multiple Choice Conduction", 5).ToArray();
            questionsScenario2 = dbManager.GetRandomUnusedQuestions(9, "Multiple Choice Convection", 5).ToArray();
            questionsScenario3 = dbManager.GetRandomUnusedQuestions(9, "Multiple Choice Radiation", 5).ToArray();
        }
        else
        {
            Debug.LogError("DatabaseManager is null!");
        }
    }

    void InitializeScenarioElements()
    {
        if (warmRoomPanel != null) warmRoomPanel.SetActive(false);
        if (warmAirArrows != null) warmAirArrows.SetActive(false);
        if (coolAirArrows != null) coolAirArrows.SetActive(false);
        if (solarPanelPanel != null) solarPanelPanel.SetActive(false);

        if (heatEffect != null)
            heatEffect.color = new Color(heatEffect.color.r, heatEffect.color.g, heatEffect.color.b, 0);
    }

    void InitializeModals()
    {
        if (passedModal != null)
        {
            passedModal.SetActive(false);
            // Canvas modalCanvas = passedModal.GetComponent<Canvas>();
            // if (modalCanvas == null) modalCanvas = passedModal.AddComponent<Canvas>();
            // modalCanvas.overrideSorting = true;
            // modalCanvas.sortingOrder = 100;
        }

        if (failedModal != null)
        {
            failedModal.SetActive(false);
            // Canvas modalCanvas = failedModal.GetComponent<Canvas>();
            // if (modalCanvas == null) modalCanvas = failedModal.AddComponent<Canvas>();
            // modalCanvas.overrideSorting = true;
            // modalCanvas.sortingOrder = 100;
        }
    }

    void SetupButtonListeners()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryCurrentScenario);
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueToNextScenario);

        // NEW: Setup retake quiz button
        if (retakeQuizButton != null)
            retakeQuizButton.onClick.AddListener(RetakeEntireQuiz);
    }

    void ShowIntro()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);
        if (messagePanel != null) messagePanel.SetActive(true);
        if (warmRoomPanel != null) warmRoomPanel.SetActive(false);

        // Reset quiz state for entire game
        currentScenarioScore = 0;
        totalQuestionsAnswered = 0;
        currentScenario = 1;
        totalCorrectAnswers = 0;
        totalQuestionsInGame = 0;
        UpdateScoreDisplay();
    }

    public void StartGame()
    {
        Debug.Log("StartGame called!");

        if (messagePanel != null) messagePanel.SetActive(true);

        if (dialogueManager != null)
        {
            Debug.Log("DialogueManager found, starting dialogue...");
            dialogueManager.StartDialogue(0);
            StartCoroutine(WaitForDialogueThen(() =>
            {
                Debug.Log("Coroutine finished, showing game panels...");
                if (gamePanel != null) gamePanel.SetActive(true);
                isHeating = false;
                heatProgress = 0f;
                if (heatTheMetalPanel != null) heatTheMetalPanel.SetActive(true);
            }));
        }
        else
        {
            Debug.LogWarning("DialogueManager is NULL, skipping dialogue!");
            if (gamePanel != null) gamePanel.SetActive(true);
            isHeating = false;
            heatProgress = 0f;
            if (heatTheMetalPanel != null) heatTheMetalPanel.SetActive(true);
        }
    }

    // NEW: OnEnable method to handle game object reactivation
    void OnEnable()
    {
        // Only trigger retake if the game has been played before
        if (originalPositionsStored && totalQuestionsInGame > 0)
        {
            Debug.Log("GameObject reactivated - triggering quiz retake");
            RetakeEntireQuiz();
        }
    }

    IEnumerator WaitForDialogueThen(System.Action onFinish)
    {
        if (dialogueManager != null)
        {
            while (!dialogueManager.dialogueFinished)
            {
                Debug.Log("Waiting... dialogueFinished = " + dialogueManager.dialogueFinished);
                yield return null;
            }
        }
        Debug.Log("Dialogue finished! Now showing game panel.");
        onFinish?.Invoke();
    }

    void Update()
    {
        if (!inScenario2 && isHeating)
        {
            heatProgress += Time.deltaTime;
            if (heatEffect != null)
            {
                Color c = heatEffect.color;
                c.a = Mathf.Clamp01(heatProgress / heatingTime);
                heatEffect.color = c;
            }

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
        currentScenario = 1;
        currentScenarioScore = 0;
        totalQuestionsAnswered = 0;

        if (gamePanel != null) gamePanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        ShowQuestionScenario1();
    }

    void ShowQuestionScenario1()
    {
        ShowQuestion(questionsScenario1, "Scenario 1");
    }

    void ShowQuestionScenario2()
    {
        ShowQuestion(questionsScenario2, "Scenario 2");
    }

    void ShowQuestionScenario3()
    {
        ShowQuestion(questionsScenario3, "Scenario 3");
    }

    // FIXED: Unified question display method
    void ShowQuestion(MultipleChoice.MultipleChoiceQuestions[] questions, string scenarioName)
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogError($"Questions for {scenarioName} is null or empty!");
            return;
        }

        if (currentQuestionIndex >= questions.Length)
        {
            Debug.LogError($"Current question index {currentQuestionIndex} is out of bounds for {scenarioName}!");
            return;
        }

        MultipleChoice.MultipleChoiceQuestions q = questions[currentQuestionIndex];
        if (questionTextUI != null)
        {
            questionTextUI.text = q.question;
        }

        // FIXED: Reset button state properly
        ResetButtonColors();
        buttonsInteractable = true;

        // FIXED: Clear all button listeners before setting up new ones
        ClearAllButtonListeners();

        // FIXED: Setup buttons with proper validation
        SetupAnswerButtons(q);

        StartQuestionTimer();

        Debug.Log($"Showing {scenarioName} question {currentQuestionIndex + 1}: {q.question}");
        Debug.Log($"Correct answer index: {q.correctIndex}");
    }

    // FIXED: New method to safely clear all button listeners
    void ClearAllButtonListeners()
    {
        if (answerButtons == null) return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].interactable = true;
            }
        }
    }

    // DIAGNOSTIC: Debug method to identify which physical button was clicked
    void SetupAnswerButtons(MultipleChoice.MultipleChoiceQuestions q)
{
    if (answerButtons == null || q.options == null)
    {
        Debug.LogError("Answer buttons or question options are null!");
        return;
    }

    Debug.Log("=== BUTTON SETUP DEBUG ===");
    Debug.Log($"Question: {q.question}");
    Debug.Log($"Choices count: {q.options.Length}");
    Debug.Log($"Correct answer index: {q.correctIndex}");
    Debug.Log($"Available buttons: {answerButtons.Length}");

    for (int i = 0; i < answerButtons.Length; i++)
    {
        if (answerButtons[i] != null)
        {
            if (i < q.options.Length)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].interactable = true;

                if (answerButtonTexts != null && i < answerButtonTexts.Length && answerButtonTexts[i] != null)
                {
                    answerButtonTexts[i].text = q.options[i];
                }

                ButtonClickTracker tracker = answerButtons[i].GetComponent<ButtonClickTracker>();
                if (tracker == null)
                {
                    tracker = answerButtons[i].gameObject.AddComponent<ButtonClickTracker>();
                }
                tracker.buttonIndex = i;
                tracker.quizManager = this;
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    Debug.Log("=== END BUTTON SETUP ===");
}


    // DIAGNOSTIC: Method called by ButtonClickTracker
    public void OnButtonClicked(int buttonIndex, GameObject buttonObject)
    {
        Debug.Log($"=== BUTTON CLICK DEBUG ===");
        Debug.Log($"Physical button clicked: {buttonObject.name}");
        Debug.Log($"Button index from tracker: {buttonIndex}");
        Debug.Log($"Button text: {(answerButtonTexts[buttonIndex] != null ? answerButtonTexts[buttonIndex].text : "NULL")}");

        if (!buttonsInteractable)
        {
            Debug.Log("Buttons not interactable, ignoring click");
            return;
        }

        SelectAnswer(buttonIndex);
    }

    // FIXED: New method that properly handles button index
    void SelectAnswerByIndex(int index)
    {
        Debug.Log($"SelectAnswerByIndex called with index: {index}");

        if (!buttonsInteractable)
        {
            Debug.Log("Buttons not interactable, ignoring click");
            return;
        }

        SelectAnswer(index);
    }

    // FIXED: Improved answer selection with better debugging
    public void SelectAnswer(int index)
{
    if (!buttonsInteractable)
    {
        Debug.Log("Buttons not interactable, ignoring click");
        return;
    }

    Debug.Log($"SelectAnswer called with index: {index}");

    StopQuestionTimer();
    buttonsInteractable = false;

    // Use MultipleChoiceQuestions instead of Question
    MultipleChoice.MultipleChoiceQuestions q = GetCurrentQuestion();
    if (q == null)
    {
        Debug.LogError("Current question is null!");
        return;
    }

    // Bounds check for safety
    if (index < 0 || index >= q.options.Length)
    {
        Debug.LogError($"Selected index {index} is out of bounds for question with {q.options.Length} options!");
        return;
    }

    bool isCorrect = index == q.correctIndex;

    Debug.Log($"Question: {q.question}");
    Debug.Log($"Selected choice {index}: {q.options[index]}");
    Debug.Log($"Correct answer index: {q.correctIndex}");
    Debug.Log($"Correct answer: {q.options[q.correctIndex]}");
    Debug.Log($"Answer is correct: {isCorrect}");

    // Update score
    totalQuestionsAnswered++;
    totalQuestionsInGame++;
    if (isCorrect)
    {
        currentScenarioScore++;
        totalCorrectAnswers++;
    }

    UpdateScoreDisplay();

    StartCoroutine(HandleAnswerFeedback(index, q.correctIndex, isCorrect, () =>
    {
        currentQuestionIndex++;
        if (ShouldShowNextQuestion())
            ShowNextQuestion();
        else
            EndCurrentScenario();
    }));
}


    // FIXED: Improved feedback handling with better validation
    IEnumerator HandleAnswerFeedback(int selectedIndex, int correctIndex, bool isCorrect, System.Action onComplete)
    {
        Debug.Log($"HandleAnswerFeedback: selected={selectedIndex}, correct={correctIndex}, isCorrect={isCorrect}");

        // Disable all buttons immediately
        SetAllButtonsInteractable(false);

        yield return new WaitForSeconds(0.1f);

        // FIXED: Better validation for button color changes
        if (selectedIndex == -1) // Timeout case
        {
            Debug.Log("Timeout - showing correct answer");
            SetButtonColor(correctIndex, correctButtonColor);
            AudioManager.Instance.PlaySFX(wrong);
        }
        else if (isCorrect)
        {
            Debug.Log("Correct answer - highlighting in green");
            SetButtonColor(selectedIndex, correctButtonColor);
            AudioManager.Instance.PlaySFX(correct);
        }
        else
        {
            Debug.Log("Incorrect answer - showing red for selected, green for correct");
            SetButtonColor(selectedIndex, incorrectButtonColor);
            SetButtonColor(correctIndex, correctButtonColor);
            AudioManager.Instance.PlaySFX(wrong);
        }

        yield return new WaitForSeconds(2f);

        Debug.Log("Feedback complete, calling onComplete");
        onComplete?.Invoke();
    }

    // FIXED: New helper method to safely set all buttons' interactable state
    void SetAllButtonsInteractable(bool interactable)
    {
        if (answerButtons == null) return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].interactable = interactable;
            }
        }
    }

    // FIXED: Improved button color setting with validation
    void SetButtonColor(int buttonIndex, Color color)
    {
        if (buttonVisualImages == null)
        {
            Debug.LogWarning("buttonVisualImages array is null!");
            return;
        }

        if (buttonIndex < 0 || buttonIndex >= buttonVisualImages.Length)
        {
            Debug.LogWarning($"Button index {buttonIndex} is out of bounds! Array length: {buttonVisualImages.Length}");
            return;
        }

        if (buttonVisualImages[buttonIndex] != null)
        {
            buttonVisualImages[buttonIndex].color = color;
            Debug.Log($"Set button {buttonIndex} color to {color}");
        }
        else
        {
            Debug.LogWarning($"Button visual image at index {buttonIndex} is null!");
        }
    }

    // FIXED: Improved color reset with fallback to default color
    void ResetButtonColors()
    {
        if (buttonVisualImages == null)
        {
            Debug.LogWarning("buttonVisualImages is null in ResetButtonColors");
            return;
        }

        for (int i = 0; i < buttonVisualImages.Length; i++)
        {
            if (buttonVisualImages[i] != null)
            {
                if (originalButtonColors != null && i < originalButtonColors.Length)
                {
                    buttonVisualImages[i].color = originalButtonColors[i];
                }
                else
                {
                    buttonVisualImages[i].color = defaultButtonColor;
                }
            }
        }

        Debug.Log("Button colors reset");
    }

    // FIXED: Better original color storage with null checks
    void StoreOriginalButtonColors()
    {
        if (buttonVisualImages == null)
        {
            Debug.LogWarning("buttonVisualImages is null, cannot store original colors");
            return;
        }

        originalButtonColors = new Color[buttonVisualImages.Length];
        for (int i = 0; i < buttonVisualImages.Length; i++)
        {
            if (buttonVisualImages[i] != null)
            {
                originalButtonColors[i] = buttonVisualImages[i].color;
            }
            else
            {
                originalButtonColors[i] = defaultButtonColor;
                Debug.LogWarning($"Button visual image at index {i} is null, using default color");
            }
        }

        Debug.Log($"Stored {originalButtonColors.Length} original button colors");
    }

    // FIXED: Better question retrieval with validation
   MultipleChoice.MultipleChoiceQuestions GetCurrentQuestion()
    {
        MultipleChoice.MultipleChoiceQuestions[] currentQuestions = null;
        string scenarioName = "";

        switch (currentScenario)
        {
            case 1:
                currentQuestions = questionsScenario1;
                scenarioName = "Scenario 1";
                break;
            case 2:
                currentQuestions = questionsScenario2;
                scenarioName = "Scenario 2";
                break;
            case 3:
                currentQuestions = questionsScenario3;
                scenarioName = "Scenario 3";
                break;
            default:
                Debug.LogError($"Invalid scenario: {currentScenario}");
                return null;
        }

        if (currentQuestions == null || currentQuestions.Length == 0)
        {
            Debug.LogError($"Questions array for {scenarioName} is null or empty!");
            return null;
        }

        if (currentQuestionIndex < 0 || currentQuestionIndex >= currentQuestions.Length)
        {
            Debug.LogError($"Question index {currentQuestionIndex} is out of bounds for {scenarioName} (length: {currentQuestions.Length})");
            return null;
        }

        return currentQuestions[currentQuestionIndex];
    }

    bool ShouldShowNextQuestion()
    {
        switch (currentScenario)
        {
            case 1: return questionsScenario1 != null && currentQuestionIndex < questionsScenario1.Length;
            case 2: return questionsScenario2 != null && currentQuestionIndex < questionsScenario2.Length;
            case 3: return questionsScenario3 != null && currentQuestionIndex < questionsScenario3.Length;
            default: return false;
        }
    }

    void ShowNextQuestion()
    {
        Debug.Log($"Showing next question for scenario {currentScenario}, index {currentQuestionIndex}");

        switch (currentScenario)
        {
            case 1: ShowQuestionScenario1(); break;
            case 2: ShowQuestionScenario2(); break;
            case 3: ShowQuestionScenario3(); break;
        }
    }

    void EndCurrentScenario()
    {
        Debug.Log($"Ending scenario {currentScenario}");

        switch (currentScenario)
        {
            case 1: EndScenario1(); break;
            case 2: EndScenario2(); break;
            case 3: EndScenario3(); break;
        }
    }

    public void FirePlacedSuccess()
    {
        isHeating = true;
    }

    public void HeaterPlacedSuccess()
    {
        if (warmAirArrows != null) warmAirArrows.SetActive(true);
        if (coolAirArrows != null) coolAirArrows.SetActive(true);

        if (arrowAnim != null) StopCoroutine(arrowAnim);
        arrowAnim = StartCoroutine(AnimateArrows());
        StartQuizScenario2();
    }

    private IEnumerator AnimateArrows()
    {
        if (warmAirArrows == null || coolAirArrows == null) yield break;

        Vector3 warmStart = warmAirArrows.transform.localPosition;
        Vector3 coolStart = coolAirArrows.transform.localPosition;

        while (warmAirArrows != null && coolAirArrows != null)
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

        currentScenario = 2;
        currentScenarioScore = 0;
        totalQuestionsAnswered = 0;

        if (gamePanel != null) gamePanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        ShowQuestionScenario2();
    }

    void EndScenario1()
    {
        if (quizPanel != null) quizPanel.SetActive(false);

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(1);
            StartCoroutine(WaitForDialogueThen(() =>
            {
                if (heatTheMetalPanel != null) heatTheMetalPanel.SetActive(false);
                StartScenario2();
            }));
        }
        else
        {
            if (heatTheMetalPanel != null) heatTheMetalPanel.SetActive(false);
            StartScenario2();
        }
    }

    void StartScenario2()
    {
        currentScenario = 2;
        currentScenarioScore = 0;
        totalQuestionsAnswered = 0;

        inScenario2 = true;
        if (gamePanel != null) gamePanel.SetActive(true);
        if (warmRoomPanel != null) warmRoomPanel.SetActive(true);
        if (warmAirArrows != null) warmAirArrows.SetActive(false);
        if (coolAirArrows != null) coolAirArrows.SetActive(false);
    }

    void EndScenario2()
    {
        if (quizPanel != null) quizPanel.SetActive(false);

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(2);
            StartCoroutine(WaitForDialogueThen(() =>
            {
                if (warmRoomPanel != null) warmRoomPanel.SetActive(false);
                StartScenario3();
            }));
        }
        else
        {
            if (warmRoomPanel != null) warmRoomPanel.SetActive(false);
            StartScenario3();
        }
    }

    void StartScenario3()
    {
        currentScenario = 3;
        currentScenarioScore = 0;
        totalQuestionsAnswered = 0;

        if (gamePanel != null) gamePanel.SetActive(true);
        if (solarPanelPanel != null) solarPanelPanel.SetActive(true);

        if (sunlightBeam != null)
            sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b, 0);

        correctHoldTime = 0f;
        scenario3Active = true;
        scenario3Completed = false;

        if (rotationSlider != null)
        {
            rotationSlider.onValueChanged.RemoveAllListeners();
            rotationSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void OnSliderChanged(float value)
    {
        if (solarPanelImage != null)
        {
            float newX = Mathf.Lerp(-200f, 200f, value);
            Vector3 pos = solarPanelImage.localPosition;
            pos.x = newX;
            solarPanelImage.localPosition = pos;
        }
    }

    void HandleScenario3()
    {
        if (solarPanelImage == null) return;

        float currentX = solarPanelImage.localPosition.x;
        float diff = Mathf.Abs(currentX - targetXPosition);

        if (diff <= positionTolerance)
        {
            correctHoldTime += Time.deltaTime;
            if (sunlightBeam != null)
            {
                sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b,
                    Mathf.MoveTowards(sunlightBeam.color.a, 1f, beamFadeSpeed * Time.deltaTime));
            }

            if (correctHoldTime >= holdTimeRequired)
            {
                scenario3Completed = true;
                scenario3Active = false;
                if (rotationSlider != null) rotationSlider.interactable = false;
                StartQuizScenario3();
            }
        }
        else
        {
            correctHoldTime = 0f;
            if (sunlightBeam != null)
            {
                sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b,
                    Mathf.MoveTowards(sunlightBeam.color.a, 0f, beamFadeSpeed * Time.deltaTime));
            }
        }
    }

    void StartQuizScenario3()
    {
        currentScenario = 3;
        currentScenarioScore = 0;
        totalQuestionsAnswered = 0;

        if (gamePanel != null) gamePanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        ShowQuestionScenario3();
    }

    void EndScenario3()
    {
        if (quizPanel != null) quizPanel.SetActive(false);

        float overallPercentage = CalculateOverallPercentage();
        Debug.Log($"Game completed! Overall percentage: {overallPercentage}%");

        if (overallPercentage >= PASSING_PERCENTAGE)
        {
            ShowPassedModal();
        }
        else
        {
            ShowFailedModal();
        }
    }

    // TIMER SYSTEM
    void StartQuestionTimer()
    {
        if (questionTimerCoroutine != null)
            StopCoroutine(questionTimerCoroutine);

        currentQuestionTimer = questionTimeLimit;
        questionTimerActive = true;
        questionTimerCoroutine = StartCoroutine(QuestionTimerCoroutine());
    }

    void StopQuestionTimer()
    {
        questionTimerActive = false;
        if (questionTimerCoroutine != null)
        {
            StopCoroutine(questionTimerCoroutine);
            questionTimerCoroutine = null;
        }
    }

    IEnumerator QuestionTimerCoroutine()
    {
        while (questionTimerActive && currentQuestionTimer > 0)
        {
            currentQuestionTimer -= Time.deltaTime;
            UpdateTimerDisplay();
            yield return null;
        }

        if (questionTimerActive)
        {
            OnQuestionTimeout();
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(currentQuestionTimer);
            timerText.text = $"{seconds}";

            if (seconds <= 10)
                timerText.color = Color.red;
        }

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = currentQuestionTimer / questionTimeLimit;
        }
    }

   void OnQuestionTimeout()
    {
        if (!buttonsInteractable) return;

        Debug.Log("Question timed out");
        buttonsInteractable = false;
        questionTimerActive = false;

        MultipleChoice.MultipleChoiceQuestions q = GetCurrentQuestion();
        if (q != null)
        {
            totalQuestionsAnswered++;
            totalQuestionsInGame++;
            UpdateScoreDisplay();

            StartCoroutine(HandleAnswerFeedback(-1, q.correctIndex, false, () =>
            {
                currentQuestionIndex++;
                if (ShouldShowNextQuestion())
                    ShowNextQuestion();
                else
                    EndCurrentScenario();
            }));
        }
    }


    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{totalCorrectAnswers}";
        }
    }

    float CalculatePercentage()
    {
        if (totalQuestionsAnswered == 0) return 0f;
        return (float)currentScenarioScore / totalQuestionsAnswered * 100f;
    }

    float CalculateOverallPercentage()
    {
        if (totalQuestionsInGame == 0) return 0f;
        return (float)totalCorrectAnswers / totalQuestionsInGame * 100f;
    }

    void ShowPassedModal()
    {
        HideAllPanels();

        if (passedModal != null)
        {
            float overallPercentage = CalculateOverallPercentage();
            if (passedScoreText != null)
                passedScoreText.text = $"Congratulations! You passed with {overallPercentage:F0}%!\nFinal Score: {totalCorrectAnswers}/{totalQuestionsInGame}";

            passedModal.SetActive(true);
            AudioManager.Instance.PlaySFX(passed);
            passedModal.transform.SetAsLastSibling();
            Debug.Log("Passed modal activated");
        }
    }

    void ShowFailedModal()
    {
        HideAllPanels();

        if (failedModal != null)
        {
            float overallPercentage = CalculateOverallPercentage();
            if (failedScoreText != null)
                failedScoreText.text = $"You scored {overallPercentage:F0}%. You need 70% or higher to pass.\nFinal Score: {totalCorrectAnswers}/{totalQuestionsInGame}";

            failedModal.SetActive(true);
            AudioManager.Instance.PlaySFX(failed);
            failedModal.transform.SetAsLastSibling();
            Debug.Log("Failed modal activated");
        }
    }

    void HideAllPanels()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);
        if (warmRoomPanel != null) warmRoomPanel.SetActive(false);
        if (messagePanel != null) messagePanel.SetActive(false);
        if (solarPanelPanel != null) solarPanelPanel.SetActive(false);
    }

    public void RetryCurrentScenario()
    {
        if (passedModal != null) passedModal.SetActive(false);
        if (failedModal != null) failedModal.SetActive(false);
        ShowIntro();
    }

    public void ContinueToNextScenario()
    {
        if (passedModal != null) passedModal.SetActive(false);
        if (failedModal != null) failedModal.SetActive(false);

        if (messagePanel != null) messagePanel.SetActive(true);
        if (messageText != null) messageText.text = "Excellent! You've completed all scenarios successfully!";
    }

    // LEGACY METHODS - Keep these for backwards compatibility if other scripts call them
    public void SelectAnswerScenario1(int index) { SelectAnswer(index); }
    public void SelectAnswerScenario2(int index) { SelectAnswer(index); }
    public void SelectAnswerScenario3(int index) { SelectAnswer(index); }
}
