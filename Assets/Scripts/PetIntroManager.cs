using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text.RegularExpressions;

public class PetIntroManager : MonoBehaviour
{
    public GameObject introPanel;
    public Text dialogueText;
    public TMP_InputField nameInputField;
    public Text clickToContinueHint;
    public DatabaseManager dbManager;

    public GameObject loadingScreen;
    public Slider loadingSlider;

    private Coroutine typingCoroutine;
    private Coroutine blinkCoroutine;
    private bool isTyping = false;
    public Image petImage; // Assign in Inspector
    public Sprite greetSprite;
    public Sprite thinkingSprite;
    public Sprite happySprite;
    public Sprite sadSprite;

    [Header("Audio Settings")]
    public AudioSource petAudioSource; // Assign in Inspector
    public AudioClip greetingAudio; // "Hello there, {username}!"
    public AudioClip introAudio; // "I'm your science buddy..."
    public AudioClip nameRequestAudio; // "But first... I need a strong name..."
    public AudioClip confirmationAudio; // "Nice name! Let's go..."

    [Header("Modal Audio")]
    public AudioClip successSoundEffect; // Success modal sound
    public AudioClip failedSoundEffect; // Warning/failed modal sound

    [Header("Text Speed Settings")]
    [Range(0.01f, 0.1f)]
    public float baseTypeSpeed = 0.03f; // Base typing speed
    [Range(0.5f, 2.0f)]
    public float audioAlignmentMultiplier = 1.0f; // Multiplier to align with audio

    [Header("Warning Modal")]
    public GameObject warningModal;
    public Text warningSubtext;
    public Button warningOkayButton;

    [Header("Success Modal")]
    public GameObject successModal;
    public Text successSubtext;
    public Button successOkayButton;

    private string[] dialogLines = {
        "",
        "I'm your science buddy! Together, we'll explore forces, motion, and energy.",
        "But first… I need a strong name to power up! What would you like to call me?"
    };

    private AudioClip[] dialogAudios; // Will be populated in Start()

    private int currentLine = 0;
    private bool isAwaitingName = false;

    void Start()
    {
        // Initialize the audio array to match dialogue lines
        dialogAudios = new AudioClip[] {
            greetingAudio,      // Line 0: "Hello there, {username}!"
            introAudio,         // Line 1: "I'm your science buddy..."
            nameRequestAudio    // Line 2: "But first... I need a strong name..."
        };

        if (dbManager.IsPetNameDefault())
        {
            introPanel.SetActive(true);
            nameInputField.gameObject.SetActive(false);
            clickToContinueHint.gameObject.SetActive(true);

            StartIntro();
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
        // If we are typing and user clicks, finish the line instantly and skip audio
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            StopAudio(); // Stop the current audio
            dialogueText.text = dialogLines[currentLine];
            isTyping = false;

            // Increment here because the line is now fully revealed
            currentLine++;
            return;
        }

        // If there are more lines, start typing the next one
        if (currentLine < dialogLines.Length)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            UpdatePetSprite();

            // Play audio for this line if available
            PlayDialogueAudio(currentLine);

            // Calculate typing speed based on audio length
            float typingSpeed = CalculateTypingSpeed(currentLine);
            typingCoroutine = StartCoroutine(TypeLine(dialogLines[currentLine], typingSpeed));
            return;
        }

        // If we reached the end of dialog lines, ask for the name
        if (!isAwaitingName)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            string namePrompt = "Please enter a name for me: (12 characters max without special characters)";
            typingCoroutine = StartCoroutine(TypeLine(namePrompt, baseTypeSpeed));
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
        if (string.IsNullOrEmpty(newName))
        {
            ShowWarning("Pet name cannot be empty.");
            return;
        }

        if (newName.Length > 12)
        {
            ShowWarning("Pet name must be 12 characters or less.");
            return;
        }

        if (!Regex.IsMatch(newName, @"^[a-zA-Z0-9 ]+$"))
        {
            ShowWarning("Only letters, numbers, and spaces are allowed.");
            return;
        }

