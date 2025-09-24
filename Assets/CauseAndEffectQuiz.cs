using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


[System.Serializable]
public class CauseEffectQuestions
{
    public string cause;
    public string[] effects = new string[3];
    public int correctAnswerIndex;

    public CauseEffectQuestions(string cause, string effect1, string effect2, string effect3, int correctIndex)
    {
        this.cause = cause;
        effects[0] = effect1;
        effects[1] = effect2;
        effects[2] = effect3;
        correctAnswerIndex = correctIndex;
    }
}

public class CauseAndEffectQuiz : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider questionProgressSlider;
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private Text questionText;
    [SerializeField] private Button[] choiceButtons = new Button[3];
    [SerializeField] private Text[] choiceTexts = new Text[3];
    [SerializeField] private Image[] choiceImages = new Image[3];
    [SerializeField] private GameObject victoryModal;
    [SerializeField] private GameObject gameOverModal;
    [SerializeField] private Text victoryText;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text scoreText;

    [Header("Dialogue Integration")]
    [SerializeField] private Dialogues dialogueSystem;
    [SerializeField] private int dialogueId = 0;

    [Header("Game Settings")]
    [SerializeField] private float startingTime = 60f;
    [SerializeField] private float timeBonus = 3f;
    [SerializeField] private int totalQuestionsInGame = 15;
    [SerializeField] private int requiredCorrectAnswers = 10;

    [Header("Feedback Colors")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Timer Animation")]
    [SerializeField] private float pulseSpeed = 2f;   // speed of zoom in/out
    [SerializeField] private float pulseAmount = 0.2f; // how much it scales (20%)
    private Vector3 timerDefaultScale;
    private Color timerDefaultColor; // Add this line

    [Header("Sound Effects")]
    public AudioClip passedsound;
    public AudioClip failed;
    public AudioClip correct;
    public AudioClip wrong;
    public AudioClip TimerTick;

    [Header("Timer Bonus Overlay")]
    [SerializeField] private Image timerOverlayFill; // an image with Fill method = Radial360 or Horizontal
    [SerializeField] private Text bonusTimeText;     // "+3s!" text that pops up
    [SerializeField] private float overlayFadeSpeed = 2f;

    // Game State
    public List<CauseEffectQuestions> questions;
    private int currentQuestionIndex = 0;
    private int totalCorrectAnswers = 0;
    private float currentTime;
    private bool gameActive = false;
    private bool waitingForNextQuestion = false;
    private bool quizStarted = false;

    private Coroutine timerPopRoutine;

    [SerializeField] private DatabaseManager databaseManager;
    [SerializeField] private int quizId = 4;
    [SerializeField] private int questionLimit = 15;

    public LessonLocker lessonHandler;
    public CategoryLocker categoryHandler;
    public int userID;
    public int lessonToUnlock;
    public int categoryToUnlock;
    public int rewardItemID;

    private Coroutine timerSoundRoutine;

    private void Start()
    {
        LoadQuestionsFromDatabase();
        StartDialogueSequence();

        if (timerText != null)
        {
            timerDefaultScale = timerText.transform.localScale;
            timerDefaultColor = timerText.color; // Store original color
        }
    }

    private void OnEnable()
    {
        // Reset and start with dialogue whenever this GameObject becomes active
        if (questions != null && questions.Count > 0)
        {
            ResetQuiz();
            LoadQuestionsFromDatabase();
            StartDialogueSequence();
        }
    }

    private void Update()
    {
        if (gameActive)
        {
            UpdateTimer();

            // Pulse effect only when time is running low
            if (currentTime <= 30f && timerText != null)
            {
                float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                timerText.transform.localScale = timerDefaultScale * scale;
                timerText.color = Color.red; // make it more urgent
            }
            else if (timerText != null)
            {
                float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                timerText.transform.localScale = timerDefaultScale * scale;
                timerText.color = timerDefaultColor;
            }
        }

        if (!quizStarted && dialogueSystem != null && dialogueSystem.dialogueFinished)
        {
            StartQuiz();
        }
    }

    private void StartDialogueSequence()
    {
        quizStarted = false;

        // Hide quiz panel during dialogue
        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }

        // Start the dialogue if dialogue system is assigned
        if (dialogueSystem != null)
        {
            dialogueSystem.StartDialogue(dialogueId);
        }
        else
        {
            // If no dialogue system, start quiz immediately
            StartQuiz();
        }
    }

    private void ResetQuiz()
    {
        // Reset all quiz state
        currentQuestionIndex = 0;
        totalCorrectAnswers = 0;
        gameActive = false;
        waitingForNextQuestion = false;
        quizStarted = false;

        // Hide modals
        if (victoryModal != null) victoryModal.SetActive(false);
        if (gameOverModal != null) gameOverModal.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);

    }

    private void StartQuiz()
    {
        quizStarted = true;
        currentTime = startingTime;
        currentQuestionIndex = 0;
        totalCorrectAnswers = 0;
        gameActive = true;
        waitingForNextQuestion = false;

        // Setup UI
        if (quizPanel != null) quizPanel.SetActive(true);
        if (victoryModal != null) victoryModal.SetActive(false);
        if (gameOverModal != null) gameOverModal.SetActive(false);

        // Setup button listeners
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null)
            {
                int index = i; // Capture for closure
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            }
        }

        DisplayCurrentQuestion();
        UpdateProgressSlider();

        if (timerSoundRoutine != null) StopCoroutine(timerSoundRoutine);
        timerSoundRoutine = StartCoroutine(PlayTimerTick());
    }

    private IEnumerator PlayTimerTick()
    {
        while (gameActive && currentTime > 0)
        {
            AudioManager.Instance.PlaySFX(TimerTick); // <-- add your ticking AudioClip
            yield return new WaitForSeconds(1f); // tick every second
        }
    }

    private void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Count)
        {
            EndQuiz(true);
            return;
        }

        var question = questions[currentQuestionIndex];
        if (questionText != null)
        {
            questionText.text = $"Cause: {question.cause}";
        }

        // Reset button colors
        ResetButtonColors();

        // Enable buttons
        SetButtonsInteractable(true);

        // Display effects
        for (int i = 0; i < choiceButtons.Length && i < choiceTexts.Length; i++)
        {
            if (choiceTexts[i] != null)
            {
                choiceTexts[i].text = question.effects[i];
            }
        }

        waitingForNextQuestion = false;
    }

    private void OnAnswerSelected(int selectedIndex)
    {
        if (!gameActive || waitingForNextQuestion) return;

        waitingForNextQuestion = true;
        SetButtonsInteractable(false);

        var question = questions[currentQuestionIndex];
        bool isCorrect = selectedIndex == question.correctAnswerIndex;

        // Visual feedback
        if (isCorrect)
        {
            if (choiceImages[selectedIndex] != null)
                choiceImages[selectedIndex].color = correctColor;

            totalCorrectAnswers++;
            currentTime += timeBonus; // Add time bonus
            scoreText.text = $"{totalCorrectAnswers}";

            // 🔹 Trigger timer pop + overlay effect
            if (timerPopRoutine != null) StopCoroutine(timerPopRoutine);
            timerPopRoutine = StartCoroutine(TimerPopEffect());

            if (timerOverlayFill != null && bonusTimeText != null)
                StartCoroutine(ShowBonusTimeOverlay());

            AudioManager.Instance.PlaySFX(correct);
        }

        else
        {
            if (choiceImages[selectedIndex] != null)
                choiceImages[selectedIndex].color = incorrectColor;
            if (choiceImages[question.correctAnswerIndex] != null)
                choiceImages[question.correctAnswerIndex].color = correctColor;
            AudioManager.Instance.PlaySFX(wrong);
        }

        // Wait before moving to next question
        StartCoroutine(MoveToNextQuestion());
    }
    private IEnumerator ShowBonusTimeOverlay()
    {
        // Reset text state
        bonusTimeText.text = $"+{timeBonus:F0}s on the timer!";
        bonusTimeText.gameObject.SetActive(true);
        bonusTimeText.color = new Color(bonusTimeText.color.r, bonusTimeText.color.g, bonusTimeText.color.b, 1f);
        bonusTimeText.transform.localScale = Vector3.one * 0.5f;

        Vector3 startPos = bonusTimeText.transform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, 50f, 0); // float upward 50 units

        // Animate scale + upward move + fade
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Scale pop (ease out)
            float scale = Mathf.Lerp(0.5f, 1.2f, Mathf.Sin(progress * Mathf.PI));
            bonusTimeText.transform.localScale = Vector3.one * scale;

            // Move upward
            bonusTimeText.transform.localPosition = Vector3.Lerp(startPos, endPos, progress);

            // Fade out
            var col = bonusTimeText.color;
            col.a = Mathf.Lerp(1f, 0f, progress);
            bonusTimeText.color = col;

            yield return null;
        }

        bonusTimeText.gameObject.SetActive(false);
    }

    private IEnumerator MoveToNextQuestion()
    {
        yield return new WaitForSeconds(2f); // Show feedback for 2 seconds

        currentQuestionIndex++;
        UpdateProgressSlider();

        if (currentQuestionIndex >= questions.Count)
        {
            EndQuiz(true); // All questions completed
        }
        else
        {
            DisplayCurrentQuestion();
        }
    }

    private IEnumerator TimerPopEffect()
    {
        Vector3 bigScale = timerDefaultScale * 1.4f; // how big it pops
        float duration = 0.2f; // time for expand/contract

        // Expand
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            timerText.transform.localScale = Vector3.Lerp(timerDefaultScale, bigScale, progress);
            yield return null;
        }

        // Contract back
        t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            timerText.transform.localScale = Vector3.Lerp(bigScale, timerDefaultScale, progress);
            yield return null;
        }

        timerText.transform.localScale = timerDefaultScale;
        timerPopRoutine = null;
    }

    private void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"{Mathf.Ceil(currentTime):F0}";
        }

        // 🔹 Update the overlay fill (0 = empty, 1 = full)
        if (timerOverlayFill != null)
        {
            SetTimerFill(currentTime / startingTime);
        }

        if (currentTime <= 0)
        {
            EndQuiz(false); // Time's up
        }
    }

    private Coroutine smoothFillRoutine;

    private void SetTimerFill(float targetValue)
    {
        if (smoothFillRoutine != null) StopCoroutine(smoothFillRoutine);
        smoothFillRoutine = StartCoroutine(SmoothFill(targetValue));
    }

    private IEnumerator SmoothFill(float target)
    {
        float start = timerOverlayFill.fillAmount;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            timerOverlayFill.fillAmount = Mathf.Lerp(start, target, progress);
            yield return null;
        }

        timerOverlayFill.fillAmount = target;
    }


    private void UpdateProgressSlider()
    {
        if (questionProgressSlider != null)
        {
            questionProgressSlider.value = (float)currentQuestionIndex / totalQuestionsInGame;
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var button in choiceButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void ResetButtonColors()
    {
        foreach (var image in choiceImages)
        {
            if (image != null)
                image.color = defaultColor;
        }
    }

    private void EndQuiz(bool completedAllQuestions)
    {
        gameActive = false;
        if (quizPanel != null) quizPanel.SetActive(false);

        float overallPercentage = (float)totalCorrectAnswers / totalQuestionsInGame * 100f;
        bool passed = totalCorrectAnswers >= requiredCorrectAnswers;

        if (passed)
        {
            databaseManager.AddUserItem(userID, rewardItemID);
            databaseManager.MarkLessonAsCompleted(userID, quizId);
            databaseManager.UnlockCategoryForUser(userID, 2);
            categoryHandler.RefreshCategoryLocks();
            databaseManager.CheckAndUnlockAllLessons(userID);
            lessonHandler.RefreshLessonLocks();
            databaseManager.AddCoin(userID, 100);
            if (victoryModal != null) victoryModal.SetActive(true);
            if (victoryText != null)
                victoryText.text =
                    $"You harnessed the power of unbalanced forces to set things in motion!\n" +
                    $"Final Score: {totalCorrectAnswers}/{totalQuestionsInGame}";
            AudioManager.Instance.PlaySFX(passedsound);
        }
        else
        {
            databaseManager.AddCoin(userID, 50);
            if (gameOverModal != null) gameOverModal.SetActive(true);
            if (gameOverText != null)
                gameOverText.text =
                    $"The forces didn’t tip in your favor—motion was lost…\n" +
                    $"Final Score: {totalCorrectAnswers}/{totalQuestionsInGame}";

            AudioManager.Instance.PlaySFX(failed);
        }

        databaseManager.SaveQuizAndScore(userID, quizId, totalCorrectAnswers);
        databaseManager.CheckAndUnlockBadges(userID);
    }

    // Public methods for UI buttons
    public void RestartQuiz()
    {
        ResetQuiz();
        LoadQuestionsFromDatabase();
        StartDialogueSequence();
    }

    public void QuitQuiz()
    {
        // Implement quit functionality (return to main menu, etc.)
        Debug.Log("Quit Quiz");
    }

    private void LoadQuestionsFromDatabase()
    {
        if (databaseManager == null)
        {
            Debug.LogError("DatabaseManager not assigned!");
            return;
        }

        // Fetch questions from DB
        List<MultipleChoice.MultipleChoiceQuestions> dbQuestions = databaseManager.GetRandomUnusedQuestions(quizId: quizId, limit: questionLimit);

        // Clear current question list
        questions = new List<CauseEffectQuestions>();

        // Convert to CauseEffectQuestions format
        foreach (var dbQ in dbQuestions)
        {
            // Assuming your DB options are structured as Cause + 3 effects
            if (dbQ.options.Length < 3)
            {
                Debug.LogWarning($"Question '{dbQ.question}' has less than 3 options. Skipping.");
                continue;
            }

            CauseEffectQuestions newQ = new CauseEffectQuestions(
                dbQ.question,
                dbQ.options[0],
                dbQ.options[1],
                dbQ.options[2],
                dbQ.correctIndex
            );

            questions.Add(newQ);
        }

        Debug.Log($"Loaded {questions.Count} Cause & Effect questions from DB.");
    }

}
