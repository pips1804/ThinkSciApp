using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SortingGame : MonoBehaviour
{
    [Header("UI Elements")]
    public Image earthImage;             // The UI Image for Earth health
    public Sprite[] earthStates;         // 5 sprites from healthy to polluted
    public Text scoreText;               // UI text for score display
    public Text waveText;                // UI text to show current wave
    public Slider progressSlider;        // UI Slider to track progress

    [Header("Target Icon Display")]
    public Image targetIconImage;        // UI Image to show the current target icon
    public Text targetIconNameText;      // Text to show target icon name

    [Header("Game Settings")]
    public RectTransform spawnArea;      // Parent canvas/area for spawning
    public GameObject[] renewableIcons;  // Array of 5 renewable icon prefabs
    public string[] renewableIconNames;  // Names for renewable icons (match index)
    public GameObject[] fossilIcons;     // Array of fossil fuel icon prefabs
    public string[] fossilIconNames;     // Names for fossil icons

    [Header("Bin Settings")]
    public Image binImage;               // UI Image for the bin
    public RectTransform binCatchZone;   // Child GameObject representing the bin hole/catch area
    public float binMoveSpeed = 600f;    // Bin movement speed
    public float baseFallDuration = 3f;  // Base time for icon to fall from top to bottom
    public float baseSpawnInterval = 1f; // Base time between icon spawns

    [Header("Challenge Settings")]
    [Range(0f, 200f)]
    public float maxWindStrength = 100f; // Maximum wind drift strength
    [Range(0.5f, 2f)]
    public float minGravityMultiplier = 0.7f; // Minimum gravity speed multiplier
    [Range(0.5f, 2f)]
    public float maxGravityMultiplier = 1.5f; // Maximum gravity speed multiplier

    [Header("Wave Settings")]
    public int iconsPerWave = 15;        // Total icons to spawn per wave
    public int fossilIconsPerWave = 10;  // Number of fossil icons per wave
    public int targetIconsPerWave = 5;   // Number of target icons per wave

    [Header("Life Saver Quiz Settings")]
    public GameObject quizModal;         // Modal panel for the quiz
    public Text questionText;            // Text component to display the question
    public Transform lettersContainer;   // Container for letter buttons
    public GameObject letterButtonPrefab; // Prefab for individual letter buttons
    public Button submitAnswerButton;    // Button to submit the answer
    public Text feedbackText;            // Text to show quiz feedback
    public Button continueButton;        // Button to continue after quiz
    public Color correctLetterColor = Color.green;
    public Color wrongLetterColor = Color.red;
    public Color defaultLetterColor = Color.white;

    [Header("Game State")]
    private int currentEarthState = 0;
    private int score = 0;
    private int currentWave = 0;
    private int targetIconIndex = 0;     // Index of current target renewable icon
    private int iconsSpawnedThisWave = 0;
    private int targetIconsCaughtThisWave = 0;
    private List<GameObject> activeIcons = new List<GameObject>();
    private bool isSpawning = false;
    private float currentFallDuration;   // Dynamic fall duration for current wave
    private float currentSpawnInterval;  // Dynamic spawn interval for current wave
    private bool isPaused = false;       // Game pause state
    private List<Coroutine> activeCoroutines = new List<Coroutine>(); // Track active coroutines for pausing

    [Header("Dialogue System")]
    public Dialogues dialogues;
    private bool gameStarted = false;

    [Header("Panel")]
    public GameObject Earth;
    public GameObject SpawnArea;
    public GameObject MainBin;
    public GameObject Score;
    public GameObject WaveInfo;
    public GameObject TargetDisplay;
    public GameObject Header;
    public GameObject Settings;
    public GameObject QuizProgress;

    [Header("End Game Modals")]
    public GameObject passModal;         // Modal to show when player passes
    public GameObject failModal;         // Modal to show when player fails
    public Text passText;
    public Text failText;

    // Store the relative position of catch zone to bin for proper syncing
    private Vector2 catchZoneOffset;

    [Header("Sound Effects")]
    public AudioClip passed;
    public AudioClip failed;
    public AudioClip correct;
    public AudioClip wrong;
    public DatabaseManager dbManager;
    public LessonLocker lessonHandler;
    public int userID;
    public int rewardItemID;

    // Track if this is the first enable or a re-enable
    private bool hasBeenInitialized = false;

    // Quiz-related variables
    private List<GameObject> letterButtons = new List<GameObject>();
    private string currentAnswer = "";
    private string correctAnswer = "";
    private bool isQuizActive = false;

    void Start()
    {
        Debug.Log("=== SORTING GAME START ===");
        InitializeGame();
        InitializeQuiz();
    }

    // Initialize quiz components
    void InitializeQuiz()
    {
        Debug.Log("Initializing life saver quiz system...");

        if (quizModal != null)
            quizModal.SetActive(false);

        if (submitAnswerButton != null)
            submitAnswerButton.onClick.AddListener(SubmitQuizAnswer);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueAfterQuiz);
    }

    // Called when the GameObject is enabled
    void OnEnable()
    {
        Debug.Log("=== SORTING GAME ENABLED ===");

        // If this has been initialized before, it means we're re-enabling after being disabled
        if (hasBeenInitialized)
        {
            Debug.Log("Re-enabling game - performing full restart");
            PerformFullRestart();
        }
    }

    // Called when the GameObject is disabled
    void OnDisable()
    {
        Debug.Log("=== SORTING GAME DISABLED ===");

        // Stop all coroutines and clean up when disabled
        StopAllCoroutines();

        // Clean up active icons
        CleanupActiveIcons();
    }

    // Initialize the game for the first time
    void InitializeGame()
    {
        Debug.Log("=== INITIALIZING GAME FOR FIRST TIME ===");

        // Calculate catch zone offset relative to bin
        if (binCatchZone != null && binImage != null)
        {
            catchZoneOffset = binCatchZone.anchoredPosition - binImage.rectTransform.anchoredPosition;
            Debug.Log($"Catch zone offset calculated: {catchZoneOffset}");
        }

        // Reset all game state to initial values
        ResetGameState();

        // Hide all UI elements initially
        HideAllGameUI();

        // Hide modals initially
        if (passModal != null) passModal.SetActive(false);
        if (failModal != null) failModal.SetActive(false);
        if (quizModal != null) quizModal.SetActive(false);

        // Start the dialogue before the game
        if (dialogues != null)
        {
            Debug.Log("Starting dialogue...");
            dialogues.StartDialogue(0);
            StartCoroutine(WaitForDialogueThenStartGame());
        }
        else
        {
            Debug.Log("No dialogue found, starting game immediately");
            BeginGame();
        }

        // Mark as initialized
        hasBeenInitialized = true;
    }

    // Perform a full restart when re-enabling
    void PerformFullRestart()
    {
        Debug.Log("=== PERFORMING FULL RESTART ===");

        // Stop all running coroutines
        StopAllCoroutines();

        // Clean up any remaining icons
        CleanupActiveIcons();

        // Reset all game state
        ResetGameState();

        HideAllGameUI();

        // Hide modals
        if (passModal != null) passModal.SetActive(false);
        if (failModal != null) failModal.SetActive(false);
        if (quizModal != null) quizModal.SetActive(false);

        // Always start from dialogue on restart
        if (dialogues != null)
        {
            Debug.Log("Starting fresh dialogue on restart");
            // StartDialogue already resets dialogueFinished to false internally
            dialogues.StartDialogue(0);
            StartCoroutine(WaitForDialogueThenStartGame());
        }
        else
        {
            Debug.Log("No dialogue system found, beginning game immediately");
            BeginGame();
        }
    }

    // Reset all game state variables to their initial values
    void ResetGameState()
    {
        Debug.Log("Resetting all game state variables");

        // Reset core game variables
        score = 0;
        currentWave = 0;
        currentEarthState = 0;
        targetIconIndex = 0;
        iconsSpawnedThisWave = 0;
        targetIconsCaughtThisWave = 0;
        isSpawning = false;
        isPaused = false;
        gameStarted = false;
        isQuizActive = false;

        // Reset dynamic difficulty values
        currentFallDuration = baseFallDuration;
        currentSpawnInterval = baseSpawnInterval;

        // Clear active coroutines list
        activeCoroutines.Clear();

        // Reset UI elements to initial state
        ResetUIElements();
    }

    // Reset UI elements to their initial state
    void ResetUIElements()
    {
        Debug.Log("Resetting UI elements to initial state");

        // Reset Earth to healthy state
        if (earthImage != null && earthStates.Length > 0)
        {
            earthImage.sprite = earthStates[0];
            earthImage.color = Color.white;
            earthImage.rectTransform.localScale = Vector3.one;
        }

        // Reset score display
        if (scoreText != null)
            scoreText.text = "0";

        // Reset wave display
        if (waveText != null)
            waveText.text = "1/5";

        // Reset progress slider
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = 5;
            progressSlider.value = 0;
        }

        // Reset bin color
        if (binImage != null)
            binImage.color = Color.white;

        // Clear target display
        if (targetIconImage != null)
            targetIconImage.sprite = null;

        if (targetIconNameText != null)
            targetIconNameText.text = "";
    }

    // Clean up all active icons
    void CleanupActiveIcons()
    {
        Debug.Log($"Cleaning up {activeIcons.Count} active icons");

        foreach (GameObject icon in activeIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        activeIcons.Clear();
    }

    IEnumerator WaitForDialogueThenStartGame()
    {
        yield return new WaitUntil(() => dialogues.dialogueFinished);
        Debug.Log("Dialogue finished, beginning game");
        BeginGame();
    }

    void BeginGame()
    {
        Debug.Log("=== BEGINNING GAME ===");

        // Show all UI elements
        ShowAllGameUI();

        gameStarted = true;

        // Debug array lengths
        Debug.Log($"Renewable icons count: {renewableIcons.Length}");
        Debug.Log($"Fossil icons count: {fossilIcons.Length}");
        Debug.Log($"Renewable names count: {renewableIconNames.Length}");

        // Initialize progress slider
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = 5; // 5 waves total
            progressSlider.value = 0;
            Debug.Log("Progress slider initialized");
        }

        UpdateScore();
        StartNewWave();
    }

    // Show all game UI elements
    void ShowAllGameUI()
    {
        Debug.Log("Showing all game UI elements");

        Earth.SetActive(true);
        SpawnArea.SetActive(true);
        MainBin.SetActive(true);
        Score.SetActive(true);
        WaveInfo.SetActive(true);
        TargetDisplay.SetActive(true);
        Header.SetActive(true);
        Settings.SetActive(true);
        QuizProgress.SetActive(true);
    }

    void Update()
    {
        if (!gameStarted || isPaused || isQuizActive) return; // Don't update when paused or quiz is active

        HandleBinMovement();
        CheckIconCollisions();
    }

    void HandleBinMovement()
    {
        float move = 0;
        Vector2 newBinPos = binImage.rectTransform.anchoredPosition;

        // Keyboard input (for testing on PC)
        move = Input.GetAxis("Horizontal");

        // Touch input (for mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPos = touch.position;

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                // Convert screen touch to local UI position
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    spawnArea, touchPos, null, out localPos);

                // Move bin directly towards touch X
                newBinPos = new Vector2(localPos.x, newBinPos.y);
            }
        }
        else if (move != 0)
        {
            // Apply keyboard movement
            newBinPos.x += move * binMoveSpeed * Time.deltaTime;
        }

        // Clamp inside spawn area
        if (move != 0 || Input.touchCount > 0)
        {
            float halfWidth = spawnArea.rect.width / 2f;
            float binHalf = binImage.rectTransform.rect.width / 2f;
            newBinPos.x = Mathf.Clamp(newBinPos.x, -halfWidth + binHalf, halfWidth - binHalf);

            binImage.rectTransform.anchoredPosition = newBinPos;

            // Immediately update catch zone position with proper offset
            if (binCatchZone != null)
            {
                binCatchZone.anchoredPosition = newBinPos + catchZoneOffset;
            }
        }
    }

    void CheckIconCollisions()
    {
        if (activeIcons.Count == 0) return;

        // Use catch zone if available, otherwise fall back to bin
        Vector2 catchPos;
        float catchWidth;

        if (binCatchZone != null)
        {
            catchPos = binCatchZone.anchoredPosition;
            catchWidth = binCatchZone.rect.width;
        }
        else
        {
            catchPos = binImage.rectTransform.anchoredPosition;
            catchWidth = binImage.rectTransform.rect.width;
            Debug.LogWarning("No catch zone assigned! Using bin image instead.");
        }

        // Check all active icons for collision with catch zone
        for (int i = activeIcons.Count - 1; i >= 0; i--)
        {
            if (activeIcons[i] == null)
            {
                activeIcons.RemoveAt(i);
                continue;
            }

            RectTransform iconRect = activeIcons[i].GetComponent<RectTransform>();
            if (iconRect == null)
            {
                Debug.LogError($"Icon at index {i} has no RectTransform!");
                activeIcons.RemoveAt(i);
                continue;
            }

            Vector2 iconPos = iconRect.anchoredPosition;

            // More lenient vertical catch range and better horizontal detection
            float catchY = catchPos.y;
            float verticalTolerance = 30f; // Increased tolerance for easier catching
            bool isAtCatchLevel = iconPos.y <= catchY + verticalTolerance && iconPos.y >= catchY - verticalTolerance;

            if (isAtCatchLevel)
            {
                // More generous horizontal catch range
                float horizontalDistance = Mathf.Abs(iconPos.x - catchPos.x);
                float horizontalTolerance = (catchWidth / 2) + 20f; // Added extra margin
                bool isInHorizontalRange = horizontalDistance <= horizontalTolerance;

                if (isInHorizontalRange)
                {
                    // Get the icon component to check its type
                    IconType iconType = activeIcons[i].GetComponent<IconType>();
                    if (iconType != null)
                    {
                        Debug.Log($"COLLISION DETECTED! Icon type: {(iconType.isRenewable ? "Renewable" : "Fossil")}, Index: {iconType.iconIndex}");
                        ProcessIconCatch(iconType, i);
                        return; // Exit early after processing one catch to avoid multiple detections
                    }
                    else
                    {
                        Debug.LogError("Icon has no IconType component!");
                        activeIcons.RemoveAt(i);
                    }
                }
            }
        }
    }

    void ProcessIconCatch(IconType iconType, int iconListIndex)
    {
        Debug.Log($"=== PROCESSING ICON CATCH ===");
        Debug.Log($"Icon isRenewable: {iconType.isRenewable}");
        Debug.Log($"Icon index: {iconType.iconIndex}");
        Debug.Log($"Target index: {targetIconIndex}");
        Debug.Log($"Current wave: {currentWave}");

        bool isCorrectCatch = false;

        // More precise matching logic
        if (iconType.isRenewable && iconType.iconIndex == targetIconIndex)
        {
            // Correct! Caught the target renewable icon
            Debug.Log("✓ CORRECT CATCH!");
            isCorrectCatch = true;
            CorrectAnswer();
            targetIconsCaughtThisWave++;
            StartCoroutine(ShowCorrectFeedback());
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(correct);
        }
        else
        {
            // Wrong catch - trigger quiz as life saver
            Debug.Log("✗ WRONG CATCH - STARTING LIFE SAVER QUIZ!");

            if (iconType.isRenewable && iconType.iconIndex != targetIconIndex)
            {
                Debug.Log("Wrong renewable type caught - Quiz can save you!");
                StartCoroutine(ShowWrongTypeFeedback());
            }
            else
            {
                Debug.Log("Fossil fuel caught - Quiz can save you!");
                StartCoroutine(ShowFossilFeedback());
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(wrong);

            // Start quiz as life saver - no immediate penalty
            StartLifeSaverQuiz();
        }

        // Remove the caught icon safely
        if (iconListIndex >= 0 && iconListIndex < activeIcons.Count)
        {
            GameObject iconToDestroy = activeIcons[iconListIndex];
            activeIcons.RemoveAt(iconListIndex);

            if (iconToDestroy != null)
            {
                Destroy(iconToDestroy);
            }

            Debug.Log($"Icon removed. Remaining active icons: {activeIcons.Count}");
        }

        // Only check wave completion if it was a correct catch
        if (isCorrectCatch)
        {
            CheckWaveCompletion();
        }
    }

    // Start the life saver quiz
    void StartLifeSaverQuiz()
    {
        Debug.Log("=== STARTING LIFE SAVER QUIZ ===");

        isQuizActive = true;
        isPaused = true;

        // Fetch one random question from DB (Quiz_ID = 11)
        JumbledQuestion1 selectedQuestion = dbManager.GetOneJumbledQuestion(11);
        correctAnswer = selectedQuestion.CorrectAnswer;

        // Display the question
        if (questionText != null)
            questionText.text = "LIFE SAVER QUIZ!\n" + selectedQuestion.QuestionText;

        // Create jumbled letters
        CreateJumbledLetters(correctAnswer);

        // Show quiz modal
        if (quizModal != null)
            quizModal.SetActive(true);

        // Initialize feedback and buttons
        UpdateFeedbackWithCurrentAnswer();

        if (submitAnswerButton != null)
        {
            submitAnswerButton.gameObject.SetActive(true);
            UpdateSubmitButtonState();
        }

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        Debug.Log($"Life saver quiz started with answer: {correctAnswer}");
    }

    public void ResetLetterButtons()
    {
        Debug.Log("Resetting letter buttons");

        currentAnswer = "";

        foreach (GameObject buttonObj in letterButtons)
        {
            if (buttonObj != null)
            {
                Button button = buttonObj.GetComponent<Button>();
                Image buttonImage = buttonObj.GetComponent<Image>();

                if (button != null)
                    button.interactable = true; // Re-enable all buttons

                if (buttonImage != null)
                    buttonImage.color = defaultLetterColor; // Reset to default color
            }
        }

        // Reset feedback to initial state
        UpdateFeedbackWithCurrentAnswer();

        // Update submit button state
        UpdateSubmitButtonState();
    }

    public void ClearAllSelectedLetters()
    {
        Debug.Log("Clearing all selected letters");

        // Reset all button states without destroying them
        ResetLetterButtons();

        // Show feedback about clearing
        if (feedbackText != null)
        {
            feedbackText.text = "All letters cleared! Select letters to spell the answer.";
            feedbackText.color = Color.yellow;

            // Return to normal feedback after 1 second
            StartCoroutine(ReturnToNormalFeedbackAfterClear());
        }
    }

    IEnumerator ReturnToNormalFeedbackAfterClear()
    {
        yield return new WaitForSeconds(1f);
        UpdateFeedbackWithCurrentAnswer();
    }

    // Create jumbled letters for the quiz
    void CreateJumbledLetters(string answer)
    {
        Debug.Log($"Creating jumbled letters for: {answer}");

        // Clear existing letter buttons
        foreach (GameObject button in letterButtons)
        {
            if (button != null)
                Destroy(button);
        }
        letterButtons.Clear();
        currentAnswer = "";

        // Validate components
        if (letterButtonPrefab == null)
        {
            Debug.LogError("Letter button prefab is not assigned!");
            return;
        }

        if (lettersContainer == null)
        {
            Debug.LogError("Letters container is not assigned!");
            return;
        }

        // Create list of letters from the answer
        List<char> letters = answer.ToList();

        // Add one random distraction letter
        char[] distractionLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        char distractionLetter;
        do
        {
            distractionLetter = distractionLetters[Random.Range(0, distractionLetters.Length)];
        } while (letters.Contains(distractionLetter));

        letters.Add(distractionLetter);
        Debug.Log($"Added distraction letter: {distractionLetter}");

        // Shuffle the letters
        for (int i = 0; i < letters.Count; i++)
        {
            char temp = letters[i];
            int randomIndex = Random.Range(i, letters.Count);
            letters[i] = letters[randomIndex];
            letters[randomIndex] = temp;
        }

        // Create buttons for each letter
        for (int i = 0; i < letters.Count; i++)
        {
            char letter = letters[i];
            GameObject letterButton = Instantiate(letterButtonPrefab, lettersContainer);

            if (letterButton == null) continue;

            // Get components
            Button buttonComponent = letterButton.GetComponent<Button>();
            Text buttonText = letterButton.GetComponentInChildren<Text>();
            Image buttonImage = letterButton.GetComponent<Image>();

            if (buttonComponent == null || buttonText == null)
            {
                Destroy(letterButton);
                continue;
            }

            // Set initial state
            buttonText.text = letter.ToString();
            buttonComponent.interactable = true; // Initially enabled

            if (buttonImage != null)
                buttonImage.color = defaultLetterColor; // Default color

            // Clear any existing listeners
            buttonComponent.onClick.RemoveAllListeners();

            // Add click listener
            char capturedLetter = letter;
            GameObject capturedButton = letterButton;
            buttonComponent.onClick.AddListener(() => OnLetterClicked(capturedLetter, capturedButton));

            letterButtons.Add(letterButton);
            Debug.Log($"Created letter button: '{letter}'");
        }

        Debug.Log($"Successfully created {letterButtons.Count} letter buttons");
        StartCoroutine(RefreshLayoutAfterFrame());
    }

    IEnumerator RefreshLayoutAfterFrame()
    {
        yield return null; // Wait one frame

        // Force layout refresh
        if (lettersContainer != null)
        {
            LayoutGroup layoutGroup = lettersContainer.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(lettersContainer.GetComponent<RectTransform>());
            }
        }
    }

    // Handle letter button clicks
    void OnLetterClicked(char letter, GameObject buttonObject)
    {
        Debug.Log($"Letter clicked: {letter}");

        // Validate button object
        if (buttonObject == null)
        {
            Debug.LogError("Button object is null!");
            return;
        }

        // Get components
        Button button = buttonObject.GetComponent<Button>();
        Image buttonImage = buttonObject.GetComponent<Image>();

        if (button == null)
        {
            Debug.LogError("Button component is null!");
            return;
        }

        // Check current state of the button
        bool isCurrentlySelected = !button.interactable;

        if (isCurrentlySelected)
        {
            // DESELECT: Remove this letter from the answer
            DeselectLetter(letter, buttonObject);
        }
        else
        {
            // SELECT: Add this letter to the answer (if not at max length)
            if (currentAnswer.Length < correctAnswer.Length)
            {
                SelectLetter(letter, buttonObject);
            }
            else
            {
                Debug.Log("Answer already at maximum length, cannot add more letters");
            }
        }

        // Update feedback and submit button
        UpdateFeedbackWithCurrentAnswer();
        UpdateSubmitButtonState();
    }

    void SelectLetter(char letter, GameObject buttonObject)
    {
        Debug.Log($"Selecting letter: {letter}");

        // Add letter to current answer
        currentAnswer += letter;

        // Update button appearance
        Button button = buttonObject.GetComponent<Button>();
        Image buttonImage = buttonObject.GetComponent<Image>();

        if (button != null)
            button.interactable = false; // Disable to show it's selected

        if (buttonImage != null)
            buttonImage.color = Color.cyan; // Cyan color for selected letters

        Debug.Log($"Current answer: '{currentAnswer}' (Length: {currentAnswer.Length})");

        // Auto-submit if answer is complete
        if (currentAnswer.Length >= correctAnswer.Length)
        {
            Debug.Log("Answer complete, auto-submitting...");
            StartCoroutine(DelayedSubmit());
        }
    }

    void DeselectLetter(char letter, GameObject buttonObject)
    {
        Debug.Log($"Deselecting letter: {letter}");

        // Find and remove the LAST occurrence of this letter from the answer
        int lastIndex = currentAnswer.LastIndexOf(letter);
        if (lastIndex != -1)
        {
            // Remove the letter at the found index
            currentAnswer = currentAnswer.Remove(lastIndex, 1);
            Debug.Log($"Removed letter '{letter}' at position {lastIndex}");
        }

        // Update button appearance back to default
        Button button = buttonObject.GetComponent<Button>();
        Image buttonImage = buttonObject.GetComponent<Image>();

        if (button != null)
            button.interactable = true; // Re-enable the button

        if (buttonImage != null)
            buttonImage.color = defaultLetterColor; // Back to default color

        Debug.Log($"Current answer after deselection: '{currentAnswer}' (Length: {currentAnswer.Length})");
    }
    void UpdateSubmitButtonState()
    {
        if (submitAnswerButton != null)
        {
            // Enable submit button if there's at least one letter selected
            submitAnswerButton.interactable = currentAnswer.Length > 0;

            // Change button text based on state
            Text submitButtonText = submitAnswerButton.GetComponentInChildren<Text>();
            if (submitButtonText != null)
            {
                if (currentAnswer.Length >= correctAnswer.Length)
                {
                    submitButtonText.text = "Submit";
                }
                else
                {
                    submitButtonText.text = $"Submit ({currentAnswer.Length}/{correctAnswer.Length})";
                }
            }
        }
    }
    void UpdateFeedbackWithCurrentAnswer()
    {
        if (feedbackText == null) return;

        if (string.IsNullOrEmpty(currentAnswer))
        {
            feedbackText.text = "Select letters to spell the answer!";
            feedbackText.color = Color.yellow;
        }
        else
        {
            // Create styled display with brackets around letters
            string displayText = "";

            // Add clicked letters in brackets
            for (int i = 0; i < currentAnswer.Length; i++)
            {
                displayText += "[" + currentAnswer[i] + "] ";
            }

            // Add empty brackets for remaining letters
            int remainingLetters = correctAnswer.Length - currentAnswer.Length;
            for (int i = 0; i < remainingLetters; i++)
            {
                displayText += "[_] ";
            }

            feedbackText.text = displayText.Trim();

            // Color based on progress
            float progress = (float)currentAnswer.Length / correctAnswer.Length;
            if (progress < 0.5f)
                feedbackText.color = Color.yellow;
            else if (progress < 1f)
                feedbackText.color = Color.cyan;
            else
                feedbackText.color = Color.white;
        }
    }

    IEnumerator DelayedSubmit()
    {
        yield return new WaitForSeconds(0.2f); // Brief delay
        SubmitQuizAnswer();
    }

    // Submit the quiz answer with life saver logic
    void SubmitQuizAnswer()
    {
        Debug.Log($"Submitting life saver answer: '{currentAnswer}' vs correct: '{correctAnswer}'");

        bool isCorrect = currentAnswer.Equals(correctAnswer, System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
        {
            Debug.Log("✓ LIFE SAVER QUIZ CORRECT - LIFE SAVED!");

            // Show success feedback with complete word
            if (feedbackText != null)
            {
                feedbackText.text = $"Correct! '{correctAnswer}' - Life saved! +10 bonus points!";
                feedbackText.color = correctLetterColor;
            }

            // Color correct letters green
            ColorLetterButtons(true);

            // LIFE SAVED - Give bonus points
            score += 10;
            UpdateScore();

            // Optional healing effect
            if (currentEarthState > 0)
            {
                Debug.Log("Life saver success - healing Earth slightly");
                currentEarthState = Mathf.Max(0, currentEarthState - 1);
                if (currentEarthState < earthStates.Length)
                    StartCoroutine(AnimateEarthChange(earthStates[currentEarthState], false));
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(correct);
        }
        else
        {
            Debug.Log("✗ LIFE SAVER QUIZ FAILED - LIFE LOST!");

            // Show failure feedback with both answers
            if (feedbackText != null)
            {
                feedbackText.text = $"Wrong! Your answer: '{currentAnswer}'\nCorrect answer: '{correctAnswer}'\nLife lost!";
                feedbackText.color = wrongLetterColor;
            }

            // Color letters red
            ColorLetterButtons(false);

            // LIFE LOST - Apply penalty
            WrongAnswer();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(wrong);
        }

        // Hide submit button, show continue button
        if (submitAnswerButton != null)
            submitAnswerButton.gameObject.SetActive(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        // Auto-continue after showing feedback
        StartCoroutine(AutoContinueAfterFeedback());
    }

    // Auto-continue after showing feedback
    IEnumerator AutoContinueAfterFeedback()
    {
        yield return new WaitForSeconds(2f); // Show feedback for 2 seconds

        if (continueButton != null && continueButton.gameObject.activeInHierarchy)
        {
            ContinueAfterQuiz(); // Auto-continue
        }
    }

    // Color letter buttons based on result
    void ColorLetterButtons(bool isCorrect)
    {
        Color targetColor = isCorrect ? correctLetterColor : wrongLetterColor;

        foreach (GameObject buttonObj in letterButtons)
        {
            if (buttonObj != null)
            {
                Image buttonImage = buttonObj.GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.color = targetColor;

                // Also disable all buttons after submission
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                    button.interactable = false;
            }
        }
    }
    // Continue after quiz
    void ContinueAfterQuiz()
    {
        Debug.Log("Continuing after life saver quiz...");

        // Hide quiz modal
        if (quizModal != null)
            quizModal.SetActive(false);

        // Clean up any existing icons that might be stuck or paused
        CleanupActiveIcons();

        // Resume game state
        isQuizActive = false;
        isPaused = false;

        // Reset bin position to center (optional - helps with control issues)
        // ResetBinPosition();

        // Spawn fresh icons instead of resuming old ones
        SpawnFreshIconsAfterQuiz();

        Debug.Log("Game resumed with fresh icons after quiz");
    }

    void SpawnFreshIconsAfterQuiz()
    {
        Debug.Log("Spawning fresh icons after quiz completion...");

        // Start a reduced batch of icons (fewer than full wave)
        int iconsToSpawn = Mathf.Min(8, targetIconsPerWave + fossilIconsPerWave); // Reduced count
        int targetIconsToSpawn = Mathf.Min(3, targetIconsPerWave); // Reduced target icons
        int fossilIconsToSpawn = iconsToSpawn - targetIconsToSpawn;

        StartCoroutine(SpawnQuizRecoveryIcons(targetIconsToSpawn, fossilIconsToSpawn));
    }

    IEnumerator SpawnQuizRecoveryIcons(int targetCount, int fossilCount)
    {
        Debug.Log($"Starting quiz recovery spawn: {targetCount} targets, {fossilCount} fossils");

        // Create spawn list
        List<SpawnData> iconsToSpawn = new List<SpawnData>();

        // Add target renewable icons
        if (targetIconIndex < renewableIcons.Length && renewableIcons[targetIconIndex] != null)
        {
            for (int i = 0; i < targetCount; i++)
            {
                iconsToSpawn.Add(new SpawnData
                {
                    prefab = renewableIcons[targetIconIndex],
                    isRenewable = true,
                    iconIndex = targetIconIndex
                });
            }
        }

        // Add fossil fuel icons
        if (fossilIcons.Length > 0)
        {
            for (int i = 0; i < fossilCount; i++)
            {
                int randomFossilIndex = Random.Range(0, fossilIcons.Length);
                if (fossilIcons[randomFossilIndex] != null)
                {
                    iconsToSpawn.Add(new SpawnData
                    {
                        prefab = fossilIcons[randomFossilIndex],
                        isRenewable = false,
                        iconIndex = randomFossilIndex
                    });
                }
            }
        }

        // Shuffle the list
        for (int i = 0; i < iconsToSpawn.Count; i++)
        {
            SpawnData temp = iconsToSpawn[i];
            int randomIndex = Random.Range(i, iconsToSpawn.Count);
            iconsToSpawn[i] = iconsToSpawn[randomIndex];
            iconsToSpawn[randomIndex] = temp;
        }

        // Spawn icons with slightly faster interval for better pacing
        float recoverySpawnInterval = Mathf.Max(0.5f, currentSpawnInterval * 0.8f);

        foreach (SpawnData spawnData in iconsToSpawn)
        {
            if (spawnData.prefab != null)
            {
                SpawnIconWithData(spawnData);
                iconsSpawnedThisWave++; // Keep track for wave completion
            }
            yield return new WaitForSeconds(recoverySpawnInterval);
        }

        Debug.Log("Quiz recovery icons spawned successfully");
    }


    void ResetBinPosition()
    {
        if (binImage != null)
        {
            Vector2 centerPosition = new Vector2(0, binImage.rectTransform.anchoredPosition.y);
            binImage.rectTransform.anchoredPosition = centerPosition;

            // Update catch zone position
            if (binCatchZone != null)
            {
                binCatchZone.anchoredPosition = centerPosition + catchZoneOffset;
            }

            Debug.Log("Bin position reset to center");
        }
    }


    void StartNewWave()
    {
        Debug.Log($"=== STARTING WAVE {currentWave + 1} ===");

        if (currentWave >= 5)
        {
            GameComplete();
            return;
        }

        // Reset wave variables
        iconsSpawnedThisWave = 0;
        targetIconsCaughtThisWave = 0;
        targetIconIndex = currentWave; // Use wave number as target icon index

        // Calculate dynamic difficulty for this wave
        UpdateDynamicDifficulty();

        Debug.Log($"Target icon index: {targetIconIndex}");
        Debug.Log($"Wave {currentWave + 1} - Fall Duration: {currentFallDuration}s, Spawn Interval: {currentSpawnInterval}s");
        if (targetIconIndex < renewableIconNames.Length)
        {
            Debug.Log($"Target icon name: {renewableIconNames[targetIconIndex]}");
        }

        // Update UI
        UpdateWaveDisplay();
        UpdateTargetDisplay();

        // Start spawning icons for this wave
        StartCoroutine(SpawnWaveIcons());
    }

    void UpdateDynamicDifficulty()
    {
        // Progressive falling speed - each wave gets 0.3s faster
        currentFallDuration = Mathf.Max(1.5f, baseFallDuration - (currentWave * 0.3f));

        // Progressive spawn rate - each wave spawns 0.15s faster
        currentSpawnInterval = Mathf.Max(0.3f, baseSpawnInterval - (currentWave * 0.15f));

        Debug.Log($"Updated difficulty - Fall: {currentFallDuration}s, Spawn: {currentSpawnInterval}s");
    }

    IEnumerator SpawnWaveIcons()
    {
        Debug.Log("Starting to spawn wave icons...");
        isSpawning = true;

        // Create a list of spawn data instead of GameObjects
        List<SpawnData> iconsToSpawn = new List<SpawnData>();

        // Add target renewable icons
        if (targetIconIndex < renewableIcons.Length && renewableIcons[targetIconIndex] != null)
        {
            for (int i = 0; i < targetIconsPerWave; i++)
            {
                iconsToSpawn.Add(new SpawnData
                {
                    prefab = renewableIcons[targetIconIndex],
                    isRenewable = true,
                    iconIndex = targetIconIndex
                });
            }
            Debug.Log($"Added {targetIconsPerWave} target renewable icons");
        }
        else
        {
            Debug.LogError($"Target renewable icon at index {targetIconIndex} is null or out of bounds!");
        }

        // Add random fossil fuel icons
        if (fossilIcons.Length > 0)
        {
            for (int i = 0; i < fossilIconsPerWave; i++)
            {
                int randomFossilIndex = Random.Range(0, fossilIcons.Length);
                if (fossilIcons[randomFossilIndex] != null)
                {
                    iconsToSpawn.Add(new SpawnData
                    {
                        prefab = fossilIcons[randomFossilIndex],
                        isRenewable = false,
                        iconIndex = randomFossilIndex
                    });
                }
            }
            Debug.Log($"Added {fossilIconsPerWave} fossil fuel icons");
        }
        else
        {
            Debug.LogError("No fossil icons available!");
        }

        Debug.Log($"Total icons to spawn: {iconsToSpawn.Count}");

        // Shuffle the list for random spawn order
        for (int i = 0; i < iconsToSpawn.Count; i++)
        {
            SpawnData temp = iconsToSpawn[i];
            int randomIndex = Random.Range(i, iconsToSpawn.Count);
            iconsToSpawn[i] = iconsToSpawn[randomIndex];
            iconsToSpawn[randomIndex] = temp;
        }

        // Spawn icons with intervals using current dynamic spawn interval
        foreach (SpawnData spawnData in iconsToSpawn)
        {
            // Wait if quiz is active
            while (isQuizActive)
            {
                yield return null;
            }

            if (spawnData.prefab != null)
            {
                SpawnIconWithData(spawnData);
                iconsSpawnedThisWave++;
                Debug.Log($"Spawned icon {iconsSpawnedThisWave}/{iconsToSpawn.Count}");
            }
            yield return new WaitForSeconds(currentSpawnInterval);
        }

        Debug.Log("Finished spawning all icons for this wave");
        isSpawning = false;
    }

    // Spawn icon with explicit data
    void SpawnIconWithData(SpawnData spawnData)
    {
        Debug.Log($"Spawning icon: {spawnData.prefab.name} - Renewable: {spawnData.isRenewable}, Index: {spawnData.iconIndex}");
        GameObject newIcon = Instantiate(spawnData.prefab, spawnArea);

        // Get or add IconType component
        IconType iconType = newIcon.GetComponent<IconType>();
        if (iconType == null)
        {
            iconType = newIcon.AddComponent<IconType>();
            Debug.Log("Added IconType component to spawned icon");
        }

        // Set the icon properties directly from spawn data
        iconType.isRenewable = spawnData.isRenewable;
        iconType.iconIndex = spawnData.iconIndex;

        // Add gravity and wind effects
        AddChallengeEffects(iconType);

        Debug.Log($"Icon configured - Renewable: {iconType.isRenewable}, Index: {iconType.iconIndex}, Gravity: {iconType.gravityMultiplier}, Wind: {iconType.windStrength}");

        // Position at random X, top of screen
        float randomX = Random.Range(-spawnArea.rect.width / 2 + 50f, spawnArea.rect.width / 2 - 50f);
        RectTransform iconRect = newIcon.GetComponent<RectTransform>();

        if (iconRect == null)
        {
            Debug.LogError("Spawned icon has no RectTransform component!");
            Destroy(newIcon);
            return;
        }

        iconRect.anchoredPosition = new Vector2(randomX, spawnArea.rect.height / 2);
        Debug.Log($"Icon positioned at: {iconRect.anchoredPosition}");

        // Add to active icons list
        activeIcons.Add(newIcon);
        Debug.Log($"Total active icons: {activeIcons.Count}");

        // Start falling animation with challenge effects
        StartCoroutine(AnimateIconFallingWithEffects(iconRect, newIcon, iconType));
    }

    void AddChallengeEffects(IconType iconType)
    {
        // Apply wind effects only on waves 2-4 (currentWave 1-3 since it's 0-indexed)
        if (currentWave >= 1 && currentWave <= 3)
        {
            // Random wind strength and direction
            iconType.windStrength = Random.Range(-maxWindStrength, maxWindStrength);
        }
        else
        {
            iconType.windStrength = 0f;
        }

        // Apply random gravity multiplier to all waves
        iconType.gravityMultiplier = Random.Range(minGravityMultiplier, maxGravityMultiplier);

        // Add zigzag movement to fossil fuel icons
        if (!iconType.isRenewable)
        {
            iconType.hasZigzag = true;
            iconType.zigzagAmplitude = Random.Range(30f, 80f); // Horizontal zigzag distance
            iconType.zigzagFrequency = Random.Range(2f, 4f);   // Zigzag speed
        }
        else
        {
            iconType.hasZigzag = false;
        }

        // Apply visual indicators for gravity
        Image iconImage = iconType.GetComponent<Image>();
        if (iconImage != null)
        {
            if (iconType.gravityMultiplier > 1.2f)
            {
                // Fast falling - red tint
                iconImage.color = new Color(1f, 0.8f, 0.8f, 1f);
            }
            else if (iconType.gravityMultiplier < 0.8f)
            {
                // Slow falling - blue tint
                iconImage.color = new Color(0.8f, 0.8f, 1f, 1f);
            }
            else
            {
                // Normal speed - no tint
                iconImage.color = Color.white;
            }
        }
    }

    IEnumerator AnimateIconFallingWithEffects(RectTransform iconRect, GameObject iconObj, IconType iconType)
    {
        float startY = spawnArea.rect.height / 2;
        // Calculate actual fall duration with gravity multiplier
        float actualFallDuration = currentFallDuration / iconType.gravityMultiplier;

        // Better end position calculation for more predictable falling
        float endY = binCatchZone != null ? binCatchZone.anchoredPosition.y - 100f : binImage.rectTransform.anchoredPosition.y - 100f;

        float elapsedTime = 0f;
        Vector2 startPos = iconRect.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, endY);

        // Store original X position for zigzag calculation
        float originalX = startPos.x;

        Debug.Log($"Icon falling from {startPos.y} to {endPos.y} over {actualFallDuration} seconds with wind {iconType.windStrength}, gravity {iconType.gravityMultiplier}, zigzag: {iconType.hasZigzag}");

        while (elapsedTime < actualFallDuration && iconObj != null && iconRect != null)
        {
            // Pause handling - wait if game is paused or quiz is active
            while ((isPaused || isQuizActive) && iconObj != null)
            {
                yield return null;
            }

            if (iconObj == null || iconRect == null) break;

            float t = elapsedTime / actualFallDuration;
            float easedT = t * t; // Ease-in for gravity effect

            // Calculate base position
            Vector2 currentPos = Vector2.Lerp(startPos, endPos, easedT);

            // Apply wind effect (horizontal drift) - only on waves 2-4
            if (currentWave >= 1 && currentWave <= 3)
            {
                float windOffset = iconType.windStrength * t * Time.deltaTime * 10f; // Gradual wind accumulation
                currentPos.x += windOffset;
            }

            // Apply zigzag movement to fossil fuel icons
            if (iconType.hasZigzag && !iconType.isRenewable)
            {
                float zigzagOffset = Mathf.Sin(t * iconType.zigzagFrequency * Mathf.PI * 2) * iconType.zigzagAmplitude * t;
                currentPos.x = originalX + zigzagOffset;
            }

            // Ensure icon doesn't drift completely off screen
            float maxX = spawnArea.rect.width / 2 - 25f;
            currentPos.x = Mathf.Clamp(currentPos.x, -maxX, maxX);

            iconRect.anchoredPosition = currentPos;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Remove icon if it reached the bottom without being caught
        if (iconObj != null)
        {
            Debug.Log("Icon reached bottom without being caught");
            activeIcons.Remove(iconObj);
            Destroy(iconObj);
            CheckWaveCompletion();
        }
    }

    void CheckWaveCompletion()
    {
        Debug.Log($"Checking wave completion. IsSpawning: {isSpawning}, Active icons: {activeIcons.Count}, Quiz active: {isQuizActive}");

        // Wave is complete when all icons have been spawned, no more icons are active, and no quiz is running
        if (!isSpawning && activeIcons.Count == 0 && !isQuizActive)
        {
            Debug.Log($"Wave {currentWave + 1} completed!");
            currentWave++;
            if (progressSlider != null)
                progressSlider.value = currentWave;

            // Small delay before next wave
            StartCoroutine(WaveTransition());
        }
    }

    IEnumerator WaveTransition()
    {
        Debug.Log("Starting wave transition...");
        yield return new WaitForSeconds(1f);

        if (currentWave < 5)
        {
            StartNewWave();
        }
        else
        {
            GameComplete();
        }
    }

    void UpdateWaveDisplay()
    {
        if (waveText != null)
        {
            waveText.text = "" + (currentWave + 1) + "/5";
            Debug.Log($"Updated wave display: {waveText.text}");
        }
    }

    void UpdateTargetDisplay()
    {
        Debug.Log("Updating target display...");

        if (targetIconImage != null && targetIconIndex < renewableIcons.Length)
        {
            // Get the sprite from the renewable icon prefab
            Image iconImage = renewableIcons[targetIconIndex].GetComponent<Image>();
            if (iconImage != null)
            {
                targetIconImage.sprite = iconImage.sprite;
                Debug.Log($"Updated target icon image with sprite: {iconImage.sprite.name}");
            }
            else
            {
                Debug.LogError($"Renewable icon at index {targetIconIndex} has no Image component!");
            }
        }

        if (targetIconNameText != null && targetIconIndex < renewableIconNames.Length)
        {
            targetIconNameText.text = "" + renewableIconNames[targetIconIndex];
            Debug.Log($"Updated target name text: {targetIconNameText.text}");
        }
    }

    // Feedback coroutines
    IEnumerator ShowCorrectFeedback()
    {
        Debug.Log("Showing correct feedback (green flash)");
        Color originalColor = binImage.color;
        binImage.color = Color.green;
        yield return new WaitForSeconds(0.3f);
        binImage.color = originalColor;
    }

    IEnumerator ShowWrongTypeFeedback()
    {
        Debug.Log("Showing wrong type feedback (yellow flash)");
        Color originalColor = binImage.color;
        binImage.color = Color.yellow; // Yellow for wrong renewable type
        yield return new WaitForSeconds(0.3f);
        binImage.color = originalColor;
    }

    IEnumerator ShowFossilFeedback()
    {
        Debug.Log("Showing fossil feedback (red flash)");
        Color originalColor = binImage.color;
        binImage.color = Color.red; // Red for fossil fuel
        yield return new WaitForSeconds(0.3f);
        binImage.color = originalColor;
    }

    void CorrectAnswer()
    {
        Debug.Log("=== CORRECT ANSWER! ===");
        score += 20; // Higher score for correct catches

        // Heal Earth occasionally for good performance
        if (score % 100 == 0 && currentEarthState > 0)
        {
            Debug.Log("Healing Earth due to good performance");
            currentEarthState--;
            StartCoroutine(AnimateEarthChange(earthStates[currentEarthState], false));
        }

        UpdateScore();
        Debug.Log($"New score: {score}");
    }

    void WrongAnswer()
    {
        Debug.Log("=== WRONG ANSWER! ===");
        // Damage Earth for wrong catches
        currentEarthState++;
        Debug.Log($"Earth state increased to: {currentEarthState}");

        if (currentEarthState < earthStates.Length)
        {
            StartCoroutine(AnimateEarthChange(earthStates[currentEarthState], true));

            // Check if Earth reached maximum pollution (5th sprite, index 4)
            if (currentEarthState >= 4)
            {
                Debug.Log("Earth fully polluted - triggering game over");
                GameOver();
            }
        }
    }

    // Earth animation method
    IEnumerator AnimateEarthChange(Sprite newSprite, bool isWrong)
    {
        if (isWrong)
        {
            float shakeDuration = 0.3f;
            float shakeMagnitude = 10f;
            Vector3 originalPos = earthImage.rectTransform.anchoredPosition;
            Image img = earthImage;
            Sprite oldSprite = img.sprite;

            GameObject overlayObj = new GameObject("EarthOverlay");
            overlayObj.transform.SetParent(img.transform.parent, false);
            Image overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.sprite = oldSprite;
            overlayImg.rectTransform.sizeDelta = img.rectTransform.sizeDelta;
            overlayImg.rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;
            overlayImg.preserveAspect = true;
            overlayImg.raycastTarget = false;

            img.sprite = newSprite;
            img.color = new Color(1, 1, 1, 0);

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeMagnitude;
                float y = Random.Range(-1f, 1f) * shakeMagnitude;
                img.rectTransform.anchoredPosition = originalPos + new Vector3(x, y, 0);
                overlayImg.rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;

                float fadeT = elapsed / shakeDuration;
                overlayImg.color = new Color(1, 1, 1, 1f - fadeT);
                img.color = new Color(1, 1, 1, fadeT);

                elapsed += Time.deltaTime;
                yield return null;
            }

            img.rectTransform.anchoredPosition = originalPos;
            Destroy(overlayObj);
            img.color = Color.white;
        }
        else
        {
            float duration = 0.2f;
            Vector3 originalScale = earthImage.rectTransform.localScale;
            Vector3 enlargedScale = originalScale * 1.2f;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);
                earthImage.rectTransform.localScale = Vector3.Lerp(originalScale, enlargedScale, easedT);
                yield return null;
            }

            earthImage.sprite = newSprite;

            t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                float easedT = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                earthImage.rectTransform.localScale = Vector3.Lerp(enlargedScale, originalScale, easedT);
                yield return null;
            }

            earthImage.rectTransform.localScale = originalScale;
        }
    }

    void UpdateScore()
    {
        scoreText.text = score.ToString();
        Debug.Log($"Score updated to: {score}");
    }

    void GameComplete()
    {
        Debug.Log("=== GAME COMPLETE! ===");

        // Hide all game UI first
        HideAllGameUI();

        // Clean up any remaining icons
        CleanupActiveIcons();

        // Show pass modal after hiding UI
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(passed);
        StartCoroutine(ShowPassModalAfterDelay());
    }

    void GameOver()
    {
        Debug.Log("=== GAME OVER! ===");

        // Hide all game UI first
        HideAllGameUI();

        // Clean up any remaining icons
        CleanupActiveIcons();

        // Show fail modal after hiding UI
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(failed);
        StartCoroutine(ShowFailModalAfterDelay());
    }

    public void RestartGame()
    {
        Debug.Log("=== RESTARTING GAME VIA BUTTON ===");

        // Stop all running coroutines
        StopAllCoroutines();

        // Clean up any remaining icons
        CleanupActiveIcons();

        // Reset all game state
        ResetGameState();

        HideAllGameUI();

        // Hide modals
        if (passModal != null) passModal.SetActive(false);
        if (failModal != null) failModal.SetActive(false);
        if (quizModal != null) quizModal.SetActive(false);

        // Always restart from dialogue when using restart button
        if (dialogues != null)
        {
            Debug.Log("Restarting from dialogue via button");
            // StartDialogue already resets dialogueFinished to false internally
            dialogues.StartDialogue(0);
            StartCoroutine(WaitForDialogueThenStartGame());
        }
        else
        {
            Debug.Log("No dialogue system found, beginning game immediately");
            BeginGame();
        }
    }

    void HideAllGameUI()
    {
        Debug.Log("Hiding all game UI elements");

        Earth.SetActive(false);
        SpawnArea.SetActive(false);
        MainBin.SetActive(false);
        Score.SetActive(false);
        WaveInfo.SetActive(false);
        TargetDisplay.SetActive(false);
        // Keep Header, Settings, and QuizProgress visible for navigation
        // Header.SetActive(false);
        // Settings.SetActive(false);
        // QuizProgress.SetActive(false);
    }

    IEnumerator ShowPassModalAfterDelay()
    {
        dbManager.AddUserItem(userID, rewardItemID);
        dbManager.MarkLessonAsCompleted(userID, 11);
        dbManager.CheckAndUnlockAllLessons(userID);
        lessonHandler.RefreshLessonLocks();
        dbManager.AddCoin(userID, 100);
        dbManager.SaveQuizAndScore(userID, 11, score);
        dbManager.CheckAndUnlockBadges(userID);
        yield return new WaitForSeconds(0.5f); // Small delay for smooth transition

        if (passModal != null)
        {
            passText.text = $"You powered up with clean, renewable energy!\n" +
    $"Final Score: {score}";
            passModal.SetActive(true);
            Debug.Log("Pass modal shown");
        }
        else
        {
            Debug.LogError("Pass modal is not assigned!");
            // Fallback to dialogue if modal not found
            if (dialogues != null)
            {
                dialogues.StartDialogue(2); // Assuming index 2 is for game completion
            }
        }
    }

    IEnumerator ShowFailModalAfterDelay()
    {
        dbManager.AddCoin(userID, 100);
        dbManager.SaveQuizAndScore(userID, 11, score);
        dbManager.CheckAndUnlockBadges(userID);
        yield return new WaitForSeconds(0.5f); // Small delay for smooth transition

        if (failModal != null)
        {
            failText.text = $"Your power ran low.\n" +
$"Final Score: {score}";
            failModal.SetActive(true);
            Debug.Log("Fail modal shown");
        }
        else
        {
            Debug.LogError("Fail modal is not assigned!");
            // Fallback to dialogue if modal not found
            if (dialogues != null)
            {
                dialogues.StartDialogue(1); // Assuming index 1 is for game over
            }
        }
    }

    // Pause and Resume Methods for Settings Modal
    public void PauseGame()
    {
        Debug.Log("=== GAME PAUSED ===");
        isPaused = true;

        // Pause physics by setting timeScale to 0 (optional, for additional effects)
        // Time.timeScale = 0f; // Uncomment if you want to pause all time-based operations
    }

    public void ResumeGame()
    {
        Debug.Log("=== GAME RESUMED ===");
        isPaused = false;

        // Resume physics
        // Time.timeScale = 1f; // Uncomment if you used Time.timeScale = 0 in PauseGame
    }

    // Enhanced IconType component with challenge effects
    [System.Serializable]
    public class IconType : MonoBehaviour
    {
        public bool isRenewable;
        public int iconIndex;

        [Header("Challenge Effects")]
        public float gravityMultiplier = 1f;  // Speed multiplier for falling
        public float windStrength = 0f;       // Horizontal drift strength

        [Header("Zigzag Movement")]
        public bool hasZigzag = false;        // Whether this icon moves in zigzag
        public float zigzagAmplitude = 50f;   // How wide the zigzag movement is
        public float zigzagFrequency = 3f;    // How fast the zigzag oscillates
    }

    // Helper class for spawn data
    [System.Serializable]
    public class SpawnData
    {
        public GameObject prefab;
        public bool isRenewable;
        public int iconIndex;
    }

    // Quiz question data structure
    [System.Serializable]
    public class QuizQuestion
    {
        public string question;
        public string answer;

        public QuizQuestion(string q, string a)
        {
            question = q;
            answer = a;
        }
    }
}
