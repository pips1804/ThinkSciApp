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

    private List<Question> dbQuestions;    // from DatabaseManager
    private int currentQuestion = 0;
    private Color defaultColor;

    void Start()
    {
        quizPanel.SetActive(false);

        // Save default background color (from first button)
        defaultColor = answerBackgrounds[0].color;

        // Attach button listeners dynamically
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }
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
    ShowQuestion();
}

    void ShowQuestion()
    {
        if (currentQuestion >= dbQuestions.Count)
        {
            EndQuiz();
            return;
        }

        Question q = dbQuestions[currentQuestion];
        questionText.text = q.questionText;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerTexts[i].text = q.choices[i];
            answerButtons[i].interactable = true;
            answerBackgrounds[i].color = defaultColor; // reset background
        }
    }

    void OnAnswerSelected(int index)
    {
        foreach (Button btn in answerButtons)
            btn.interactable = false;

        Question q = dbQuestions[currentQuestion];
        int correctIndex = q.correctAnswerIndex;

        if (index == correctIndex)
        {
            Debug.Log("✅ Correct!");
            answerBackgrounds[index].color = Color.green;
        }
        else
        {
            Debug.Log("❌ Wrong!");
            answerBackgrounds[index].color = Color.red;
            answerBackgrounds[correctIndex].color = Color.green;
        }

        StartCoroutine(NextQuestionDelay());
    }

    IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(3f);
        currentQuestion++;
        ShowQuestion();
    }

    void EndQuiz()
    {
        quizPanel.SetActive(false);
        Debug.Log("✅ Quiz finished!");
    }
}
