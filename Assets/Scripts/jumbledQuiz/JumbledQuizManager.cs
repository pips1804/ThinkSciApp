using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class JumbledQuizManager : MonoBehaviour
{
    public List<JumbledQuestion> questions;
    public Text questionText;
    public GameObject letterButtonPrefab;
    public Transform letterContainer;
    public Text timerText;
    public GameObject resultPanel;
    public Text scoreText;
    public Button submitButton;

    private int currentQuestionIndex = 0;
    private List<Button> letterButtons = new List<Button>();
    private int selectedIndex = -1;
    private float timer = 30f;
    private bool isTimerRunning = true;
    private int score = 0;
    private Color defaultColor = Color.white;
    private Color selectedColor = Color.black   ;


    void Start()
    {
        resultPanel.SetActive(false);
        DisplayQuestion();
        scoreText.text = $"Score: {score}/{questions.Count}";
    }

    void Update()
    {
        if (isTimerRunning)
        {
            timer -= Time.deltaTime;
            timerText.text = Mathf.CeilToInt(timer).ToString();

            if (timer <= 0)
            {
                isTimerRunning = false;
                SubmitAnswer();
            }
        }
    }

    void DisplayQuestion()
    {
        ClearLetters();
        var question = questions[currentQuestionIndex];
        questionText.text = question.question;

        string shuffled = new string(question.answer.ToCharArray().OrderBy(c => Random.value).ToArray());

        for (int i = 0; i < shuffled.Length; i++)
        {
            GameObject letterObj = Instantiate(letterButtonPrefab, letterContainer);
            Button letterButton = letterObj.GetComponent<Button>();
            int index = i;
            letterButton.GetComponentInChildren<Text>().text = shuffled[i].ToString();
            letterButton.onClick.AddListener(() => OnLetterClick(index));
            letterButtons.Add(letterButton);
        }

        timer = 30f;
        isTimerRunning = true;
    }

    void OnLetterClick(int index)
    {
        if (index < 0 || index >= letterButtons.Count)
            return;

        if (selectedIndex == -1)
        {
            selectedIndex = index;
            letterButtons[index].GetComponentInChildren<Text>().color = selectedColor;
        }
        else if (selectedIndex != index)
        {
            string temp = letterButtons[selectedIndex].GetComponentInChildren<Text>().text;
            letterButtons[selectedIndex].GetComponentInChildren<Text>().text = letterButtons[index].GetComponentInChildren<Text>().text;
            letterButtons[index].GetComponentInChildren<Text>().text = temp;

            letterButtons[selectedIndex].GetComponentInChildren<Text>().color = defaultColor;
            letterButtons[index].GetComponentInChildren<Text>().color = defaultColor;

            selectedIndex = -1;
        }
        else
        {
            letterButtons[selectedIndex].GetComponentInChildren<Text>().color = defaultColor;
            selectedIndex = -1;
        }
    }



    public void OnSubmitButton()
    {
        isTimerRunning = false;
        SubmitAnswer();
    }

    void SubmitAnswer()
    {
        string userAnswer = string.Concat(letterButtons.Select(b => b.GetComponentInChildren<Text>().text));
        if (userAnswer == questions[currentQuestionIndex].answer)
        {
            score += 1;
            scoreText.text = $"Score: {score}/{questions.Count}";
        }

        currentQuestionIndex++;
        if (currentQuestionIndex < questions.Count)
        {
            DisplayQuestion();
        }
        else
        {
            ShowResult();
        }
    }

    void ShowResult()
    {
        resultPanel.SetActive(true);
        questionText.text = "";
        timerText.text = "";
        ClearLetters();
    }

    void ClearLetters()
    {
        foreach (Transform child in letterContainer)
        {
            Destroy(child.gameObject);
        }
        letterButtons.Clear();
    }
}
