using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HeatEscape : MonoBehaviour
{
    public GameObject HousePanel;
    public GameObject ButtonsPanel;
    public GameObject quizPanel;
    public Text questionText;
    public List<Button> optionButtons;
    public List<Text> optionButtonTexts;
    public List<Image> optionButtonImages;
    public Dialogues dialogues;

    private int currentQuestionIndex = 0;

    private List<string> questions = new List<string>()
    {
        "Which material lets heat escape the fastest?",
        "Which material bounces heat the most?",
        "Which material slows down heat the most?",
        "Which material is the least effective at heat insulation?",
        "Which material absorbs the most heat?",
        "Which material reflects heat the best?",
        "Which material allows heat to pass through easily?",
        "Which material is strongest against heat impact?",
        "Which material reduces heat transfer the most?",
        "Which material causes particles to disappear the fastest?"
    };

    private List<string[]> options = new List<string[]>()
    {
        new string[] { "Glass", "Brick", "Foam", "Metal" },
        new string[] { "Brick", "Metal", "Foam", "Glass" },
        new string[] { "Foam", "Glass", "Brick", "Metal" },
        new string[] { "Foam", "Glass", "Brick", "Metal" },
        new string[] { "Metal", "Foam", "Glass", "Brick" },
        new string[] { "Metal", "Brick", "Foam", "Glass" },
        new string[] { "Glass", "Metal", "Brick", "Foam" },
        new string[] { "Metal", "Glass", "Foam", "Brick" },
        new string[] { "Foam", "Brick", "Glass", "Metal" },
        new string[] { "Glass", "Brick", "Foam", "Metal" }
    };

    private int[] correctAnswers = { 0, 1, 0, 1, 2, 0, 0, 0, 0, 0 };

    void Start()
    {
        HousePanel.SetActive(false);
        ButtonsPanel.SetActive(false);
        StartCoroutine(GameFlow());
    }

    private IEnumerator GameFlow()
    {
        dialogues.StartDialogue(0);
        yield return new WaitUntil(() => dialogues.dialogueFinished);

        HousePanel.SetActive(true);
        ButtonsPanel.SetActive(true);

        yield return StartCoroutine(SimulationTimer());

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
            yield return null;
        }
    }

    public void StartQuiz()
    {
        HousePanel.SetActive(false);
        ButtonsPanel.SetActive(false);
        quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        questionText.text = questions[currentQuestionIndex];

        foreach (Button btn in optionButtons)
        {
            btn.image.color = Color.white;
            btn.interactable = true;
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i;
            optionButtonTexts[i].text = options[currentQuestionIndex][i];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }
    }

    private void OnOptionSelected(int selectedIndex)
    {
        StartCoroutine(HandleAnswer(selectedIndex));
    }

    private IEnumerator HandleAnswer(int selectedIndex)
    {
        int correctIndex = correctAnswers[currentQuestionIndex];

        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i == correctIndex)
                optionButtonImages[i].color = Color.green;
            else if (i == selectedIndex)
                optionButtonImages[i].color = Color.red;
            else
                optionButtonImages[i].color = Color.white;

            optionButtons[i].interactable = false;
        }

        yield return new WaitForSeconds(1.5f);

        currentQuestionIndex++;
        if (currentQuestionIndex < questions.Count)
            ShowQuestion();
        else
            quizPanel.SetActive(false);
    }

    public void SetGlass() => HeatParticle.SetMaterial("Glass");
    public void SetBrick() => HeatParticle.SetMaterial("Brick");
    public void SetFoam() => HeatParticle.SetMaterial("Foam");
    public void SetMetal() => HeatParticle.SetMaterial("Metal");
}
