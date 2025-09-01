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

    [Header("Other Components")]
    public Dialogues dialogues;
    public Image insulatorImage;
    public Sprite[] insulators;

    [Header("Database Manager")]
    public DatabaseManager dbManager;

    [Header("Particle Spawner")]
    public ParticleSpawner particleSpawner; // Drag your ParticleSpawner here

    private int currentQuestionIndex = 0;
    private int currentScore = 0;
    private int totalQuestions = 10;
    private List<Question> dbQuestions;
    private bool quizCompleted = false;

    void Start()
    {
        InitializeGame();
    }

    void OnEnable()
    {
        // Reset when game object is re-enabled
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

        // Hide timer and score initially
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        insulatorImage.color = new Color(1, 1, 1, 0);

        StartCoroutine(GameFlow());
    }

    private void ResetGame()
    {
        // Reset all variables
        currentQuestionIndex = 0;
        currentScore = 0;
        quizCompleted = false;

        // Hide all panels
        HousePanel.SetActive(false);
        ButtonsPanel.SetActive(false);
        quizPanel.SetActive(false);
        passedModal.SetActive(false);
        failedModal.SetActive(false);

        // Hide timer and score
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        // Reset insulator image
        insulatorImage.color = new Color(1, 1, 1, 0);

        // Restart from simulation part (skip first dialogue)
        StartCoroutine(RestartFromSimulation());
    }

    private IEnumerator GameFlow()
    {
        dialogues.StartDialogue(0);
        yield return new WaitUntil(() => dialogues.dialogueFinished);

        HousePanel.SetActive(true);
        ButtonsPanel.SetActive(true);

        // Show timer during simulation
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "Timer: 30s";
        }

        yield return StartCoroutine(SimulationTimer());

        // Hide timer after simulation
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

            // Update timer display
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

        // Show score display
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            UpdateScoreDisplay();
        }

        // Initialize quiz progress slider
        if (quizProgressSlider != null)
        {
            quizProgressSlider.maxValue = totalQuestions;
            quizProgressSlider.value = 0;
        }

        // Load questions
        dbQuestions = dbManager.LoadRandomQuestions(13, "Multiple Choice", totalQuestions);

        // Reset quiz variables
        currentQuestionIndex = 0;
        currentScore = 0;
        quizCompleted = false;

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

        // Reset button colors and states
        foreach (Button btn in optionButtons)
        {
            btn.image.color = Color.white;
            btn.interactable = true;
        }

        // Set up option buttons
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i;
            optionButtonTexts[i].text = q.choices[i];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }

        // Update progress slider
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

        // Update score if correct
        if (isCorrect)
        {
            currentScore++;
            UpdateScoreDisplay();
        }

        // Color the buttons based on correctness
        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (isCorrect)
            {
                // ✅ Correct Answer Selected
                if (i == selectedIndex)
                    optionButtonImages[i].color = Color.green; // Chosen correct answer
                else
                    optionButtonImages[i].color = Color.red;   // All others wrong
            }
            else
            {
                // ❌ Wrong Answer Selected
                if (i == selectedIndex)
                    optionButtonImages[i].color = Color.red;   // Chosen wrong answer
                else if (i == correctIndex)
                    optionButtonImages[i].color = Color.green; // Show the correct one
                else
                    optionButtonImages[i].color = Color.white; // Neutral others
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

    private void EndQuiz()
    {
        quizPanel.SetActive(false);
        quizCompleted = true;

        // Update final progress
        if (quizProgressSlider != null)
        {
            quizProgressSlider.value = totalQuestions;
        }

        // Show appropriate modal based on score
        bool passed = currentScore >= 7;

        if (passed)
        {
            passedModal.SetActive(true);
        }
        else
        {
            failedModal.SetActive(true);
        }

        // Set up retake button if it exists
        if (retakeButton != null)
        {
            retakeButton.onClick.RemoveAllListeners();
            retakeButton.onClick.AddListener(RetakeQuiz);
        }
    }

    public void RetakeQuiz()
    {
        // Hide modals
        passedModal.SetActive(false);
        failedModal.SetActive(false);

        // Reset all variables
        currentQuestionIndex = 0;
        currentScore = 0;
        quizCompleted = false;

        // Hide score display
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        // Reset insulator image
        insulatorImage.color = new Color(1, 1, 1, 0);

        // Restart from simulation part
        StartCoroutine(RestartFromSimulation());
    }

    private IEnumerator RestartFromSimulation()
    {
        // Restart particle spawning
        if (particleSpawner != null)
        {
            particleSpawner.StartSpawningProcess();
        }

        // Show house and buttons panels for simulation
        HousePanel.SetActive(true);
        ButtonsPanel.SetActive(true);

        // Show timer during simulation
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "30";
        }

        yield return StartCoroutine(SimulationTimer());

        // Hide timer after simulation
        if (timerText != null) timerText.gameObject.SetActive(false);

        dialogues.StartDialogue(1);
        yield return new WaitUntil(() => dialogues.dialogueFinished);

        StartQuiz();
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
