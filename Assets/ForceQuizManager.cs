using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;

[System.Serializable]
public class ForceQuizQuestion
{
    public string questionText;
    public string[] choices;
    public int correctAnswerIndex;
}

public class ForceQuizManager : MonoBehaviour
{
    [Header("UI")]
    public Text questionText;
    public Button[] choiceButtons;  // The clickable buttons
    public Image[] choiceImages;    // The background images for each choice
    public Text[] choiceTexts;      // The texts for each choice
    public Slider progressSlider;
    public Text scoreText;
    public Text timerText;

    [Header("Quiz Settings")]
    public int passingScore = 10;
    public float questionTime = 20f;  // 20 sec per question

    [Header("Sound Effects")]
    public AudioClip correct;
    public AudioClip wrong;

    public List<ForceQuizQuestion> questions;
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool hasAnswered = false;
    private float timeRemaining;
    public DatabaseManager dbManager;
    public LessonLocker lessonsHandler;
    public int userID;
    public int rewardItemID;

    private void Start()
    {
        SetupQuestions();

        // Assign click listeners
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            choiceButtons[i].onClick.AddListener(() => SelectAnswer(index));
        }
    }

    private void SetupQuestions()
    {
        TugOfWarSimulation simulation = FindFirstObjectByType<TugOfWarSimulation>();
        var dbQuestions = dbManager.GetRandomUnusedQuestions(quizId: 1, limit: 15);

        // Convert DB questions to ForceQuizQuestion format
        questions = new List<ForceQuizQuestion>();
        foreach (var q in dbQuestions)
        {
            questions.Add(new ForceQuizQuestion
            {
                questionText = q.question,
                choices = q.options,
                correctAnswerIndex = q.correctIndex
            });
        }
    }

    private void Update()
    {
        if (!hasAnswered && currentQuestionIndex < questions.Count)
        {
            timeRemaining -= Time.deltaTime;
            timerText.text = "" + Mathf.Ceil(timeRemaining).ToString();

            if (timeRemaining <= 0f)
            {
                hasAnswered = true;
                AutoWrongAnswer();
            }
        }
    }

    public void StartQuiz()
    {
        currentQuestionIndex = 0;
        score = 0;
        progressSlider.value = 0;
        DisplayCurrentQuestion();
    }

    private void DisplayCurrentQuestion()
    {
        hasAnswered = false;

        if (currentQuestionIndex >= questions.Count)
        {
            EndQuiz();
            return;
        }

        ForceQuizQuestion question = questions[currentQuestionIndex];
        questionText.text = question.questionText;

        // Reset and set buttons
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < question.choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceTexts[i].text = question.choices[i];
                choiceImages[i].color = Color.white;
                choiceButtons[i].interactable = true;
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        // Reset timer
        timeRemaining = questionTime;
        timerText.text = "" + questionTime.ToString("F0");

        // Update progress & score
        progressSlider.value = (float)currentQuestionIndex / questions.Count;
        scoreText.text = "" + score;
    }

    public void SelectAnswer(int choiceIndex)
    {
        if (hasAnswered) return;

        hasAnswered = true;
        ForceQuizQuestion question = questions[currentQuestionIndex];

        // Disable all buttons
        foreach (Button button in choiceButtons)
            button.interactable = false;

        // Correct / Wrong answer coloring
        if (choiceIndex == question.correctAnswerIndex)
        {
            choiceImages[choiceIndex].color = Color.green;
            score++;
            AudioManager.Instance.PlaySFX(correct);
        }
        else
        {
            choiceImages[choiceIndex].color = Color.red;
            choiceImages[question.correctAnswerIndex].color = Color.green;
            AudioManager.Instance.PlaySFX(wrong);
        }

        scoreText.text = "" + score;
        Invoke(nameof(NextQuestion), 2f);
    }

    private void AutoWrongAnswer()
    {
        ForceQuizQuestion question = questions[currentQuestionIndex];

        foreach (Button button in choiceButtons)
            button.interactable = false;

        // Highlight correct
        choiceImages[question.correctAnswerIndex].color = Color.green;

        Invoke(nameof(NextQuestion), 2f);
    }

    private void NextQuestion()
    {
        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    private void EndQuiz()
    {
        progressSlider.value = 1f;
        bool passed = score >= passingScore;
        if (passed)
        {
            dbManager.MarkLessonAsCompleted(userID, 1);
            dbManager.AddUserItem(userID, rewardItemID);
            dbManager.CheckAndUnlockAllLessons(userID);
            lessonsHandler.RefreshLessonLocks();
            dbManager.AddCoin(userID, 100);
        }
        else
        {
            dbManager.AddCoin(userID, 50);
        }
        FindFirstObjectByType<ForceGameManager>().OnQuizComplete(passed);
        dbManager.SaveQuizAndScore(userID, 1, score);
        dbManager.CheckAndUnlockBadges(userID);
    }

    public void ResetQuiz()
    {
        currentQuestionIndex = 0;
        score = 0;
        hasAnswered = false;
        SetupQuestions();
    }
}
