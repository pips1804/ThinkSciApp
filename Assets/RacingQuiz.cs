using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ===================================================================
// Question Data Structure
// ===================================================================
[System.Serializable]
public class RacingQuizQuestion
{
    public string questionText;
    public string[] choices;
    public int correctAnswerIndex;
}

// ===================================================================
// RACING QUIZ SYSTEM - SLIDER HANDLES AS CARS
// ===================================================================
public class RacingQuiz : MonoBehaviour
{
    [Header("Dialogue System Integration")]
    public Dialogues dialogueSystem;
    public GameObject quizPanel; // Main quiz UI panel
    public GameObject gamePanel; // Game elements panel (sliders, etc.)

    [Header("UI References")]
    public Text questionText;
    public Button[] choiceButtons;
    public Text timerText;
    public Text scoreText;
    public Slider playerProgressSlider;
    public Slider enemyProgressSlider;
    public Text playerSpeed;
    public Text enemySpeed;
    public Slider questionProgressSlider;

    [Header("Game Objects - REMOVED (Using Slider Handles)")]
    // playerCar and enemyCar removed - using slider handles instead
    public GameObject victoryModal;
    public GameObject gameOverModal;

    [Header("Car Movement Settings")]
    public float carMoveSpeed = 5f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Slider Handle Animation")]
    public float handleAnimationDuration = 1f;

    [Header("Audio (Optional)")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip victorySound;
    public AudioClip gameOverSound;

    [Header("Game Settings")]
    public float questionTimeLimit = 60f;
    public int questionsToWin = 10;
    public int wrongAnswersToLose = 5;

    private int playerSpeedValue = 0;
    private int enemySpeedValue = 0;
    private bool quizStarted = false;

    [Header("Database Integration")]
    public DatabaseManager databaseManager;
    public int quizId = 7; // set the Quiz ID
    public RacingQuizQuestion[] questions;

    // Game State Variables
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private int wrongAnswers = 0;
    private float timeRemaining;
    private bool isAnswerSelected = false;
    private bool gameEnded = false;

    // Slider Animation Variables
    private Coroutine playerSliderCoroutine;
    private Coroutine enemySliderCoroutine;

    // Colors
    private Color defaultButtonColor = Color.white;
    private Color correctColor = Color.green;
    private Color wrongColor = Color.red;

    public LessonLocker lessonHandler;
    public CategoryLocker categoryHandler;
    public int userID;
    public int categoryToUnlock;
    public int rewardItemID;

    // ===================================================================
    // UNITY LIFECYCLE - RESET ON ENABLE/DISABLE
    // ===================================================================
    void OnEnable()
    {
        ResetQuizFromStart();
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Start()
    {
        LoadQuestionsFromDatabase();
        SetQuizPanelsActive(false);
        InitializeGameVariables();
    }

    void Update()
    {
        // Check if dialogue is finished and quiz hasn't started yet
        if (!quizStarted && dialogueSystem != null && dialogueSystem.dialogueFinished)
        {
            StartQuiz();
        }

        // Regular quiz update logic
        if (quizStarted && !gameEnded && !isAnswerSelected)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timeRemaining <= 0)
            {
                HandleTimeOut();
            }
        }
    }

    // ===================================================================
    // DIALOGUE INTEGRATION
    // ===================================================================
    void StartQuiz()
    {
        
        quizStarted = true;
        SetQuizPanelsActive(true);
        SetupQuizUI();
        LoadNextQuestion();
    }

    void SetQuizPanelsActive(bool active)
    {
        if (quizPanel != null) quizPanel.SetActive(active);
        if (gamePanel != null) gamePanel.SetActive(active);
    }

    // ===================================================================
    // QUIZ RESET FUNCTIONS
    // ===================================================================
    void ResetQuizFromStart()
    {
        LoadQuestionsFromDatabase();
        StopAllCoroutines();
        ResetGameState();
        SetQuizPanelsActive(false);
    }

