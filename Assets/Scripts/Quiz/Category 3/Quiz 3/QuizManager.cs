using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class QuizQuestion
{
    public string question;
    public string[] answers; // length = 4
    public int correctAnswerIndex; // 0–3
}

public class QuizManager : MonoBehaviour
{
    [Header("UI References")]
    public Text questionText;
    public Button[] answerButtons; // drag your 4 buttons here
    public GameObject quizPanel;

    [Header("Quiz Data")]
    public QuizQuestion[] questions; // fill in Inspector
    private int currentQuestion = 0;

    void Start()
    {
        quizPanel.SetActive(false);

        // Attach button listeners dynamically
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i; // local copy to avoid closure issue
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }
    }

    public void StartQuiz()
    {
        quizPanel.SetActive(true);
        currentQuestion = 0;
        ShowQuestion();
    }

    void ShowQuestion()
    {
        if (currentQuestion >= questions.Length)
        {
            EndQuiz();
            return;
        }

        QuizQuestion q = questions[currentQuestion];
        questionText.text = q.question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].GetComponentInChildren<Text>().text = q.answers[i];
        }
    }

    void OnAnswerSelected(int index)
    {
        bool isCorrect = (index == questions[currentQuestion].correctAnswerIndex);

        if (isCorrect)
        {
            Debug.Log("✅ Correct!");
        }
        else
        {
            Debug.Log("❌ Wrong!");
        }

        currentQuestion++;
        ShowQuestion();
    }

    void EndQuiz()
    {
        quizPanel.SetActive(false);
    }
}
