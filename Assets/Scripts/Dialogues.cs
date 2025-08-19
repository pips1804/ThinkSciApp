using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class Dialogue
{
    public int id;                      // Scenario number (e.g., 1, 2, 3)
    [TextArea(2, 5)] public List<string> lines;  // Lines of dialogue for this scenario
}

public class Dialogues : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public Text dialogueText;
    public Text buttonText;
    public Button nextButton;

    [Header("Dialogue Scenarios")]
    public List<Dialogue> scenarios = new List<Dialogue>();

    private List<string> currentLines;
    private int currentLineIndex = 0;

    void Start()
    {
        dialoguePanel.SetActive(false);
        nextButton.onClick.AddListener(OnNextButton);
    }

    // Call this to start a scenario by ID
    public void StartDialogue(int scenarioId)
    {
        Dialogue scenario = scenarios.Find(s => s.id == scenarioId);

        if (scenario != null)
        {
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
            dialoguePanel.SetActive(false); // Close when finished
        }
    }
}
