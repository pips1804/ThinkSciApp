using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    private List<ForceQuizQuestion> questions;
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool hasAnswered = false;
    private float timeRemaining;

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
        TugOfWarSimulation simulation = FindObjectOfType<TugOfWarSimulation>();

        questions = new List<ForceQuizQuestion>
        {
            new ForceQuizQuestion
            {
                questionText = "What determines which side wins in tug of war?",
                choices = new string[] { "Number of pets", "Total force", "Pet size", "Rope length" },
                correctAnswerIndex = 1
            },
            new ForceQuizQuestion
            {
                questionText = "If the left side has 5 units of force and right side has 3 units, what happens?",
                choices = new string[] { "Right side wins", "Left side wins", "It's a tie", "Nothing happens" },
                correctAnswerIndex = 1
            },
            new ForceQuizQuestion
            {
                questionText = "What is force?",
                choices = new string[] { "A push or pull", "Speed of movement", "Size of object", "Color of rope" },
                correctAnswerIndex = 0
            },
            new ForceQuizQuestion
            {
                questionText = "When forces are unbalanced, what happens?",
                choices = new string[] { "Nothing", "Movement occurs", "Forces disappear", "Rope breaks" },
                correctAnswerIndex = 1
            },
            new ForceQuizQuestion
            {
                questionText = "In your simulation, which side had more force?",
                choices = new string[] {
                    "Left side",
                    "Right side",
                    "Equal force",
                    "No force applied"
                },
                correctAnswerIndex = simulation != null && simulation.DidRightWin() ? 1 : 0
            },

            // --- 10 more questions ---
            new ForceQuizQuestion
            {
                questionText = "Balanced forces cause what effect on an object?",
                choices = new string[] { "Change in motion", "No change", "Increase speed", "Break the object" },
                correctAnswerIndex = 1
            },
            new ForceQuizQuestion
            {
                questionText = "What unit is used to measure force?",
                choices = new string[] { "Newton", "Watt", "Joule", "Meter" },
                correctAnswerIndex = 0
            },
            new ForceQuizQuestion
            {
                questionText = "Which law states that every action has an equal and opposite reaction?",
                choices = new string[] { "Newton's 1st", "Newton's 2nd", "Newton's 3rd", "Law of Gravity" },
                correctAnswerIndex = 2
            },
            new ForceQuizQuestion
            {
                questionText = "If two people pull a rope with equal force in opposite directions, the rope will…",
                choices = new string[] { "Move left", "Move right", "Stay still", "Break" },
                correctAnswerIndex = 2
            },
            new ForceQuizQuestion
            {
                questionText = "Which factor increases the pulling force in tug of war?",
                choices = new string[] { "More players pulling", "Shorter rope", "Different colors", "Standing still" },
                correctAnswerIndex = 0
            },
            new ForceQuizQuestion
            {
                questionText = "What type of force slows objects down when sliding?",
                choices = new string[] { "Friction", "Gravity", "Magnetism", "Push" },
                correctAnswerIndex = 0
            },
            new ForceQuizQuestion
            {
                questionText = "Gravity always pulls objects…",
                choices = new string[] { "Up", "Sideways", "Down", "In circles" },
                correctAnswerIndex = 2
            },
            new ForceQuizQuestion
            {
                questionText = "A stronger force produces…",
                choices = new string[] { "Less motion", "Greater motion", "No motion", "Equal motion" },
                correctAnswerIndex = 1
            },
            new ForceQuizQuestion
            {
                questionText = "If one side suddenly stops pulling in tug of war, what happens?",
                choices = new string[] { "Both sides win", "The other side falls", "Rope breaks", "Nothing" },
                correctAnswerIndex = 1
            },
            new ForceQuizQuestion
            {
                questionText = "Forces can change an object’s…",
                choices = new string[] { "Shape", "Speed", "Direction", "All of these" },
                correctAnswerIndex = 3
            }
        };
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
        FindObjectOfType<ForceGameManager>().OnQuizComplete(passed);
    }

    public void ResetQuiz()
    {
        currentQuestionIndex = 0;
        score = 0;
        hasAnswered = false;
    }
}
