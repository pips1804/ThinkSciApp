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
    public Image insulatorImage;
    public Sprite[] insulators;

    [Header("Database Manager")]
    public DatabaseManager dbManager;   // drag your DBManager here in Inspector

    private int currentQuestionIndex = 0;
    private List<Question> dbQuestions;
    void Start()
    {
        HousePanel.SetActive(false);
        ButtonsPanel.SetActive(false);
        StartCoroutine(GameFlow());

        insulatorImage.color = new Color(1, 1, 1, 0);
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

        // Load 10 random multiple choice questions for Quiz_ID = 13
        dbQuestions = dbManager.LoadRandomQuestions(13, "Multiple Choice", 10);

        currentQuestionIndex = 0;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        var q = dbQuestions[currentQuestionIndex];
        questionText.text = q.questionText;

        foreach (Button btn in optionButtons)
        {
            btn.image.color = Color.white;
            btn.interactable = true;
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i;
            optionButtonTexts[i].text = q.choices[i];
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
        int correctIndex = dbQuestions[currentQuestionIndex].correctAnswerIndex;

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
        if (currentQuestionIndex < dbQuestions.Count)
            ShowQuestion();
        else
            quizPanel.SetActive(false);
    }

    #region Material Buttons
    public void SetGlass()
    {
        HeatParticle.SetMaterial("Glass");
        insulatorImage.sprite = insulators[0];
        insulatorImage.color = new Color(1, 1, 1, 1);
    }
    public void SetBrick()
    {
        HeatParticle.SetMaterial("Brick");
        insulatorImage.sprite = insulators[1];
        insulatorImage.color = new Color(1, 1, 1, 1);
    }
    public void SetFoam()
    {
        HeatParticle.SetMaterial("Foam");
        insulatorImage.sprite = insulators[2];
        insulatorImage.color = new Color(1, 1, 1, 1);
    }
    public void SetMetal()
    {
        HeatParticle.SetMaterial("Metal");
        insulatorImage.sprite = insulators[3];
        insulatorImage.color = new Color(1, 1, 1, 1);
    }
    #endregion
}
