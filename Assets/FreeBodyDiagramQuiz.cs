using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class FreeBodyQuizQuestion
{
    [TextArea(3, 5)]
    public string questionText;
    public string[] answerOptions = new string[4];
    public int correctAnswerIndex;
}

public class FreeBodyDiagramQuiz : MonoBehaviour
{
    [Header("Game Setup")]
    [SerializeField] private List<FreeBodyQuizQuestion> questions = new List<FreeBodyQuizQuestion>();
    [SerializeField] private int finishLineDistance = 10;
    [SerializeField] private float questionTimeLimit = 30f;
    [SerializeField] private float passPercentage = 70f; // Pass threshold

    [Header("UI References - Question Display")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private Text questionText;
    [SerializeField] private Button[] answerButtons = new Button[4];
    [SerializeField] private Text[] answerTexts = new Text[4];
    [SerializeField] private Image[] answerButtonImages = new Image[4];

    [Header("Dialogue Integration")]
    [SerializeField] private Dialogues dialogueSystem; // Reference to dialogue system
    [SerializeField] private GameObject gameUIPanel; // Main game UI panel to hide/show
    [SerializeField] private int dialogueScenarioId = 0; // Which dialogue scenario to play

    [Header("UI References - Game Info")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text boxPositionText;
    [SerializeField] private Slider progressSlider;

    [Header("Visual Game Elements")]
    [SerializeField] private RectTransform boxImage;
    [SerializeField] private RectTransform gameArea;
    [SerializeField] private float gameAreaWidth = 800f;

    [Header("UI References - Modals")]
    [SerializeField] private GameObject victoryModal;
    [SerializeField] private GameObject gameOverModal;
    [SerializeField] private GameObject drawModal;
    [SerializeField] private Text victoryModalText; // Add reference to victory modal text
    [SerializeField] private Text gameOverModalText; // Add reference to game over modal text
    [SerializeField] private Button retakeButton; // Add reference to retake button

    [Header("Visual Feedback")]
    [SerializeField] private Color defaultButtonColor = Color.white;
    [SerializeField] private Color correctAnswerColor = Color.green;
    [SerializeField] private Color wrongAnswerColor = Color.red;
    [SerializeField] private float feedbackDisplayTime = 2f;

    [Header("Box Animation")]
    [SerializeField] private float boxMoveSpeed = 2f;
    [SerializeField] private AnimationCurve boxMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Game State
    private int currentQuestionIndex = 0;
    private int boxPosition = 0; // Positive = player side, Negative = enemy side
    private int correctAnswers = 0;
    private int wrongAnswers = 0;
    private int totalQuestionsAnswered = 0; // Track total questions answered
    private float currentQuestionTimer;
    private bool isAnswering = false;
    private bool gameEnded = false;
    private bool gameStarted = false; // Track if quiz has started after dialogue
    private bool hasInitialized = false; // Track if initialization has happened

    [Header("Sound Effects")]
    public AudioClip passedsound;
    public AudioClip failed;
    public AudioClip correct;
    public AudioClip wrong;
    private void Start()
    {
        hasInitialized = true;

        // Initialize dialogue system if available
        if (dialogueSystem != null)
        {
            StartDialogueBeforeQuiz();
        }
        else
        {
            // No dialogue system, start quiz immediately
            InitializeGame();
        }
    }

    private void StartDialogueBeforeQuiz()
    {
        // Hide game UI elements
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(false);
        }
        else
        {
            // If no main panel assigned, hide individual panels
            if (questionPanel != null) questionPanel.SetActive(false);
        }

        // Hide modals
        if (victoryModal != null) victoryModal.SetActive(false);
        if (gameOverModal != null) gameOverModal.SetActive(false);
        if (drawModal != null) drawModal.SetActive(false);

        gameStarted = false;

        // Start the dialogue
        dialogueSystem.StartDialogue(dialogueScenarioId);
    }

    private void Update()
    {
        // Check if dialogue is finished and game hasn't started yet
        if (dialogueSystem != null && !gameStarted && dialogueSystem.dialogueFinished)
        {
            StartQuizAfterDialogue();
        }

        if (!gameEnded && isAnswering && gameStarted)
        {
            UpdateTimer();
        }
    }

    private void StartQuizAfterDialogue()
    {
        gameStarted = true;

        // Show game UI elements
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(true);
        }
        else
        {
            // If no main panel assigned, show individual panels
            if (questionPanel != null) questionPanel.SetActive(true);
        }

        // Initialize and start the game
        InitializeGame();
    }

