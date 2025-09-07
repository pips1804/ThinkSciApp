// GameManager.cs - Works with your existing Dialogues.cs script
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ForceGameManager : MonoBehaviour
{
    [Header("Game Flow")]
    public TugOfWarSimulation tugOfWar;
    public Dialogues dialogueSystem; // Your existing Dialogues script
    public ForceQuizManager quizManager;

    [Header("UI Panels")]
    public GameObject simulationPanel;
    public GameObject quizPanel;
    public GameObject passModal;
    public GameObject failModal;

    [Header("Buttons")]
    public Button retakeButton;

    [Header("Dialogue IDs")]
    public int preSimulationDialogueId = 0;  // ID for pre-simulation dialogue
    public int postSimulationDialogueId = 1; // ID for post-simulation dialogue

    private GameState currentState = GameState.PreSimulationDialogue;

    public enum GameState
    {
        PreSimulationDialogue,
        Simulation,
        PostSimulationDialogue,
        Quiz,
        Results
    }

    private void Start()
    {
        StartGame();
        retakeButton.onClick.AddListener(RestartFromSimulation);
    }

    private void Update()
    {
        // Check YOUR dialogue system's dialogueFinished property
        if (currentState == GameState.PreSimulationDialogue && dialogueSystem.dialogueFinished)
        {
            currentState = GameState.Simulation;
            StartSimulationPhase();
        }
        else if (currentState == GameState.PostSimulationDialogue && dialogueSystem.dialogueFinished)
        {
            currentState = GameState.Quiz;
            StartQuiz();
        }
    }

    public void StartGame()
    {
        currentState = GameState.PreSimulationDialogue;
        ShowSimulationPanel();

        // Call YOUR StartDialogue method with ID 0
        dialogueSystem.StartDialogue(preSimulationDialogueId);
    }

    private void StartSimulationPhase()
    {
        tugOfWar.StartSimulation();
    }

    public void OnSimulationComplete()
    {
        currentState = GameState.PostSimulationDialogue;

        // Call YOUR StartDialogue method with ID 1
        dialogueSystem.StartDialogue(postSimulationDialogueId);
    }

    private void StartQuiz()
    {
        ShowQuizPanel();
        quizManager.StartQuiz();
        currentState = GameState.Quiz;
    }

    public void OnQuizComplete(bool passed)
    {
        currentState = GameState.Results;

        if (passed)
        {
            passModal.SetActive(true);
        }
        else
        {
            failModal.SetActive(true);
        }
    }

    public void RestartFromSimulation()
    {
        // Reset all systems
        tugOfWar.ResetSimulation();
        quizManager.ResetQuiz();

        // Hide modals
        passModal.SetActive(false);
        failModal.SetActive(false);

        // Start over
        StartGame();
    }

    private void ShowSimulationPanel()
    {
        simulationPanel.SetActive(true);
        quizPanel.SetActive(false);
    }

    private void ShowQuizPanel()
    {
        simulationPanel.SetActive(false);
        quizPanel.SetActive(true);
    }
}