    void ResetGameState()
    {
        currentQuestionIndex = 0;
        correctAnswers = 0;
        wrongAnswers = 0;
        timeRemaining = questionTimeLimit;
        isAnswerSelected = false;
        gameEnded = false;
        quizStarted = false;
        playerSpeedValue = 0;
        enemySpeedValue = 0;

        if (victoryModal != null) victoryModal.SetActive(false);
        if (gameOverModal != null) gameOverModal.SetActive(false);

        // Reset slider values
        if (playerProgressSlider != null) playerProgressSlider.value = 0;
        if (enemyProgressSlider != null) enemyProgressSlider.value = 0;

        ResetAllButtonColors();
    }

    void ResetAllButtonColors()
    {
        if (choiceButtons != null)
        {
            foreach (Button button in choiceButtons)
            {
                if (button != null)
                {
                    ResetButtonColor(button);
                    button.interactable = true;
                }
            }
        }
    }

    // ===================================================================
    // SLIDER OPACITY FIX
    // ===================================================================
    void SetSliderFullAlpha(Slider slider)
    {
        // Fix background image alpha
        Image backgroundImage = slider.GetComponent<Image>();
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = 1f; // Full alpha
            backgroundImage.color = bgColor;
        }

        // Fix fill area alpha
        if (slider.fillRect != null)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                Color fillColor = fillImage.color;
                fillColor.a = 1f; // Full alpha
                fillImage.color = fillColor;
            }
        }

        // Fix handle alpha
        if (slider.handleRect != null)
        {
            Image handleImage = slider.handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                Color handleColor = handleImage.color;
                handleColor.a = 1f; // Full alpha
                handleImage.color = handleColor;
            }
        }

        // Override ColorBlock to prevent disabled state opacity
        ColorBlock colors = slider.colors;
        colors.disabledColor = Color.white; // Keep full color when disabled
        slider.colors = colors;
    }

    // ===================================================================
    // GAME INITIALIZATION
    // ===================================================================
    void InitializeGameVariables()
    {
        correctAnswers = 0;
        wrongAnswers = 0;
        gameEnded = false;
        playerSpeedValue = 0;
        enemySpeedValue = 0;
        timeRemaining = questionTimeLimit;
    }

    void SetupQuizUI()
    {
        // Initialize progress sliders
        if (playerProgressSlider != null)
        {
            playerProgressSlider.minValue = 0;
            playerProgressSlider.maxValue = questionsToWin;
            playerProgressSlider.value = 0;
            playerProgressSlider.interactable = false; // Prevent manual interaction

            // Fix opacity issue - ensure full alpha on all slider components
            SetSliderFullAlpha(playerProgressSlider);
        }

        if (enemyProgressSlider != null)
        {
            enemyProgressSlider.minValue = 0;
            enemyProgressSlider.maxValue = wrongAnswersToLose;
            enemyProgressSlider.value = 0;
            enemyProgressSlider.interactable = false; // Prevent manual interaction

            // Fix opacity issue - ensure full alpha on all slider components
            SetSliderFullAlpha(enemyProgressSlider);
        }

        // Setup choice buttons
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(index));
        }

        if (questionProgressSlider != null)
        {
            questionProgressSlider.minValue = 0;
            questionProgressSlider.maxValue = questions.Length; // total number of questions
            questionProgressSlider.value = 0;
            questionProgressSlider.interactable = true;
        }
    }

    // ===================================================================
    // QUESTION MANAGEMENT
    // ===================================================================
    void LoadNextQuestion()
    {
        if (currentQuestionIndex >= questions.Length)
        {
            currentQuestionIndex = 0;
        }

        RacingQuizQuestion currentQuestion = questions[currentQuestionIndex];
        questionText.text = currentQuestion.questionText;

        for (int i = 0; i < choiceButtons.Length && i < currentQuestion.choices.Length; i++)
        {
            choiceButtons[i].GetComponentInChildren<Text>().text = currentQuestion.choices[i];
            ResetButtonColor(choiceButtons[i]);
            choiceButtons[i].interactable = true;
        }

        timeRemaining = questionTimeLimit;
        isAnswerSelected = false;

        UpdateScoreDisplay();

        if (questionProgressSlider != null)
        {
            questionProgressSlider.value = currentQuestionIndex + 1; // +1 because index starts at 0
        }
    }

    void LoadCurrentQuestion()
    {
        if (questions.Length == 0) return;

        RacingQuizQuestion currentQuestion = questions[currentQuestionIndex];
        questionText.text = currentQuestion.questionText;

        for (int i = 0; i < choiceButtons.Length && i < currentQuestion.choices.Length; i++)
        {
            choiceButtons[i].GetComponentInChildren<Text>().text = currentQuestion.choices[i];
            ResetButtonColor(choiceButtons[i]);
            choiceButtons[i].interactable = true;
        }

        if (questionProgressSlider != null)
        {
            questionProgressSlider.value = currentQuestionIndex + 1;
        }

        UpdateScoreDisplay();
    }

    public void OnChoiceSelected(int selectedIndex)
    {
        if (isAnswerSelected || gameEnded || !quizStarted) return;

        isAnswerSelected = true;
        RacingQuizQuestion currentQuestion = questions[currentQuestionIndex];

        foreach (Button button in choiceButtons)
        {
            button.interactable = false;
        }

        if (selectedIndex == currentQuestion.correctAnswerIndex)
        {
            SetButtonColor(choiceButtons[selectedIndex], correctColor);
            correctAnswers++;
            MovePlayerForward();
            playerSpeedValue += 10;
            AudioManager.Instance.PlaySFX(correctSound);
            if (correctAnswers >= questionsToWin)
            {
                StartCoroutine(ShowResultWithDelay(true));
                return;
            }
        }
        else
        {
            SetButtonColor(choiceButtons[selectedIndex], wrongColor);
            SetButtonColor(choiceButtons[currentQuestion.correctAnswerIndex], correctColor);
            wrongAnswers++;
            MoveEnemyForward();
            enemySpeedValue += 10;
            AudioManager.Instance.PlaySFX(wrongSound);
            if (wrongAnswers >= wrongAnswersToLose)
            {
                StartCoroutine(ShowResultWithDelay(false));
                return;
            }
        }

        StartCoroutine(LoadNextQuestionWithDelay());
    }

    void HandleTimeOut()
    {
        if (isAnswerSelected || gameEnded || !quizStarted) return;

        isAnswerSelected = true;
        RacingQuizQuestion currentQuestion = questions[currentQuestionIndex];

        foreach (Button button in choiceButtons)
        {
            button.interactable = false;
        }

        SetButtonColor(choiceButtons[currentQuestion.correctAnswerIndex], correctColor);
        wrongAnswers++;
        MoveEnemyForward();

        if (wrongAnswers >= wrongAnswersToLose)
        {
            StartCoroutine(ShowResultWithDelay(false));
            return;
        }

        StartCoroutine(LoadNextQuestionWithDelay());
    }

    // ===================================================================
    // SLIDER MOVEMENT SYSTEM (HANDLES AS CARS)
    // ===================================================================
    void MovePlayerForward()
    {
        if (playerProgressSlider != null)
        {
            if (playerSliderCoroutine != null) StopCoroutine(playerSliderCoroutine);
            playerSliderCoroutine = StartCoroutine(AnimateSliderValue(playerProgressSlider, correctAnswers));
        }
    }

    void MoveEnemyForward()
    {
        if (enemyProgressSlider != null)
        {
            if (enemySliderCoroutine != null) StopCoroutine(enemySliderCoroutine);
            enemySliderCoroutine = StartCoroutine(AnimateSliderValue(enemyProgressSlider, wrongAnswers));
        }
    }

    IEnumerator AnimateSliderValue(Slider slider, int targetValue)
    {
        float startValue = slider.value;
        float elapsedTime = 0f;

        while (elapsedTime < handleAnimationDuration)
        {
            float t = elapsedTime / handleAnimationDuration;
            float curveValue = moveCurve.Evaluate(t);
            slider.value = Mathf.Lerp(startValue, targetValue, curveValue);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        slider.value = targetValue;
    }

    // ===================================================================
    // UI MANAGEMENT
    // ===================================================================
    void UpdateTimerDisplay()
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = seconds.ToString() + "";

        if (timeRemaining <= 10f)
        {
            timerText.color = Color.red;
        }
    }

    void UpdateScoreDisplay()
    {
        scoreText.text = $"{correctAnswers}";
        playerSpeed.text = $"Player: {playerSpeedValue} m/s";
        enemySpeed.text = $"Enemy: {enemySpeedValue} m/s";
    }

    void SetButtonColor(Button button, Color color)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.pressedColor = color;
        colors.selectedColor = color;
        button.colors = colors;
    }

    void ResetButtonColor(Button button)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = defaultButtonColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = defaultButtonColor;
        colors.highlightedColor = Color.grey;
        colors.pressedColor = Color.grey;
        colors.selectedColor = Color.grey;
        button.colors = colors;
    }

    // ===================================================================
    // COROUTINES
    // ===================================================================
    IEnumerator LoadNextQuestionWithDelay()
    {
        yield return new WaitForSeconds(2f);

        currentQuestionIndex++;
        LoadNextQuestion();
    }

    IEnumerator ShowResultWithDelay(bool playerWon)
    {
        yield return new WaitForSeconds(2f);

        gameEnded = true;

        if (playerWon)
        {
            databaseManager.AddUserItem(userID, rewardItemID);
            databaseManager.UnlockCategoryForUser(userID, categoryToUnlock);
            categoryHandler.RefreshCategoryLocks();
            databaseManager.CheckAndUnlockAllLessons(userID);
            lessonHandler.RefreshLessonLocks();
            databaseManager.AddCoin(userID, 100);
            victoryModal.SetActive(true);
            AudioManager.Instance.PlaySFX(victorySound);
        }
        else
        {
            databaseManager.AddCoin(userID, 100);
            gameOverModal.SetActive(true);
            AudioManager.Instance.PlaySFX(gameOverSound);
        }
        databaseManager.SaveQuizAndScore(userID, quizId, correctAnswers);
    }

    // ===================================================================
    // PUBLIC METHODS FOR UI BUTTONS
    // ===================================================================
    public void RetakeQuizFromStart()
    {
        Debug.Log("Retaking quiz from start!");
        ResetQuizFromStart();

        // Restart dialogue if available
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue(0);
        }
    }

    public void RestartGame()
    {
        victoryModal.SetActive(false);
        gameOverModal.SetActive(false);

        ResetQuizFromStart();

        // Restart dialogue if available
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue(0);
        }
    }

    public void RestartCurrentQuestion()
    {
        if (!quizStarted) return;

        isAnswerSelected = false;
        timeRemaining = questionTimeLimit;

        ResetAllButtonColors();
        LoadCurrentQuestion();
    }

    public void GoToQuestion(int questionNumber)
    {
        if (!quizStarted) return;

        if (questionNumber >= 1 && questionNumber <= questions.Length)
        {
            currentQuestionIndex = questionNumber - 1;
            isAnswerSelected = false;
            timeRemaining = questionTimeLimit;

            ResetAllButtonColors();
            LoadCurrentQuestion();
        }
    }

    private void LoadQuestionsFromDatabase()
    {
        if (databaseManager != null)
        {
            List<MultipleChoice.MultipleChoiceQuestions> mcqList = 
                databaseManager.GetRandomUnusedQuestions(quizId);

            Debug.Log("Questions fetched: " + mcqList.Count);

            questions = new RacingQuizQuestion[mcqList.Count];

            for (int i = 0; i < mcqList.Count; i++)
            {
                questions[i] = new RacingQuizQuestion
                {
                    questionText = mcqList[i].question,
                    choices = mcqList[i].options,
                    correctAnswerIndex = mcqList[i].correctIndex
                };
            }
        }
        else
        {
            Debug.LogError("DatabaseManager is null! Assign it in the Inspector.");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
