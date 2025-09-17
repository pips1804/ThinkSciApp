using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string questionText;

    public string[] options = new string[4];   // Stores A, B, C, D
    [Tooltip("0 = A, 1 = B, 2 = C, 3 = D")]
    public int correctIndex = 0;
}

public class BatteryQuiz : MonoBehaviour
{
    [Header("Questions (Editable in Inspector)")]
    [Tooltip("Add between 10 and 15 questions (but any count works).")]
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [Header("UI Elements")]
    public Text questionText;
    public Text timerText;
    public Text batteryPercentText;
    public Image batteryImage; // must be Image type Filled (Fill Method/Amount used)
    public Slider progressSlider;

    [Header("Answer Buttons (Button and separate Image)")]
    public Button buttonA;
    public Image buttonAImage; // you said Image is on different GameObject
    public Text buttonAText;

    public Button buttonB;
    public Image buttonBImage;
    public Text buttonBText;

    public Button buttonC;
    public Image buttonCImage;
    public Text buttonCText;

    public Button buttonD;
    public Image buttonDImage;
    public Text buttonDText;

    [Header("Feedback & Panels")]
    public GameObject passPanel;
    public GameObject failPanel;
    public Text passText; // used by both panels
    public Text failText; // used by both panels

    [Header("Quiz Settings")]
    public float timePerQuestion = 15f;
    [Range(0f, 1f)]
    public float passThreshold = 0.7f; // 70% default
    public float feedbackDelay = 1.2f; // delay before next question
    private bool quizActive = false;

    [Header("Battery Fill Animation")]
    public float batteryFillAnimDuration = 0.5f;

    // internal
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private float timer = 0f;
    private bool isWaitingForNext = false;
    private Image[] answerImages;
    private Button[] answerButtons;
    private Text[] answerTexts;
    private Color defaultButtonColor = Color.white;
    private Color correctColor = new Color(0.2f, 0.8f, 0.2f); // green-ish
    private Color wrongColor = new Color(0.9f, 0.3f, 0.3f);   // red-ish
    private float batteryTargetFill = 0f;
    private Coroutine batteryAnimCoroutine = null;

    [Header("Dialogue Integration")]
    public Dialogues dialogueSystem;   // Reference to your Dialogues script
    public GameObject quizPanel;       // Parent panel of your quiz UI (set inactive until dialogue ends)

    [Header("Database")]
    public DatabaseManager dbManager;   // Drag DatabaseManager into Inspector
    public int quizId = 14;              // Quiz ID in DB
    public int questionLimit = 15;

    [Header("Sound Effects")]
    public AudioClip passed;
    public AudioClip failed;
    public AudioClip correct;
    public AudioClip wrong;

