using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class PetIntroManager : MonoBehaviour
{
    public GameObject introPanel;
    public Text dialogueText;
    public TMP_InputField nameInputField;
    public Text clickToContinueHint;
    public DatabaseManager dbManager;

    private Coroutine typingCoroutine;
    private Coroutine blinkCoroutine;
    private bool isTyping = false;

    private string[] dialogLines = {
        "",
        "I'm your companion for this journey. We'll learn, battle, and grow together!",
        "But first... I need a name! What would you like to call me?"
    };

    private int currentLine = 0;
    private bool isAwaitingName = false;

    void Start()
    {
        if (dbManager.IsPetNameDefault())
        {
            introPanel.SetActive(true);
            nameInputField.gameObject.SetActive(false);
            clickToContinueHint.gameObject.SetActive(true);

            (string fname, string _, string _, int _) = dbManager.GetUser();
            dialogLines[0] = $"Hello there, {fname}!";

            typingCoroutine = StartCoroutine(TypeLine(dialogLines[0]));
            blinkCoroutine = StartCoroutine(BlinkHint());
        }
        else
        {
            introPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (introPanel.activeSelf && !isAwaitingName && Input.GetMouseButtonDown(0))
        {
            ShowNextLine();
        }
    }

    void ShowNextLine()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = dialogLines[currentLine];
            isTyping = false;
            currentLine++;
            return;
        }

        if (currentLine < dialogLines.Length)
        {
            typingCoroutine = StartCoroutine(TypeLine(dialogLines[currentLine]));
            currentLine++;
        }
        else if (!isAwaitingName)
        {
            typingCoroutine = StartCoroutine(TypeLine("Please enter a name for me:"));
            nameInputField.gameObject.SetActive(true);
            clickToContinueHint.gameObject.SetActive(false);
            isAwaitingName = true;

            nameInputField.Select();
            nameInputField.ActivateInputField();
        }
    }


    public void OnNameEntered()
    {
        string newName = nameInputField.text.Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            dbManager.SavePetName(newName);
            StartCoroutine(DelayedCloseAndLoad());
        }
    }

    public void StartIntro()
    {
        currentLine = 0;
        nameInputField.text = "";
        nameInputField.gameObject.SetActive(false);
        clickToContinueHint.gameObject.SetActive(true);
        introPanel.SetActive(true);
        typingCoroutine = StartCoroutine(TypeLine(dialogLines[currentLine]));
    }

    public void HandleInputSubmit(string input)
    {
        OnNameEntered();
    }

    private IEnumerator DelayedCloseAndLoad()
    {
        nameInputField.interactable = false;
        yield return StartCoroutine(TypeLine("Nice name! Let's go..."));
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("MainScene");
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.03f);
        }
        isTyping = false;
    }


    private IEnumerator BlinkHint()
    {
        while (true)
        {
            clickToContinueHint.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            clickToContinueHint.gameObject.SetActive(false);
            yield return new WaitForSeconds(1f);
        }
    }

}