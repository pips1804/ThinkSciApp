using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    public List<SwipeQuestions> questions;
    public Text questionText, leftText, rightText;
    public Text timerText;

    public GameObject resultPanel;                
    public Button mainMenuButton;         

    private int currentQuestionIndex = 0;
    private float timer = 10f;
    private bool isTimerRunning = false;

    public BattleManager battleManager;

    void Start()
    {
        DisplayQuestion();
        //Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void Update()
    {
        if (isTimerRunning)
        {
            timer -= Time.deltaTime;
            timerText.text = Mathf.Ceil(timer).ToString();

            if (timer <= 0)
            {
                isTimerRunning = false;
                timerText.text = "0";
                PlayerMissedAnswer();
            }
        }
    }

    void PlayerMissedAnswer()
    {
        battleManager.PlayerTakeDamage(10);
        MoveToNextQuestion();
    }

    public void HandleAnswer(string swipeDirection)
    {
        isTimerRunning = false;

        var question = questions[currentQuestionIndex];

        if (swipeDirection == question.correctAnswer)
        {
            battleManager.EnemyTakeDamage(10);
        }
        else
        {
            battleManager.PlayerTakeDamage(10);
        }

        MoveToNextQuestion();
    }

    void MoveToNextQuestion()
    {
        currentQuestionIndex++;

        if (currentQuestionIndex < questions.Count)
        {
            DisplayQuestion();
        }
        else
        {
            EndQuiz();
        }
    }

    void DisplayQuestion()
    {
        var question = questions[currentQuestionIndex];
        questionText.text = question.questionText;
        leftText.text = question.leftAnswerText;
        rightText.text = question.rightAnswerText;

        timer = 10f;
        isTimerRunning = true;
    }

    void EndQuiz()
    {
        timerText.text = "";
        questionText.text = "";
        leftText.text = "";
        rightText.text = "";

        resultPanel.SetActive(true);
    }
}