    public LessonLocker lessonHandler;
    public int userID;
    private void Awake()
    {
        // gather arrays for easier handling
        answerImages = new Image[] { buttonAImage, buttonBImage, buttonCImage, buttonDImage };
        answerButtons = new Button[] { buttonA, buttonB, buttonC, buttonD };
        answerTexts = new Text[] { buttonAText, buttonBText, buttonCText, buttonDText };

        // store default color if any image assigned
        if (buttonAImage != null) defaultButtonColor = buttonAImage.color;

        // wire button listeners safely (capture index)
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int idx = i;
            if (answerButtons[idx] != null)
            {
                answerButtons[idx].onClick.RemoveAllListeners();
                answerButtons[idx].onClick.AddListener(() => OnAnswerSelected(idx));
            }
        }
    }

    // You asked to restart the quiz whenever you reopen the game object.
    // OnEnable is called when the GameObject becomes active/in the scene.
    private void OnEnable()
    {
        ResetQuiz();
        HideEndPanels();

        // Start with dialogue if assigned
        if (dialogueSystem != null)
        {
            quizPanel.SetActive(false); // hide quiz UI
            dialogueSystem.StartDialogue(0); // Start first dialogue (id = 0 for example)
            StartCoroutine(WaitForDialogueThenStartQuiz());
        }
        else
        {
            StartQuiz(); // fallback if no dialogue system assigned
        }
    }

    private IEnumerator WaitForDialogueThenStartQuiz()
    {
        // Wait until dialogue finishes
        yield return new WaitUntil(() => dialogueSystem.dialogueFinished);

        // Show quiz UI now
        quizPanel.SetActive(true);

        StartQuiz();
        quizActive = true; // ✅ allow timer to run now
    }

    private void Start()
    {
        LoadQuestionsFromDB();
        // Also ensure panels start hidden
        if (passPanel) passPanel.SetActive(false);
        if (failPanel) failPanel.SetActive(false);

        // Progress slider setup
        if (progressSlider)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = Mathf.Max(1, questions.Count);
            progressSlider.value = 0;
        }

        // Battery initial value
        if (batteryImage != null)
        {
            batteryImage.type = Image.Type.Filled; // ensure type
            batteryImage.fillAmount = 0f;
            batteryTargetFill = 0f;
        }
    }

    private void Update()
    {
        if (!quizActive) return; // ⛔ don't run until dialogue is done
        if (isWaitingForNext) return;

        // Timer handling
        timer -= Time.deltaTime;
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(timer).ToString("0");
        }

        if (timer <= 0f && !isWaitingForNext)
        {
            // time's up — treat as wrong and reveal correct
            StartCoroutine(HandleTimeUp());
        }
    }

    private void StartQuiz()
    {
        // Safety: if no questions, show nothing
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("No questions in the quiz! Add questions in the inspector.");
            return;
        }

        currentQuestionIndex = 0;
        correctAnswers = 0;
        progressSlider.value = 0;

        // reset battery visuals
        SetBatteryFillInstant(0f);
        UpdateBatteryPercentText();

        HideEndPanels();

        ShowQuestion(currentQuestionIndex);
    }

    private void ResetQuiz()
    {
        // Reset state variables and UI to start state
        currentQuestionIndex = 0;
        correctAnswers = 0;
        timer = timePerQuestion;
        isWaitingForNext = false;

        if (batteryAnimCoroutine != null) StopCoroutine(batteryAnimCoroutine);
        SetBatteryFillInstant(0f);
        UpdateBatteryPercentText();

        ResetAllButtonColors();
        EnableAllButtons(true);

        if (progressSlider) progressSlider.value = 0;
    }

    private void ShowQuestion(int qIndex)
    {
        if (qIndex < 0 || qIndex >= questions.Count)
        {
            EndQuiz();
            return;
        }

        QuizQuestion q = questions[qIndex];

        // populate UI
        if (questionText != null) questionText.text = q.questionText;
        if (buttonAText != null) buttonAText.text = q.options.Length > 0 ? q.options[0] : "";
        if (buttonBText != null) buttonBText.text = q.options.Length > 1 ? q.options[1] : "";
        if (buttonCText != null) buttonCText.text = q.options.Length > 2 ? q.options[2] : "";
        if (buttonDText != null) buttonDText.text = q.options.Length > 3 ? q.options[3] : "";

        // reset button colors and enable them
        ResetAllButtonColors();
        EnableAllButtons(true);

        // set timer
        timer = timePerQuestion;
        if (timerText != null) timerText.text = Mathf.CeilToInt(timer).ToString("0");
    }


    private IEnumerator HandleTimeUp()
    {
        isWaitingForNext = true;
        EnableAllButtons(false);

        // highlight correct answer as green, selected nothing as red since no selection
        int correctIdx = questions[currentQuestionIndex].correctIndex;
        if (IsImageAssigned(correctIdx))
        {
            answerImages[correctIdx].color = correctColor;
        }

        yield return new WaitForSeconds(feedbackDelay);
        ProceedToNextQuestion();
    }

    private void OnAnswerSelected(int index)
    {
        if (isWaitingForNext) return;

        isWaitingForNext = true;
        EnableAllButtons(false);

        QuizQuestion q = questions[currentQuestionIndex];
        int correctIdx = q.correctIndex;

        // If correct
        if (index == correctIdx)
        {
            correctAnswers++;
            // animate battery increase
            AudioManager.Instance.PlaySFX(correct);
            float increment = 1f / (float)questions.Count;
            batteryTargetFill = Mathf.Clamp01(batteryImage.fillAmount + increment);
            if (batteryAnimCoroutine != null) StopCoroutine(batteryAnimCoroutine);
            batteryAnimCoroutine = StartCoroutine(AnimateBatteryFill(batteryImage.fillAmount, batteryTargetFill, batteryFillAnimDuration));

            // color selected correct green
            if (IsImageAssigned(index)) answerImages[index].color = correctColor;
        }
        else
        {
            AudioManager.Instance.PlaySFX(wrong);
            // selected wrong -> red
            if (IsImageAssigned(index)) answerImages[index].color = wrongColor;
            // correct one -> green
            if (IsImageAssigned(correctIdx)) answerImages[correctIdx].color = correctColor;
        }

        UpdateBatteryPercentText();
        UpdateProgressSlider(); // show we've completed one more

        // wait a short time then go to next question or end
        StartCoroutine(WaitThenProceed(feedbackDelay));
    }

    private IEnumerator WaitThenProceed(float wait)
    {
        yield return new WaitForSeconds(wait);
        ProceedToNextQuestion();
    }

    private void ProceedToNextQuestion()
    {
        isWaitingForNext = false;
        currentQuestionIndex++;

        if (currentQuestionIndex >= questions.Count)
        {
            EndQuiz();
        }
        else
        {
            ShowQuestion(currentQuestionIndex);
        }
    }

    private void EndQuiz()
    {
        isWaitingForNext = true;
        EnableAllButtons(false);

        float finalScore = (questions.Count > 0) ? (float)correctAnswers / (float)questions.Count : 0f;
        int percent = Mathf.RoundToInt(finalScore * 100f);

        if (finalScore >= passThreshold)
        {
            dbManager.CheckAndUnlockAllLessons(userID);
            dbManager.MarkLessonAsCompleted(userID, quizId);
            lessonHandler.RefreshLessonLocks();
            dbManager.AddCoin(userID, 100);
            // Pass
            if (passPanel != null)
            {
                AudioManager.Instance.PlaySFX(passed);
                passPanel.SetActive(true);
                if (passText) passText.text = $"Battery is charged!\n You passed with the score of {percent}%!";
            }
        }
        else
        {
            dbManager.AddCoin(userID, 50);
            // Fail
            if (failPanel != null)
            {
                AudioManager.Instance.PlaySFX(failed);
                failPanel.SetActive(true);
                if (failText) failText.text = $"Battery is drained!\n You failed with the score of {percent}%.\nTry again!";
            }
        }
        dbManager.SaveQuizAndScore(userID, quizId, correctAnswers);
        dbManager.CheckAndUnlockBadges(userID);
        // ensure progress slider is full (all questions completed)
        if (progressSlider) progressSlider.value = progressSlider.maxValue;
    }

    public void OnRetakeQuizButton()
    {
        // Called by Retake button on failPanel (hook this up in inspector)
        ResetQuiz();
        StartQuiz();
    }

    #region Helpers

    private void EnableAllButtons(bool enable)
    {
        foreach (Button b in answerButtons)
        {
            if (b != null) b.interactable = enable;
        }
    }

    private void ResetAllButtonColors()
    {
        for (int i = 0; i < answerImages.Length; i++)
        {
            if (answerImages[i] != null)
                answerImages[i].color = defaultButtonColor;
        }
    }

    private bool IsImageAssigned(int idx)
    {
        return answerImages != null && idx >= 0 && idx < answerImages.Length && answerImages[idx] != null;
    }

    private void UpdateBatteryPercentText()
    {
        if (batteryPercentText != null && batteryImage != null)
        {
            batteryPercentText.text = $"{Mathf.RoundToInt(batteryImage.fillAmount * 100f)}%";
        }
    }

    private void UpdateProgressSlider()
    {
        if (progressSlider != null)
        {
            progressSlider.value = Mathf.Min(progressSlider.maxValue, currentQuestionIndex + 1);
        }
    }

    private void HideEndPanels()
    {
        if (passPanel) passPanel.SetActive(false);
        if (failPanel) failPanel.SetActive(false);
    }

    private void SetBatteryFillInstant(float fill)
    {
        if (batteryImage != null)
        {
            batteryImage.fillAmount = Mathf.Clamp01(fill);
            batteryTargetFill = batteryImage.fillAmount;
        }
    }

    private IEnumerator AnimateBatteryFill(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            batteryImage.fillAmount = Mathf.Lerp(from, to, t);
            UpdateBatteryPercentText();
            yield return null;
        }
        batteryImage.fillAmount = to;
        UpdateBatteryPercentText();
    }

    private void LoadQuestionsFromDB()
    {
        if (dbManager == null)
        {
            Debug.LogError("⚠ No DatabaseManager assigned in BatteryQuiz!");
            return;
        }

        // Fetch from DB
        var dbQuestions = dbManager.GetRandomUnusedQuestions(quizId: quizId, limit: questionLimit);
        questions.Clear();

        foreach (var dbQ in dbQuestions)
        {
            QuizQuestion q = new QuizQuestion();
            q.questionText = dbQ.question;
            q.options = dbQ.options;
            q.correctIndex = dbQ.correctIndex;

            questions.Add(q);
        }

        Debug.Log($"✅ Loaded {questions.Count} questions from database (Quiz {quizId})");
    }

    #endregion
}
