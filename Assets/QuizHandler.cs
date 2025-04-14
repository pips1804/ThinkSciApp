using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class Question
{
    public string questionText;
    public string[] answers;
    public int correctAnswerIndex;
}

public class QuizHandler : MonoBehaviour
{
    public Text questionText;
    public Button[] answerButtons;
    public Text[] answerTexts;

    public Button nextButton;
    public Button backButton;

    public Color correctColor;
    public Color wrongColor;
    public Color defaultColor;

    public List<Question> questions;
    private int currentQuestionIndex = 0;
    private bool hasAnswered = false;

    private List<int> selectedAnswers = new List<int>(); // -1 if unanswered

    void Start()
    {
        // Initialize all answers as unanswered (-1)
        for (int i = 0; i < questions.Count; i++)
            selectedAnswers.Add(-1);

        LoadQuestion();
    }

    void LoadQuestion()
    {
        ResetButtonColors();

        Question q = questions[currentQuestionIndex];
        questionText.text = q.questionText;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerTexts[i].text = q.answers[i];
            int index = i;

            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => CheckAnswer(index));
        }

        int answeredIndex = selectedAnswers[currentQuestionIndex];
        hasAnswered = answeredIndex != -1;

        if (hasAnswered)
        {
            ShowPreviousAnswer();
        }
        else
        {
            EnableButtons(true);
        }

        backButton.interactable = currentQuestionIndex > 0;
        nextButton.interactable = hasAnswered && currentQuestionIndex < questions.Count - 1;
    }

    void ShowPreviousAnswer()
    {
        int correct = questions[currentQuestionIndex].correctAnswerIndex;
        int selected = selectedAnswers[currentQuestionIndex];

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Image btnImage = answerButtons[i].GetComponent<Image>();

            if (i == correct)
            {
                btnImage.color = correctColor;
            }
            else if (i == selected)
            {
                btnImage.color = wrongColor;
            }
            else
            {
                btnImage.color = defaultColor;
            }

            answerButtons[i].interactable = false;
        }
    }

    void CheckAnswer(int index)
    {
        if (hasAnswered) return;

        hasAnswered = true;
        selectedAnswers[currentQuestionIndex] = index;

        int correct = questions[currentQuestionIndex].correctAnswerIndex;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = false;
            answerButtons[i].GetComponent<Image>().color =
                (i == correct) ? correctColor : wrongColor;
        }

        nextButton.interactable = true;
    }

    void EnableButtons(bool state)
    {
        foreach (Button btn in answerButtons)
            btn.interactable = state;
    }

    public void NextQuestion()
    {
        if (currentQuestionIndex < questions.Count - 1)
        {
            currentQuestionIndex++;
            LoadQuestion();
        }
    }

    public void PreviousQuestion()
    {
        if (currentQuestionIndex > 0)
        {
            currentQuestionIndex--;
            LoadQuestion();
        }
    }

    void ResetButtonColors()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].GetComponent<Image>().color = defaultColor;
        }
    }

}
