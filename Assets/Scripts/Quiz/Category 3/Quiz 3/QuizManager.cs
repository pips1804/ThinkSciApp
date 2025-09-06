using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class QuizManager : MonoBehaviour
{
    [Header("UI References")]
    public Text questionText;
    public Button[] answerButtons;         // drag your 4 buttons here
    public Text[] answerTexts;             // drag 4 Text components (children of buttons)
    public Image[] answerBackgrounds;      // ✅ drag the background Image of each button
    public GameObject quizPanel;

    [Header("Database")]
    public DatabaseManager dbManager;      // drag your DatabaseManager
    public int quizID = 10;                // which Quiz_ID to load
    public string questionType = "Multiple Choice"; // type filter
    public int numberOfQuestions = 10;     // how many to fetch

    [Header("Integration")]
    public ParticleManager particleManager; // Reference to ParticleManager

    [Header("Feedback UI")]
    public GameObject feedbackPanel;       // Optional: panel to show "Correct!" or "Wrong!" messages
    public Text feedbackText;             // Optional: text component for feedback

    private List<Question> dbQuestions;    // from DatabaseManager
    private int currentQuestion = 0;
    private int correctAnswers = 0;        // Track correct answers
    private Color defaultColor;
    private bool questionAnswered = false; // Prevent multiple answers
    private bool timeExpired = false;      // Track if time expired

    [Header("Sound Effects")]
    public AudioClip correct;
    public AudioClip wrong;

    void Start()
    {
        quizPanel.SetActive(false);

        // Save default background color (from first button)
        if (answerBackgrounds != null && answerBackgrounds.Length > 0)
            defaultColor = answerBackgrounds[0].color;

        // Attach button listeners dynamically
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }

        // Hide feedback panel if it exists
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }

    public void StartQuiz()
    {
        // ✅ Load questions from DB
        dbQuestions = dbManager.LoadRandomQuestions(quizID, questionType, numberOfQuestions);

        for (int i = 0; i < dbQuestions.Count; i++)
        {
            Question q = dbQuestions[i];
            string answersText = "";
            for (int j = 0; j < q.choices.Length; j++)
            {
                answersText += $"\n   {j}. {q.choices[j]}";
            }
        }

        quizPanel.SetActive(true);
        currentQuestion = 0;
        correctAnswers = 0;
        ShowQuestion();
    }

    void ShowQuestion()
    {
        if (currentQuestion >= dbQuestions.Count)
        {
            EndQuiz();
            return;
        }

        // Reset question state
        questionAnswered = false;
        timeExpired = false;

        Question q = dbQuestions[currentQuestion];
        questionText.text = $"{q.questionText}";

        // Setup answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < q.choices.Length)
            {
                answerTexts[i].text = q.choices[i];
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].interactable = true;
                answerBackgrounds[i].color = defaultColor; // reset background
            }
            else
            {
                // Hide unused buttons if less than 4 choices
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        // Hide feedback panel
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        // Start question timer in ParticleManager
        if (particleManager != null)
            particleManager.StartQuestionTimer();
    }

    void OnAnswerSelected(int index)
    {
        if (questionAnswered || timeExpired) return; // Prevent multiple answers

        questionAnswered = true;

        // Disable all buttons
        foreach (Button btn in answerButtons)
            btn.interactable = false;

        Question q = dbQuestions[currentQuestion];
        int correctIndex = q.correctAnswerIndex;
        bool isCorrect = index == correctIndex;

        // Update score tracking
        if (isCorrect)
        {
            correctAnswers++;
            Debug.Log("✅ Correct!");
            answerBackgrounds[index].color = Color.green;

            // Show positive feedback
            ShowFeedback("Correct! 🎉", Color.green);
            AudioManager.Instance.PlaySFX(correct);
        }
        else
        {
            Debug.Log("❌ Wrong!");
            answerBackgrounds[index].color = Color.red;
            answerBackgrounds[correctIndex].color = Color.green;

            // Show negative feedback
            ShowFeedback("Wrong! ❌", Color.red);
            AudioManager.Instance.PlaySFX(wrong);
        }

        // Notify ParticleManager about the answer
        if (particleManager != null)
            particleManager.OnQuestionAnswered(isCorrect);

        // Wait then proceed to next question
        StartCoroutine(NextQuestionDelay());
    }

    // Called by ParticleManager when time expires
    public void OnTimeUp()
    {
        if (questionAnswered) return; // Already answered

        timeExpired = true;

        // Disable all buttons
        foreach (Button btn in answerButtons)
            btn.interactable = false;

        // Show correct answer
        Question q = dbQuestions[currentQuestion];
        int correctIndex = q.correctAnswerIndex;
        answerBackgrounds[correctIndex].color = Color.green;

        // Show timeout feedback
        ShowFeedback("Time's up! ⏰", Color.yellow);

        Debug.Log("⏰ Time expired! Auto-marked as wrong.");

        // Notify ParticleManager (marked as incorrect)
        if (particleManager != null)
            particleManager.OnQuestionAnswered(false);

        // Wait then proceed to next question
        StartCoroutine(NextQuestionDelay());
    }

    void ShowFeedback(string message, Color color)
    {
        if (feedbackPanel != null && feedbackText != null)
        {
            feedbackPanel.SetActive(true);
            feedbackText.text = message;
            feedbackText.color = color;
        }
    }

    IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(2f); // Reduced from 3f for better pacing

        // Hide feedback
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        currentQuestion++;

        // Check if more questions remain
        if (currentQuestion < dbQuestions.Count)
        {
            // Notify ParticleManager we're moving to next question
            if (particleManager != null)
                particleManager.OnNextQuestion();

            ShowQuestion();
        }
        else
        {
            ShowQuestion(); // This will call EndQuiz()
        }
    }

    void EndQuiz()
    {
        quizPanel.SetActive(false);

        // Calculate final score
        float scorePercentage = dbQuestions.Count > 0 ? (float)correctAnswers / dbQuestions.Count * 100f : 0f;

        Debug.Log($"✅ Quiz finished! Score: {correctAnswers}/{dbQuestions.Count} ({scorePercentage:F1}%)");

        // Notify ParticleManager that quiz is completed
        if (particleManager != null)
            particleManager.OnQuizCompleted();
    }

    // Public methods for external access (optional)
    public int GetCurrentScore()
    {
        return correctAnswers;
    }

    public int GetTotalQuestions()
    {
        return dbQuestions != null ? dbQuestions.Count : 0;
    }

    public float GetScorePercentage()
    {
        return dbQuestions != null && dbQuestions.Count > 0 ?
               (float)correctAnswers / dbQuestions.Count * 100f : 0f;
    }

    // Method to pause/resume quiz (if needed)
    public void SetQuizActive(bool active)
    {
        foreach (Button btn in answerButtons)
        {
            btn.interactable = active && !questionAnswered;
        }
    }

    public int GetCurrentQuestionIndex()
    {
        return currentQuestion + 1;
    }
}