    // This will be called when the GameObject is enabled (reopening)
    private void OnEnable()
    {
        // Skip if this is the initial enable during Start()
        if (!hasInitialized) return;

        // Always reset when enabling the GameObject (reopening)
        if (dialogueSystem != null)
        {
            StartDialogueBeforeQuiz();
        }
        else
        {
            ResetGameForRetake();
        }
    }

    private void InitializeGame()
    {
        currentQuestionIndex = 0;
        boxPosition = 0;
        correctAnswers = 0;
        wrongAnswers = 0;
        totalQuestionsAnswered = 0;
        gameEnded = false;

        // Setup progress slider
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = questions.Count;
            progressSlider.value = 0;
        }


        // Position UI elements at center
        UpdateVisualElements();

        // Initialize button events
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int buttonIndex = i; // Capture for closure
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(buttonIndex));
        }

        // Initialize retake button
        if (retakeButton != null)
        {
            retakeButton.onClick.RemoveAllListeners();
            retakeButton.onClick.AddListener(RetakeGame);
        }

        // Hide modals
        if (victoryModal != null) victoryModal.SetActive(false);
        if (gameOverModal != null) gameOverModal.SetActive(false);
        if (drawModal != null) drawModal.SetActive(false);

        // Start first question
        DisplayCurrentQuestion();
        UpdateUI();
    }

    private void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Count)
        {
            EndGame();
            return;
        }

        FreeBodyQuizQuestion question = questions[currentQuestionIndex];

        // Display question
        if (questionText != null)
            questionText.text = question.questionText;

        // Display answer options
        for (int i = 0; i < answerButtons.Length && i < question.answerOptions.Length; i++)
        {
            if (answerTexts[i] != null)
                answerTexts[i].text = question.answerOptions[i];

            // Reset button colors
            if (answerButtonImages[i] != null)
                ResetButtonColor(answerButtonImages[i]);
            answerButtons[i].interactable = true;
        }

        // Start timer
        currentQuestionTimer = questionTimeLimit;
        isAnswering = true;

        UpdateUI();
    }

    private void OnAnswerSelected(int selectedIndex)
    {
        if (!isAnswering || gameEnded) return;

        isAnswering = false;
        FreeBodyQuizQuestion currentQuestion = questions[currentQuestionIndex];
        bool isCorrect = selectedIndex == currentQuestion.correctAnswerIndex;

        // Count this question as answered
        totalQuestionsAnswered++;

        if (progressSlider != null)
        {
            progressSlider.value = totalQuestionsAnswered;
        }


        // Visual feedback
        StartCoroutine(ShowAnswerFeedback(selectedIndex, currentQuestion.correctAnswerIndex, isCorrect));

        // Update game state
        if (isCorrect)
        {
            correctAnswers++;
            boxPosition++;
            AudioManager.Instance.PlaySFX(correct);
        }
        else
        {
            wrongAnswers++;
            boxPosition--;
            AudioManager.Instance.PlaySFX(wrong);
        }

        // Animate box movement
        StartCoroutine(AnimateBoxMovement());

        // Check for immediate win/loss
        if (boxPosition >= finishLineDistance)
        {
            StartCoroutine(DelayedVictory());
            return;
        }
        else if (boxPosition <= -finishLineDistance)
        {
            StartCoroutine(DelayedDefeat());
            return;
        }

        UpdateUI();
    }

    private IEnumerator ShowAnswerFeedback(int selectedIndex, int correctIndex, bool isCorrect)
    {
        // Disable all buttons
        foreach (Button button in answerButtons)
        {
            button.interactable = false;
        }

        // Show feedback colors
        if (isCorrect)
        {
            if (answerButtonImages[selectedIndex] != null)
                SetButtonColor(answerButtonImages[selectedIndex], correctAnswerColor);
        }
        else
        {
            if (answerButtonImages[selectedIndex] != null)
                SetButtonColor(answerButtonImages[selectedIndex], wrongAnswerColor);
            if (answerButtonImages[correctIndex] != null)
                SetButtonColor(answerButtonImages[correctIndex], correctAnswerColor);
        }

        yield return new WaitForSeconds(feedbackDisplayTime);

        // Move to next question
        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    private IEnumerator AnimateBoxMovement()
    {
        if (boxImage != null)
        {
            Vector2 startPos = boxImage.anchoredPosition;
            float targetX = CalculateBoxPosition();
            Vector2 targetPos = new Vector2(targetX, startPos.y);

            float elapsedTime = 0f;
            float animationDuration = 0.5f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / animationDuration;
                float curveValue = boxMoveCurve.Evaluate(progress);

                boxImage.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveValue);
                yield return null;
            }

            boxImage.anchoredPosition = targetPos;
        }

        UpdateVisualElements();
    }

    private float CalculateBoxPosition()
    {
        // Calculate box position based on game area width and current position
        float stepSize = gameAreaWidth / (finishLineDistance * 2);
        return boxPosition * stepSize;
    }

    private void UpdateVisualElements()
    {
        if (boxImage != null)
        {
            float targetX = CalculateBoxPosition();
            boxImage.anchoredPosition = new Vector2(targetX, boxImage.anchoredPosition.y);
        }
    }

    private IEnumerator DelayedVictory()
    {
        yield return new WaitForSeconds(feedbackDisplayTime);
        Victory();
    }

    private IEnumerator DelayedDefeat()
    {
        yield return new WaitForSeconds(feedbackDisplayTime);
        Defeat();
    }

    private void UpdateTimer()
    {
        currentQuestionTimer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"{Mathf.Max(0, Mathf.CeilToInt(currentQuestionTimer))}";
        }

        if (currentQuestionTimer <= 0)
        {
            // Time's up - treat as wrong answer
            OnAnswerSelected(-1); // Invalid index triggers wrong answer
        }
    }

    private void UpdateUI()
    {
        // Update score
        if (scoreText != null)
        {
            scoreText.text = $"{correctAnswers}";
        }

        // Update box position display
        if (boxPositionText != null)
        {
            string positionDescription = boxPosition == 0 ? "Center" :
                                       boxPosition > 0 ? $"Player +{boxPosition}" :
                                       $"Enemy {boxPosition}";
            boxPositionText.text = $"Box Position: {positionDescription}";
        }
    }

    private void EndGame()
    {
        gameEnded = true;
        isAnswering = false;

        // Calculate percentage based on questions answered vs correct answers
        float overallPercentage = totalQuestionsAnswered > 0 ?
            (float)correctAnswers / totalQuestionsAnswered * 100f : 0f;

        // Determine pass/fail based on percentage
        if (overallPercentage >= passPercentage)
        {
            Victory(overallPercentage);
        }
        else
        {
            Defeat(overallPercentage);
        }
    }

    private void Victory()
    {
        // Calculate percentage for immediate victory (reached finish line)
        float overallPercentage = totalQuestionsAnswered > 0 ?
            (float)correctAnswers / totalQuestionsAnswered * 100f : 0f;
        Victory(overallPercentage);
    }

    private void Victory(float overallPercentage)
    {
        gameEnded = true;
        AudioManager.Instance.PlaySFX(passedsound);

        if (victoryModal != null)
        {
            victoryModal.SetActive(true);

            // Update victory modal text
            if (victoryModalText != null)
            {
                victoryModalText.text = $"Congratulations! You passed with {overallPercentage:F0}%!\nFinal Score: {correctAnswers}/{totalQuestionsAnswered}";
            }
        }
        Debug.Log("Player Wins!");
    }

    private void Defeat()
    {
        // Calculate percentage for immediate defeat (reached enemy finish line)
        float overallPercentage = totalQuestionsAnswered > 0 ?
            (float)correctAnswers / totalQuestionsAnswered * 100f : 0f;
        Defeat(overallPercentage);
    }

    private void Defeat(float overallPercentage)
    {
        gameEnded = true;
        AudioManager.Instance.PlaySFX(failed);

        if (gameOverModal != null)
        {
            gameOverModal.SetActive(true);

            // Update game over modal text
            if (gameOverModalText != null)
            {
                gameOverModalText.text = $"You scored {overallPercentage:F0}%. You need {passPercentage:F0}% or higher to pass.\nFinal Score: {correctAnswers}/{totalQuestionsAnswered}";
            }
        }
        Debug.Log("Enemy Wins!");
    }

    private void Draw()
    {
        gameEnded = true;
        if (drawModal != null)
        {
            drawModal.SetActive(true);
        }
        Debug.Log("It's a Draw!");
    }

    private void ResetButtonColor(Image buttonImage)
    {
        SetButtonColor(buttonImage, defaultButtonColor);
    }

    private void SetButtonColor(Image buttonImage, Color color)
    {
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }
    }

    // Public methods for UI buttons
    public void RestartGame()
    {
        InitializeGame();
    }

    public void RetakeGame()
    {
        // If we have a dialogue system, restart with dialogue
        if (dialogueSystem != null)
        {
            StartDialogueBeforeQuiz();
        }
        else
        {
            ResetGameForRetake();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // Reset game specifically for retake functionality
    private void ResetGameForRetake()
    {
        // Reset all game variables to initial state
        currentQuestionIndex = 0;
        boxPosition = 0;
        correctAnswers = 0;
        wrongAnswers = 0;
        totalQuestionsAnswered = 0;
        gameEnded = false;
        isAnswering = false;
        gameStarted = true; // Mark as started since we're skipping dialogue

        if (progressSlider != null)
        {
            progressSlider.value = 0;
        }

        // Hide all modals
        if (victoryModal != null) victoryModal.SetActive(false);
        if (gameOverModal != null) gameOverModal.SetActive(false);
        if (drawModal != null) drawModal.SetActive(false);

        // Show game UI if hidden
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(true);
        }
        else if (questionPanel != null)
        {
            questionPanel.SetActive(true);
        }

        // Reset visual elements
        UpdateVisualElements();

        // Reset button colors to default
        for (int i = 0; i < answerButtonImages.Length; i++)
        {
            if (answerButtonImages[i] != null)
            {
                ResetButtonColor(answerButtonImages[i]);
            }
        }

        // Re-enable answer buttons
        foreach (Button button in answerButtons)
        {
            button.interactable = true;
        }

        // Start the quiz from the first question
        DisplayCurrentQuestion();
        UpdateUI();

        Debug.Log("Game retake initiated - All progress reset!");
    }

    // Sample questions for Free Body Diagrams (you can replace with your own)
    private void CreateSampleQuestions()
    {
        questions.Clear();

        // Sample Question 1
        FreeBodyQuizQuestion q1 = new FreeBodyQuizQuestion();
        q1.questionText = "A box is at rest on a horizontal surface. What forces act on the box?";
        q1.answerOptions = new string[] {
            "Weight and Normal Force",
            "Only Weight",
            "Weight, Normal Force, and Friction",
            "Only Normal Force"
        };
        q1.correctAnswerIndex = 0;
        questions.Add(q1);

        // Sample Question 2
        FreeBodyQuizQuestion q2 = new FreeBodyQuizQuestion();
        q2.questionText = "A book slides across a table with constant velocity. What is the net force?";
        q2.answerOptions = new string[] {
            "Greater than zero in the direction of motion",
            "Zero",
            "Greater than zero opposite to motion",
            "Cannot be determined"
        };
        q2.correctAnswerIndex = 1;
        questions.Add(q2);

        // Add more questions as needed...
    }

    // Call this in inspector or Start() to populate sample questions
    [ContextMenu("Create Sample Questions")]
    public void GenerateSampleQuestions()
    {
        CreateSampleQuestions();
    }
}