        // ✅ Passed validation
        dbManager.SavePetName(newName);
        ShowSuccess("Your pet's name is set! Let's apply some force and move forward!");
    }

    public void StartIntro()
    {
        currentLine = 0;
        nameInputField.text = "";
        nameInputField.gameObject.SetActive(false);
        clickToContinueHint.gameObject.SetActive(true);
        introPanel.SetActive(true);

        (string uname, int _, int _) = dbManager.GetUser();
        dialogLines[0] = $"Hello there, {uname}!";

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        UpdatePetSprite();
        PlayDialogueAudio(currentLine);
        float typingSpeed = CalculateTypingSpeed(currentLine);
        typingCoroutine = StartCoroutine(TypeLine(dialogLines[currentLine], typingSpeed));
    }

    public void HandleInputSubmit(string input)
    {
        OnNameEntered();
    }

    private void PlayDialogueAudio(int lineIndex)
    {
        if (petAudioSource != null && lineIndex < dialogAudios.Length && dialogAudios[lineIndex] != null)
        {
            petAudioSource.Stop(); // Stop any currently playing audio
            petAudioSource.clip = dialogAudios[lineIndex];
            petAudioSource.Play();
        }
    }

    private void StopAudio()
    {
        if (petAudioSource != null && petAudioSource.isPlaying)
        {
            petAudioSource.Stop();
        }
    }

    private float CalculateTypingSpeed(int lineIndex)
    {
        // If we have audio for this line, calculate typing speed to match audio duration
        if (lineIndex < dialogAudios.Length && dialogAudios[lineIndex] != null)
        {
            float audioLength = dialogAudios[lineIndex].length;
            float textLength = dialogLines[lineIndex].Length;

            if (audioLength > 0 && textLength > 0)
            {
                // Calculate speed to match audio duration
                float calculatedSpeed = audioLength / textLength;
                return calculatedSpeed * audioAlignmentMultiplier;
            }
        }

        // Fall back to base speed if no audio or calculation fails
        return baseTypeSpeed;
    }

    private void PlaySoundEffect(AudioClip soundEffect)
    {
        if (petAudioSource != null && soundEffect != null)
        {
            // Store the current clip to restore later if needed
            AudioClip previousClip = petAudioSource.clip;

            // Play the sound effect
            petAudioSource.PlayOneShot(soundEffect);
        }
    }

    private IEnumerator DelayedCloseAndLoad()
    {
        nameInputField.interactable = false;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        petImage.sprite = happySprite;

        // Play confirmation audio and calculate its typing speed
        string confirmationText = "Nice name! Let's go...";
        if (petAudioSource != null && confirmationAudio != null)
        {
            petAudioSource.Stop();
            petAudioSource.clip = confirmationAudio;
            petAudioSource.Play();

            // Calculate typing speed for confirmation audio
            float audioLength = confirmationAudio.length;
            float textLength = confirmationText.Length;
            float typingSpeed = audioLength > 0 && textLength > 0 ?
                (audioLength / textLength) * audioAlignmentMultiplier : baseTypeSpeed;

            yield return StartCoroutine(TypeLine(confirmationText, typingSpeed));
        }
        else
        {
            yield return StartCoroutine(TypeLine(confirmationText, baseTypeSpeed));
        }

        yield return new WaitForSeconds(1f);

        //  Step 1: Show loading screen
        loadingScreen.SetActive(true);
        loadingSlider.value = 0f;

        //  Step 2: Force Unity to render the UI
        yield return new WaitForEndOfFrame(); // This is critical

        //  Step 3: Start loading scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainScene");
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            loadingSlider.value = Mathf.Lerp(loadingSlider.value, progress, Time.deltaTime * 10f);

            if (asyncLoad.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private IEnumerator TypeLine(string line)
    {
        return TypeLine(line, baseTypeSpeed);
    }

    private IEnumerator TypeLine(string line, float typeSpeed)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;

        // ✅ Increment here so next click moves to the next line
        currentLine++;
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

    private void UpdatePetSprite()
    {
        // Change sprite depending on currentLine or special dialogues
        if (currentLine == 0) // "Hello there..."
        {
            petImage.sprite = greetSprite;
        }
        else if (currentLine == 1) // "I'm your companion..."
        {
            petImage.sprite = greetSprite;
        }
        else if (currentLine == 2) // "But first... I need a name!"
        {
            petImage.sprite = thinkingSprite;
        }
    }

    private void ShowWarning(string message)
    {
        petImage.sprite = sadSprite;
        StopAudio(); // Stop any playing audio when showing warning

        // Play failed sound effect
        PlaySoundEffect(failedSoundEffect);

        if (warningModal != null)
        {
            warningModal.SetActive(true);

            if (warningSubtext != null)
                warningSubtext.text = message;
        }

        if (warningOkayButton != null)
        {
            warningOkayButton.onClick.RemoveAllListeners();
            warningOkayButton.onClick.AddListener(() =>
            {
                warningModal.SetActive(false);
            });
        }
    }

    private void ShowSuccess(string message)
    {
        // Play success sound effect
        PlaySoundEffect(successSoundEffect);

        if (successModal != null)
        {
            successModal.SetActive(true);

            if (successSubtext != null)
                successSubtext.text = message;
        }

        if (successOkayButton != null)
        {
            successOkayButton.onClick.RemoveAllListeners();
            successOkayButton.onClick.AddListener(() =>
            {
                successModal.SetActive(false);
                StartCoroutine(DelayedCloseAndLoad()); // scene change or next step
            });
        }
    }

    // Public methods to adjust settings at runtime if needed
    public void SetBaseTypeSpeed(float speed)
    {
        baseTypeSpeed = Mathf.Clamp(speed, 0.01f, 0.1f);
    }

    public void SetAudioAlignmentMultiplier(float multiplier)
    {
        audioAlignmentMultiplier = Mathf.Clamp(multiplier, 0.5f, 2.0f);
    }
}
