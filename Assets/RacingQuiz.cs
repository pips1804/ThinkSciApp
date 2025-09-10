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

    [Header("Questions Database")]
    public RacingQuizQuestion[] questions = new RacingQuizQuestion[]
    {
        new RacingQuizQuestion
        {
            questionText = "A car travels 100m in 10 seconds. What is its average velocity?",
            choices = new string[] { "5 m/s", "10 m/s", "20 m/s", "100 m/s" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "Which graph shows constant velocity?",
            choices = new string[] { "Curved line up", "Straight horizontal line", "Curved line down", "Zigzag line" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "What is acceleration when velocity changes from 0 to 20 m/s in 4 seconds?",
            choices = new string[] { "4 m/s²", "5 m/s²", "20 m/s²", "80 m/s²" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "In a distance-time graph, what does the slope represent?",
            choices = new string[] { "Distance", "Time", "Speed", "Acceleration" },
            correctAnswerIndex = 2
        },
        new RacingQuizQuestion
        {
            questionText = "A car decelerates from 30 m/s to 10 m/s in 5 seconds. What is the acceleration?",
            choices = new string[] { "-4 m/s²", "-2 m/s²", "4 m/s²", "2 m/s²" },
            correctAnswerIndex = 0
        },
        new RacingQuizQuestion
        {
            questionText = "What happens to kinetic energy when speed doubles?",
            choices = new string[] { "Doubles", "Triples", "Quadruples", "Stays same" },
            correctAnswerIndex = 2
        },
        new RacingQuizQuestion
        {
            questionText = "Distance = Speed × Time. If speed is 25 m/s and time is 8s, what's the distance?",
            choices = new string[] { "200m", "150m", "300m", "33m" },
            correctAnswerIndex = 0
        },
        new RacingQuizQuestion
        {
            questionText = "What force is needed to accelerate a 1000kg car at 2 m/s²?",
            choices = new string[] { "500N", "1000N", "2000N", "4000N" },
            correctAnswerIndex = 2
        },
        new RacingQuizQuestion
        {
            questionText = "In uniform motion, what is the acceleration?",
            choices = new string[] { "Increasing", "Decreasing", "Zero", "Constant but not zero" },
            correctAnswerIndex = 2
        },
        new RacingQuizQuestion
        {
            questionText = "What does the area under a velocity-time graph represent?",
            choices = new string[] { "Acceleration", "Distance", "Speed", "Force" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "A racing car accelerates from rest to 60 m/s in 12 seconds. What is its acceleration?",
            choices = new string[] { "3 m/s²", "5 m/s²", "7.2 m/s²", "720 m/s²" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "If a car maintains constant speed of 80 km/h, how far will it travel in 2.5 hours?",
            choices = new string[] { "160 km", "200 km", "240 km", "32 km" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "In a velocity-time graph, what does a horizontal line indicate?",
            choices = new string[] { "Increasing velocity", "Decreasing velocity", "Constant velocity", "Zero velocity" },
            correctAnswerIndex = 2
        },
        new RacingQuizQuestion
        {
            questionText = "What is the average speed if a car travels 150 km in 3 hours?",
            choices = new string[] { "45 km/h", "50 km/h", "60 km/h", "453 km/h" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "A ball is thrown upward. At the highest point of its trajectory, what is its velocity?",
            choices = new string[] { "Maximum", "Minimum but not zero", "Zero", "Cannot be determined" },
            correctAnswerIndex = 2
        },
        new RacingQuizQuestion
        {
            questionText = "Which equation correctly represents Newton's second law?",
            choices = new string[] { "F = ma", "v = u + at", "s = ut + ½at²", "v² = u² + 2as" },
            correctAnswerIndex = 0
        },
        new RacingQuizQuestion
        {
            questionText = "A car brakes and comes to a stop. What type of acceleration does it experience?",
            choices = new string[] { "Positive acceleration", "Negative acceleration", "Zero acceleration", "Variable acceleration" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "If initial velocity is 15 m/s and final velocity is 35 m/s after 10 seconds, what is the acceleration?",
            choices = new string[] { "1 m/s²", "2 m/s²", "3.5 m/s²", "20 m/s²" },
            correctAnswerIndex = 1
        },
        new RacingQuizQuestion
        {
            questionText = "What is displacement if a car travels 80m north, then 60m south?",
            choices = new string[] { "20m north", "140m", "20m south", "0m" },
            correctAnswerIndex = 0
        },
        new RacingQuizQuestion
        {
            questionText = "In free fall (ignoring air resistance), all objects fall with the same:",
            choices = new string[] { "Velocity", "Force", "Acceleration", "Mass" },
            correctAnswerIndex = 2
        }
    };

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
        // Hide quiz panels initially and let dialogue system start itself
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
            victoryModal.SetActive(true);
            AudioManager.Instance.PlaySFX(victorySound);
        }
        else
        {
            gameOverModal.SetActive(true);
            AudioManager.Instance.PlaySFX(gameOverSound);
        }
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

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
