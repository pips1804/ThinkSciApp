using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HeatEscape : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject HousePanel;
    public GameObject ButtonsPanel;
    public GameObject quizPanel;
    public GameObject passedModal;
    public GameObject failedModal;

    [Header("Quiz UI")]
    public Text questionText;
    public List<Button> optionButtons;
    public List<Text> optionButtonTexts;
    public List<Image> optionButtonImages;
    public Slider quizProgressSlider;

    [Header("Timer and Score")]
    public Text timerText;
    public Text scoreText;
    public Button retakeButton;

    [Header("Performance Display")]
    [Tooltip("Text component in the passed modal to show performance message")]
    public Text passedPerformanceText;
    [Tooltip("Text component in the failed modal to show performance message")]
    public Text failedPerformanceText;
    [Tooltip("Minimum percentage required to pass")]
    [Range(0f, 100f)]
    public float passingPercentage = 70f;

    [Header("Other Components")]
    public Dialogues dialogues;
    public Image insulatorImage;
    public Sprite[] insulators;

    [Header("Database Manager")]
    public DatabaseManager dbManager;

    [Header("Particle Spawner")]
    public ParticleSpawner particleSpawner;

    private int currentQuestionIndex = 0;
    private int currentScore = 0;
    private int totalQuestions = 10;
    private List<Question> dbQuestions;
    private bool quizCompleted = false;

    // NEW: Performance tracking variables
    private int totalCorrectAnswers = 0;
    private int totalQuestionsAnswered = 0;

    void Start()
    {
        InitializeGame();
    }

    void OnEnable()
    {
        if (quizCompleted)
        {
            ResetGame();
        }
    }

    private void InitializeGame()
    {
        HousePanel.SetActive(false);
        ButtonsPanel.SetActive(false);
        quizPanel.SetActive(false);
        passedModal.SetActive(false);
        failedModal.SetActive(false);

        if (timerText != null) timerText.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        insulatorImage.color = new Color(1, 1, 1, 0);

        // NEW: Reset performance tracking
        totalCorrectAnswers = 0;
        totalQuestionsAnswered = 0;

        StartCoroutine(GameFlow());
    }

    private void ResetGame()
    {
        currentQuestionIndex = 0;
        currentScore = 0;
        quizCompleted = false;

        // NEW: Reset performance tracking
        totalCorrectAnswers = 0;
        totalQuestionsAnswered = 0;

        HousePanel.SetActive(false);
        ButtonsPanel.SetActive(false);
        quizPanel.SetActive(false);
        passedModal.SetActive(false);
        failedModal.SetActive(false);

        if (timerText != null) timerText.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        insulatorImage.color = new Color(1, 1, 1, 0);

        StartCoroutine(RestartFromSimulation());
    }

    private IEnumerator GameFlow()
    {
        dialogues.StartDialogue(0);
        yield return new WaitUntil(() => dialogues.dialogueFinished);

        HousePanel.SetActive(true);
        ButtonsPanel.SetActive(true);

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "Timer: 30s";
        }

        yield return StartCoroutine(SimulationTimer());

        if (timerText != null) timerText.gameObject.SetActive(false);

        dialogues.StartDialogue(1);
        yield return new WaitUntil(() => dialogues.dialogueFinished);

        StartQuiz();
    }

    private IEnumerator SimulationTimer()
    {
        float simulationTime = 30f;
        float elapsed = 0f;

        while (elapsed < simulationTime)
        {
            elapsed += Time.deltaTime;
            float remainingTime = simulationTime - elapsed;

            if (timerText != null)
            {
                timerText.text = $"{Mathf.Ceil(remainingTime)}";
            }

            yield return null;
        }
    }

    public void StartQuiz()
    {
        HousePanel.SetActive(false);
        ButtonsPanel.SetActive(false);
        quizPanel.SetActive(true);

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            UpdateScoreDisplay();
        }

        if (quizProgressSlider != null)
        {
            quizProgressSlider.maxValue = totalQuestions;
            quizProgressSlider.value = 0;
        }

        dbQuestions = dbManager.LoadRandomQuestions(13, "Multiple Choice", totalQuestions);

        currentQuestionIndex = 0;
        currentScore = 0;
        quizCompleted = false;

        // NEW: Reset performance tracking for new quiz
        totalCorrectAnswers = 0;
        totalQuestionsAnswered = 0;

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (currentQuestionIndex >= dbQuestions.Count)
        {
            EndQuiz();
            return;
        }

        var q = dbQuestions[currentQuestionIndex];
        questionText.text = q.questionText;

        foreach (Button btn in optionButtons)
        {
            btn.image.color = Color.white;
            btn.interactable = true;
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i;
            optionButtonTexts[i].text = q.choices[i];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }

        if (quizProgressSlider != null)
        {
            quizProgressSlider.value = currentQuestionIndex;
        }
    }

    private void OnOptionSelected(int selectedIndex)
    {
        StartCoroutine(HandleAnswer(selectedIndex));
    }

    private IEnumerator HandleAnswer(int selectedIndex)
    {
        int correctIndex = dbQuestions[currentQuestionIndex].correctAnswerIndex;
        bool isCorrect = selectedIndex == correctIndex;

        // NEW: Track total questions answered
        totalQuestionsAnswered++;

        if (isCorrect)
        {
            currentScore++;
            // NEW: Track total correct answers
            totalCorrectAnswers++;
            UpdateScoreDisplay();
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (isCorrect)
            {
                if (i == selectedIndex)
                    optionButtonImages[i].color = Color.green;
                else
                    optionButtonImages[i].color = Color.white;
            }
            else
            {
                if (i == selectedIndex)
                    optionButtonImages[i].color = Color.red;
                else if (i == correctIndex)
                    optionButtonImages[i].color = Color.green;
                else
                    optionButtonImages[i].color = Color.white;
            }

            optionButtons[i].interactable = false;
        }

        yield return new WaitForSeconds(1.5f);

        currentQuestionIndex++;

        if (currentQuestionIndex < dbQuestions.Count)
        {
            ShowQuestion();
        }
        else
        {
            EndQuiz();
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{currentScore}";
        }
    }

    // NEW: Generate performance message based on results
    private string GetPerformanceMessage()
    {
        if (totalQuestionsAnswered == 0)
        {
            return "No questions were answered.";
        }

        float overallPercentage = ((float)totalCorrectAnswers / totalQuestionsAnswered) * 100f;

        if (overallPercentage >= passingPercentage)
        {
            return $"Congratulations! You passed with {overallPercentage:F0}%!\nFinal Score: {totalCorrectAnswers}/{totalQuestionsAnswered}";
        }
        else
        {
            return $"You scored {overallPercentage:F0}%. You need {passingPercentage:F0}% or higher to pass.\nFinal Score: {totalCorrectAnswers}/{totalQuestionsAnswered}";
        }
    }

    // NEW: Display performance message on appropriate modal
    private void DisplayPerformanceMessage(bool passed)
    {
        string performanceMessage = GetPerformanceMessage();

        if (passed && passedPerformanceText != null)
        {
            passedPerformanceText.text = performanceMessage;
        }
        else if (!passed && failedPerformanceText != null)
        {
            failedPerformanceText.text = performanceMessage;
        }

        // Fallback: Log to console if no UI elements are assigned
        Debug.Log("Quiz Performance: " + performanceMessage);
    }

    private void EndQuiz()
    {
        quizPanel.SetActive(false);
        quizCompleted = true;

        if (quizProgressSlider != null)
        {
            quizProgressSlider.value = totalQuestions;
        }

        bool passed = currentScore >= 7;

        // NEW: Display performance message before showing modal
        DisplayPerformanceMessage(passed);

        if (passed)
        {
            passedModal.SetActive(true);
        }
        else
        {
            failedModal.SetActive(true);
        }

        if (retakeButton != null)
        {
            retakeButton.onClick.RemoveAllListeners();
            retakeButton.onClick.AddListener(RetakeQuiz);
        }
    }

    public void RetakeQuiz()
    {
        passedModal.SetActive(false);
        failedModal.SetActive(false);

        currentQuestionIndex = 0;
        currentScore = 0;
        quizCompleted = false;

        // NEW: Reset performance tracking for retake
        totalCorrectAnswers = 0;
        totalQuestionsAnswered = 0;

        if (scoreText != null) scoreText.gameObject.SetActive(false);

        insulatorImage.color = new Color(1, 1, 1, 0);

        StartCoroutine(RestartFromSimulation());
    }

    private IEnumerator RestartFromSimulation()
    {
        if (particleSpawner != null)
        {
            particleSpawner.StartSpawningProcess();
        }

        HousePanel.SetActive(true);
        ButtonsPanel.SetActive(true);

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "30";
        }

        yield return StartCoroutine(SimulationTimer());

        if (timerText != null) timerText.gameObject.SetActive(false);

        dialogues.StartDialogue(1);
        yield return new WaitUntil(() => dialogues.dialogueFinished);

        StartQuiz();
    }

    // NEW: Public methods for getting performance statistics
    public void GetCurrentPerformanceStats(out int correct, out int total, out float percentage)
    {
        correct = totalCorrectAnswers;
        total = totalQuestionsAnswered;
        percentage = totalQuestionsAnswered > 0 ? ((float)totalCorrectAnswers / totalQuestionsAnswered) * 100f : 0f;
    }

    public bool IsCurrentlyPassing()
    {
        if (totalQuestionsAnswered == 0) return true;

        float currentPercentage = ((float)totalCorrectAnswers / totalQuestionsAnswered) * 100f;
        return currentPercentage >= passingPercentage;
    }

    public string GetCurrentPerformanceMessage()
    {
        return GetPerformanceMessage();
    }

    #region Material Buttons
    public void SetGlass()
    {
        HeatParticle.SetMaterial("Glass");
        insulatorImage.sprite = insulators[0];
        insulatorImage.color = new Color(1, 1, 1, 1);
    }

    public void SetBrick()
    {
        HeatParticle.SetMaterial("Brick");
        insulatorImage.sprite = insulators[1];
        insulatorImage.color = new Color(1, 1, 1, 1);
    }

    public void SetFoam()
    {
        HeatParticle.SetMaterial("Foam");
        insulatorImage.sprite = insulators[2];
        insulatorImage.color = new Color(1, 1, 1, 1);
    }

    public void SetMetal()
    {
        HeatParticle.SetMaterial("Metal");
        insulatorImage.sprite = insulators[3];
        insulatorImage.color = new Color(1, 1, 1, 1);
    }
    #endregion
}
