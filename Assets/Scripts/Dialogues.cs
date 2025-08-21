using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class Dialogue
{
    public int id;
    [TextArea(2, 5)] public List<string> lines;
}

public class Dialogues : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public Text dialogueText;
    public Text buttonText;
    public Button nextButton;

    [Header("Dialogue")]
    public List<Dialogue> dialogue = new List<Dialogue>();

    public bool dialogueFinished { get; private set; } = false; 

    private List<string> currentLines;
    private int currentLineIndex = 0;

    void Start()
    {
        dialoguePanel.SetActive(false);
        nextButton.onClick.AddListener(OnNextButton);
        StartDialogue(0);
    }

    public void StartDialogue(int scenarioId)
    {
        Dialogue scenario = dialogue.Find(s => s.id == scenarioId);

        if (scenario != null)
        {
            dialogueFinished = false; // reset flag
            currentLines = scenario.lines;
            currentLineIndex = 0;
            dialoguePanel.SetActive(true);
            ShowLine();
        }
    }

    private void ShowLine()
    {
        dialogueText.text = currentLines[currentLineIndex];

        // If last line → change button to OK
        if (currentLineIndex == currentLines.Count - 1)
            buttonText.text = "OK";
        else
            buttonText.text = "Next";
    }

    private void OnNextButton()
    {
        if (currentLineIndex < currentLines.Count - 1)
        {
            currentLineIndex++;
            ShowLine();
        }
        else
        {
            dialoguePanel.SetActive(false); 
            dialogueFinished = true;
        }
    }
}
