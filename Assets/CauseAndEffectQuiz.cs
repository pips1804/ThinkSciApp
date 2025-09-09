using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Sound Effects")]
    public AudioClip passedsound;
    public AudioClip failed;
    public AudioClip correct;
    public AudioClip wrong;

    // Game State
    private List<CauseEffectQuestions> questions;
    private int currentQuestionIndex = 0;
    private int totalCorrectAnswers = 0;
    private float currentTime;
    private bool gameActive = false;
    private bool waitingForNextQuestion = false;
    private bool quizStarted = false;

    private Coroutine timerPopRoutine;

    private void Start()
    {
        InitializeQuestions();
        StartDialogueSequence();

        if (timerText != null)
        {
            timerDefaultScale = timerText.transform.localScale;
        }
    }

    private void OnEnable()
    {
        // Reset and start with dialogue whenever this GameObject becomes active
        if (questions != null && questions.Count > 0)
        {
            ResetQuiz();
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

    private void InitializeQuestions()
    {
        // Only initialize questions if they haven't been initialized yet
        if (questions != null && questions.Count > 0) return;

        questions = new List<CauseEffectQuestions>
        {
            new CauseEffectQuestions("A soccer ball is kicked harder than before.",
                "The ball slows down", "The ball speeds up more", "The ball stops immediately", 1),

            new CauseEffectQuestions("A heavy box is pushed with the same force as a light box.",
                "Both boxes move at the same speed", "The heavy box moves slower", "The heavy box moves faster", 1),

            new CauseEffectQuestions("A car applies its brakes while driving.",
                "The car speeds up", "The car slows down", "The car maintains speed", 1),

            new CauseEffectQuestions("A ball is thrown upward against gravity.",
                "The ball accelerates upward continuously", "The ball slows down as it rises", "The ball maintains constant speed", 1),

            new CauseEffectQuestions("Two people push a cart in opposite directions with equal force.",
                "The cart moves forward", "The cart moves backward", "The cart remains stationary", 2),

            new CauseEffectQuestions("A skateboarder pushes off the ground with their foot.",
                "The skateboard slows down", "The skateboard accelerates forward", "The skateboard stops", 1),

            new CauseEffectQuestions("A book is placed on a frictionless surface and given a push.",
                "The book gradually slows down", "The book continues moving at constant speed", "The book immediately stops", 1),

            new CauseEffectQuestions("A parachutist opens their parachute while falling.",
                "The fall speed increases", "The fall speed decreases", "The fall speed remains the same", 1),

            new CauseEffectQuestions("A bowling ball and tennis ball are dropped from the same height (ignoring air resistance).",
                "The bowling ball hits the ground first", "The tennis ball hits the ground first", "Both hit the ground at the same time", 2),

            new CauseEffectQuestions("A rocket fires its engines in space.",
                "The rocket slows down", "The rocket accelerates", "The rocket maintains constant speed", 1),

            new CauseEffectQuestions("A hockey puck is hit with a stick on ice with minimal friction.",
                "The puck stops immediately", "The puck moves at nearly constant speed", "The puck accelerates continuously", 1),

            new CauseEffectQuestions("A person jumps off a diving board.",
                "They fall at constant speed", "They accelerate downward", "They slow down while falling", 1),

            new CauseEffectQuestions("A train applies emergency brakes.",
                "The train speeds up", "The train slows down", "The train maintains speed", 1),

            new CauseEffectQuestions("An object is pushed up a rough inclined plane.",
                "It moves up at constant speed", "It slows down due to friction and gravity", "It speeds up", 1),

            new CauseEffectQuestions("A satellite orbits Earth with no external forces acting on it.",
                "It spirals inward", "It maintains its orbital path", "It flies off into space", 1)
        };

        // Shuffle questions for variety
        ShuffleQuestions();

        // Trim to required number of questions
        if (questions.Count > totalQuestionsInGame)
        {
            questions = questions.GetRange(0, totalQuestionsInGame);
        }
    }

    private void ShuffleQuestions()
    {
        for (int i = 0; i < questions.Count; i++)
        {
            var temp = questions[i];
            int randomIndex = Random.Range(i, questions.Count);
            questions[i] = questions[randomIndex];
            questions[randomIndex] = temp;
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

        // Reshuffle questions
        ShuffleQuestions();
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

            // 🔹 Trigger timer pop animation
            if (timerPopRoutine != null) StopCoroutine(timerPopRoutine);
            timerPopRoutine = StartCoroutine(TimerPopEffect());
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

        if (currentTime <= 0)
        {
            EndQuiz(false); // Time's up
        }
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
            if (victoryModal != null) victoryModal.SetActive(true);
            if (victoryText != null)
                victoryText.text = $"Congratulations! You passed with {overallPercentage:F0}%!\nFinal Score: {totalCorrectAnswers}/{totalQuestionsInGame}";
            AudioManager.Instance.PlaySFX(passedsound);
        }
        else
        {
            if (gameOverModal != null) gameOverModal.SetActive(true);
            if (gameOverText != null)
                gameOverText.text = $"You scored {overallPercentage:F0}%. You need 70% or higher to pass.\nFinal Score: {totalCorrectAnswers}/{totalQuestionsInGame}";
            AudioManager.Instance.PlaySFX(failed);
        }
    }

    // Public methods for UI buttons
    public void RestartQuiz()
    {
        ResetQuiz();
        StartDialogueSequence();
    }

    public void QuitQuiz()
    {
        // Implement quit functionality (return to main menu, etc.)
        Debug.Log("Quit Quiz");
    }
}
