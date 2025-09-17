using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RocketAsteroidGame : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider fuelMeter;
    public Slider lifeMeter;
    public Slider progressMeter;
    public Text scoreText;
    public Text waveText;

    [Header("Player Rocket")]
    public RectTransform rocket;
    public float rocketMoveSpeed = 600f;
    public RectTransform rocketCatchZone;

    [Header("NEW: Rocket Sprites for Different Life States")]
    [Tooltip("Rocket sprite when life is full/high (75-100%)")]
    public Sprite rocketHealthySprite;
    [Tooltip("Rocket sprite when life is medium (50-75%)")]
    public Sprite rocketDamagedSprite;
    [Tooltip("Rocket sprite when life is low (25-50%)")]
    public Sprite rocketBadlyDamagedSprite;
    [Tooltip("Rocket sprite when life is critical (0-25%)")]
    public Sprite rocketCriticalSprite;

    public Image rocketImage;

    [Header("Game Area")]
    public RectTransform gameArea;
    public RectTransform shootingArea;

    [Header("Prefabs")]
    public GameObject asteroidPrefab;
    public GameObject[] devicePrefabs;
    public GameObject bulletPrefab;

    [Header("Shooting")]
    public RectTransform firePoint;
    public float bulletSpeed = 800f;
    public float fireRate = 0.3f;
    private float nextFireTime = 0f;
    private List<GameObject> activeBullets = new List<GameObject>();

    [Header("Game Settings")]
    public float fallDuration = 4f;
    public float spawnInterval = 1.5f;
    public int asteroidMinHealth = 2;
    public int asteroidMaxHealth = 3;
    public int targetScore = 100;

    [Header("NEW: Animation Settings")]
    [Tooltip("Animation curves for smooth transitions")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Falling animation curve for objects")]
    public AnimationCurve fallCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f, 2f, 2f));
    [Tooltip("Enable smooth rocket movement with momentum")]
    public bool enableSmoothRocketMovement = true;
    [Tooltip("Rocket movement acceleration")]
    public float rocketAcceleration = 1200f;
    [Tooltip("Rocket movement drag")]
    public float rocketDrag = 8f;
    [Tooltip("Rocket tilt angle when moving")]
    public float rocketTiltAngle = 15f;
    [Tooltip("Enable rotating asteroids")]
    public bool enableRotatingAsteroids = true;
    [Tooltip("Asteroid rotation speed range")]
    public Vector2 asteroidRotationSpeed = new Vector2(30f, 120f);

    [Header("NEW: Fuel System")]
    [Tooltip("Maximum fuel capacity")]
    public float maxFuel = 100f;
    [Tooltip("How much fuel decreases per second during gameplay (calculated to last exactly 60 seconds)")]
    public float fuelConsumptionRate = 3.3333f;
    [Tooltip("How many asteroids need to be destroyed to refill fuel")]
    public int asteroidsForFuelRefill = 5;
    [Tooltip("How many devices need to be caught to refill fuel")]
    public int devicesForFuelRefill = 3;
    [Tooltip("Amount of fuel restored when refill conditions are met")]
    public float fuelRefillAmount = 15f;

    [Header("NEW: Life System")]
    [Tooltip("Maximum life capacity")]
    public float maxLife = 100f;
    [Tooltip("Life lost when answering question incorrectly after asteroid hit")]
    public float lifeLossOnWrongAnswer = 20f;

    [Header("Configurable Scoring")]
    [Tooltip("Points gained for destroying an asteroid")]
    public int asteroidScoreValue = 2;
    [Tooltip("Points gained for catching a device")]
    public int deviceScoreValue = 3;

    [Header("Asteroid Movement")]
    [Tooltip("Enable zigzag movement for asteroids")]
    public bool enableZigzagMovement = true;
    [Tooltip("Amplitude of zigzag movement")]
    public float zigzagAmplitude = 100f;
    [Tooltip("Speed of zigzag oscillation")]
    public float zigzagSpeed = 2f;
    [Tooltip("Enable asteroids to follow player")]
    public bool enablePlayerFollowing = true;
    [Tooltip("How strongly asteroids are attracted to player (0-1)")]
    [Range(0f, 1f)]
    public float followStrength = 0.3f;

    [Header("Animation Settings")]
    [Tooltip("Duration of device catch animation")]
    public float deviceCatchAnimDuration = 0.5f;
    [Tooltip("Scale factor for device catch animation")]
    public float deviceCatchScaleMultiplier = 1.5f;
    [Tooltip("Duration of asteroid hit animation")]
    public float asteroidHitAnimDuration = 0.8f;
    [Tooltip("Shake intensity for asteroid hit")]
    public float shakeIntensity = 20f;
    [Tooltip("Number of flashes during invulnerability")]
    public int invulnerabilityFlashes = 6;

    [Header("Question System")]
    public GameObject questionPanel;
    public Text questionText;
    public Button[] answerButtons;
    public Text[] answerTexts;
    public Image[] answerButtonImages;
    [Tooltip("Points gained for correct answer after asteroid hit")]
    public int correctAnswerPoints = 15;
    [Tooltip("Points lost for wrong answer (life is also lost)")]
    public int wrongAnswerPenalty = -5;

    [Header("Dynamic Difficulty System")]
    [Tooltip("How much faster asteroids fall after wrong answers (multiplier)")]
    public float wrongAnswerSpeedMultiplier = 1.4f;
    [Tooltip("How much more frequently asteroids spawn after wrong answers (spawn rate multiplier)")]
    public float wrongAnswerSpawnMultiplier = 0.7f;
    [Tooltip("How much the follow strength increases after wrong answers")]
    public float wrongAnswerFollowIncrease = 0.2f;
    [Tooltip("Maximum difficulty multiplier (prevents impossible gameplay)")]
    public float maxDifficultyMultiplier = 3f;
    [Tooltip("How long the difficulty increase lasts (seconds)")]
    public float difficultyDuration = 15f;
    [Tooltip("Enable punishment for consecutive wrong answers")]
    public bool enableConsecutiveWrongPunishment = true;

    [Header("Pause System")]
    [Tooltip("Pause button in game UI")]
    public Button pauseButton;
    [Tooltip("Settings modal that appears when game is paused")]
    public GameObject settingsModal;
    [Tooltip("Resume button in settings modal")]
    public Button resumeButton;
    [Tooltip("Pause overlay (optional - darkens background)")]
    public GameObject pauseOverlay;
    [Tooltip("Pause indicator text")]
    public Text pauseText;

    [Header("Game State")]
    private float currentFuel = 100f;
    private float currentLife = 100f;
    private int score = 0;
    private int asteroidsDestroyed = 0;
    private int devicesCaught = 0;
    private int asteroidsFuelCounter = 0;
    private int devicesFuelCounter = 0;
    private List<GameObject> activeObjects = new List<GameObject>();
    private bool isGameActive = false;
    private bool isInvulnerable = false;
    private bool isQuestionActive = false;
    private bool isWaitingForQuestionAnswer = false;
    private float invulnerabilityTime = 1.0f;
    private Vector2 originalRocketPosition;

    [Header("UI Panels")]
    public GameObject gameUI;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("Optional Dialogue")]
    public Dialogues dialogues;

    // Enhanced object tracking
    private Dictionary<GameObject, bool> isAsteroidDict = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, int> asteroidHealthDict = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, float> asteroidTimeDict = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, Vector2> asteroidStartPosDict = new Dictionary<GameObject, Vector2>();
    private Dictionary<GameObject, float> asteroidRotationSpeedDict = new Dictionary<GameObject, float>();
    private Vector2 catchZoneOffset;

    // Enhanced rocket movement
    private Vector2 rocketVelocity = Vector2.zero;
    private float currentRocketTilt = 0f;
    private float targetRocketTilt = 0f;

    // Dynamic difficulty variables
    private float currentDifficultyMultiplier = 1f;
    private float difficultyEndTime = 0f;
    private int consecutiveWrongAnswers = 0;
    private bool isDifficultyActive = false;

    // Base values to restore after difficulty period
    private float baseFallDuration;
    private float baseSpawnInterval;
    private float baseFollowStrength;

    // Pause system variables
    private bool isGamePaused = false;
    private bool wasPausedBeforeQuestion = false;
    private float pausedTimeScale = 0f;

    // Animation tracking
    private Dictionary<GameObject, Coroutine> activeAnimations = new Dictionary<GameObject, Coroutine>();

    public DatabaseManager dbManager;
    public int quizId = 12;
    private MultipleChoice.MultipleChoiceQuestions currentQuestion;

    private bool isProcessingQuestion = false;
    private Coroutine currentQuestionCoroutine = null;
    private bool hasBeenInitialized = false;


    [Header("Sound Effects")]
    public AudioClip passed;
    public AudioClip failed;
    public AudioClip correct;
    public AudioClip wrong;
    public AudioClip fuelLow;
    public AudioClip fuelRefill;
    public AudioClip lifeLow;

    [System.Serializable]
    public class GameQuestion
    {
        public string question;
        public string[] answers = new string[4];
        public int correctAnswerIndex;
    }

    public LessonLocker lessonHandler;
    public int userID;
    void Start()
    {
        Debug.Log("=== ROCKET ASTEROID GAME STARTED ===");

        // Store base values for difficulty system
        baseFallDuration = fallDuration;
        baseSpawnInterval = spawnInterval;
        baseFollowStrength = followStrength;

        // Initialize catch zone offset
        if (rocketCatchZone != null && rocket != null)
        {
            catchZoneOffset = rocketCatchZone.anchoredPosition - rocket.anchoredPosition;
        }

        // Store original rocket position for animations
        originalRocketPosition = rocket.anchoredPosition;

        InitializeGame();
        hasBeenInitialized = true;
    }

    void OnEnable()
    {
        // Only restart if this isn't the first time being enabled
        if (hasBeenInitialized)
        {
            Debug.Log("=== GAME OBJECT RE-ENABLED - RESTARTING GAME ===");
            // Use a coroutine to ensure proper timing
            StartCoroutine(RestartGameAfterEnable());
        }
    }

    IEnumerator RestartGameAfterEnable()
    {
        // Wait a frame to ensure all components are ready
        yield return new WaitForEndOfFrame();

        // Reset time scale
        Time.timeScale = 1f;

        // Stop any running coroutines
        StopAllCoroutines();

        // Start this coroutine again since we just stopped all
        StartCoroutine(RestartGameAfterEnable_Internal());
    }

    IEnumerator RestartGameAfterEnable_Internal()
    {
        // Full game reset
        ResetGameToInitialState();

        // Wait another frame to ensure cleanup is complete
        yield return new WaitForEndOfFrame();

        // Re-initialize catch zone offset (might have been lost)
        if (rocketCatchZone != null && rocket != null)
        {
            catchZoneOffset = rocketCatchZone.anchoredPosition - rocket.anchoredPosition;
        }

        // Start the game from the beginning
        if (dialogues != null)
        {
            dialogues.StartDialogue(0);
            StartCoroutine(WaitForDialogueThenStartGame());
        }
        else
        {
            BeginGame();
        }
    }

    void ResetGameToInitialState()
    {
        Debug.Log("Resetting game to initial state...");

        // Reset all game flags
        isGameActive = false;
        isGamePaused = false;
        isInvulnerable = false;
        isQuestionActive = false;
        isWaitingForQuestionAnswer = false;
        isProcessingQuestion = false;
        wasPausedBeforeQuestion = false;

        // Reset game statistics
        score = 0;
        asteroidsDestroyed = 0;
        devicesCaught = 0;
        asteroidsFuelCounter = 0;
        devicesFuelCounter = 0;
        consecutiveWrongAnswers = 0;

        // Reset difficulty
        currentDifficultyMultiplier = 1f;
        isDifficultyActive = false;
        ResetDifficulty();

        // Reset player resources
        currentFuel = maxFuel;
        currentLife = maxLife;

        // Reset rocket movement
        rocketVelocity = Vector2.zero;
        currentRocketTilt = 0f;
        targetRocketTilt = 0f;

        // IMPORTANT: Reset rocket position properly
        if (rocket != null)
        {
            rocket.anchoredPosition = originalRocketPosition;
            rocket.localEulerAngles = Vector3.zero;
            rocket.localScale = Vector3.one;

            // Also reset catch zone position
            if (rocketCatchZone != null)
            {
                rocketCatchZone.anchoredPosition = originalRocketPosition + catchZoneOffset;
            }
        }

        // Reset question state
        currentQuestion = null;
        if (currentQuestionCoroutine != null)
        {
            StopCoroutine(currentQuestionCoroutine);
            currentQuestionCoroutine = null;
        }

        // Clear all active objects and bullets
        CleanupActiveObjects();

        // Reset UI panels
        ResetUIPanels();

        // Update UI elements (without animations to avoid conflicts)
        UpdateAllUIImmediate();

    }

    void CleanupActiveObjects()
    {
        // Clean up active objects
        if (activeObjects != null)
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeObjects[i];
                if (obj != null)
                {
                    RemoveObjectData(obj);
                    Destroy(obj);
                }
            }
            activeObjects.Clear();
        }

        // Clean up bullets
        if (activeBullets != null)
        {
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                GameObject bullet = activeBullets[i];
                if (bullet != null)
                    Destroy(bullet);
            }
            activeBullets.Clear();
        }

        // Clear all dictionaries
        if (isAsteroidDict != null) isAsteroidDict.Clear();
        if (asteroidHealthDict != null) asteroidHealthDict.Clear();
        if (asteroidTimeDict != null) asteroidTimeDict.Clear();
        if (asteroidStartPosDict != null) asteroidStartPosDict.Clear();
        if (asteroidRotationSpeedDict != null) asteroidRotationSpeedDict.Clear();
        if (activeAnimations != null) activeAnimations.Clear();
    }

    void ResetUIPanels()
    {
        // Hide all game panels
        if (gameUI != null) gameUI.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (questionPanel != null) questionPanel.SetActive(false);
        if (settingsModal != null) settingsModal.SetActive(false);
        if (pauseOverlay != null) pauseOverlay.SetActive(false);

        // Reset pause text
        if (pauseText != null)
        {
            pauseText.gameObject.SetActive(false);
        }

        // Reset answer button colors and states
        if (answerButtonImages != null)
        {
            for (int i = 0; i < answerButtonImages.Length; i++)
            {
                if (answerButtonImages[i] != null)
                    answerButtonImages[i].color = Color.white;
            }
        }

        if (answerButtons != null)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                    answerButtons[i].interactable = true;
            }
        }
    }

    void UpdateAllUIImmediate()
    {
        // Update rocket sprite
        UpdateRocketSpriteImmediate();

        // Update all meters immediately
        if (fuelMeter != null)
        {
            fuelMeter.minValue = 0;
            fuelMeter.maxValue = maxFuel;
            fuelMeter.value = currentFuel;
            UpdateFuelMeterColor();
        }

        if (lifeMeter != null)
        {
            lifeMeter.minValue = 0;
            lifeMeter.maxValue = maxLife;
            lifeMeter.value = currentLife;
            UpdateLifeMeterColor();
        }

        if (progressMeter != null)
        {
            progressMeter.minValue = 0;
            progressMeter.maxValue = targetScore;
            progressMeter.value = score;
            UpdateProgressMeterColor();
        }

        // Update score and wave display
        if (scoreText != null)
            scoreText.text = score.ToString();

        UpdateWaveDisplay();
    }

    void UpdateRocketSpriteImmediate()
    {
        if (rocketImage == null) return;

        float lifePercentage = currentLife / maxLife;
        Sprite newSprite = null;

        if (lifePercentage >= 0.75f)
            newSprite = rocketHealthySprite;
        else if (lifePercentage >= 0.5f)
            newSprite = rocketDamagedSprite;
        else if (lifePercentage >= 0.25f)
            newSprite = rocketBadlyDamagedSprite;
        else
            newSprite = rocketCriticalSprite;

        if (newSprite != null)
            rocketImage.sprite = newSprite;
    }

    void InitializeGame()
    {
        // Set up UI panels
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
        questionPanel.SetActive(false);

        InitializePauseSystem();

        // Set up answer button listeners
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int buttonIndex = i;
            answerButtons[i].onClick.RemoveAllListeners(); // Clear existing listeners
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(buttonIndex));
        }

        // Start initial dialogue or game
        if (dialogues != null)
        {
            dialogues.StartDialogue(0);
            StartCoroutine(WaitForDialogueThenStartGame());
        }
        else
        {
            BeginGame();
        }
    }


    void InitializePauseSystem()
    {
        if (settingsModal != null)
            settingsModal.SetActive(false);

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(PauseGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (pauseText != null)
        {
            pauseText.gameObject.SetActive(false);
        }
    }

    public void PauseGame()
    {
        if (!isGameActive || isGamePaused) return;

        Debug.Log("Game Paused");
        isGamePaused = true;
        wasPausedBeforeQuestion = isQuestionActive;

        if (settingsModal != null)
            settingsModal.SetActive(true);

        if (pauseOverlay != null)
            pauseOverlay.SetActive(true);

        if (pauseText != null)
        {
            pauseText.gameObject.SetActive(true);
            pauseText.text = "PAUSED";
        }

        Time.timeScale = 0.1f;
        UpdateWaveDisplay();
    }

    public void ResumeGame()
    {
        if (!isGamePaused) return;

        Debug.Log("Game Resumed");
        isGamePaused = false;

        if (settingsModal != null)
            settingsModal.SetActive(false);

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        if (pauseText != null)
        {
            pauseText.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;
        UpdateWaveDisplay();
    }

    public void TogglePause()
    {
        if (isGamePaused)
            ResumeGame();
        else
            PauseGame();
    }

    IEnumerator WaitForDialogueThenStartGame()
    {
        yield return new WaitUntil(() => dialogues.dialogueFinished);
        BeginGame();
    }

    void BeginGame()
    {
        Debug.Log("=== BEGINNING GAME ===");

        gameUI.SetActive(true);
        isGameActive = true;
        isInvulnerable = false;

        // IMPORTANT: Reset all question states
        ResetQuestionStates();

        isGamePaused = false;
        asteroidsDestroyed = 0;
        devicesCaught = 0;
        asteroidsFuelCounter = 0;
        devicesFuelCounter = 0;

        // Reset rocket movement
        currentRocketTilt = 0f;
        targetRocketTilt = 0f;

        currentFuel = maxFuel;
        currentLife = maxLife;

        ResetDifficulty();

        if (fuelMeter != null)
        {
            fuelMeter.minValue = 0;
            fuelMeter.maxValue = maxFuel;
            fuelMeter.value = currentFuel;
        }

        if (lifeMeter != null)
        {
            lifeMeter.minValue = 0;
            lifeMeter.maxValue = maxLife;
            lifeMeter.value = currentLife;
        }

        if (progressMeter != null)
        {
            progressMeter.minValue = 0;
            progressMeter.maxValue = targetScore;
            progressMeter.value = score;
        }

        // Ensure question panel is hidden
        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
        }

        UpdateRocketSprite();
        UpdateScore();
        UpdateWaveDisplay();
        StartCoroutine(SpawnObjects());
        StartCoroutine(ManageDifficulty());
        StartCoroutine(ManageFuelConsumption());
    }

    IEnumerator ManageFuelConsumption()
    {
        bool lowFuelWarningPlayed = false;

        while (isGameActive)
        {
            if (!isGamePaused && !isQuestionActive)
            {
                currentFuel -= fuelConsumptionRate * Time.deltaTime;
                currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);

                UpdateFuelMeter();

                if (currentFuel <= maxFuel * 0.2f && !lowFuelWarningPlayed)
                {
                    if (fuelLow != null)
                        AudioManager.Instance.PlaySFX(fuelLow);
                    lowFuelWarningPlayed = true;
                    StartCoroutine(LowFuelWarning());
                }

                if (currentFuel > maxFuel * 0.3f)
                {
                    lowFuelWarningPlayed = false;
                }

                if (currentFuel <= 0f)
                {
                    Debug.Log("Game Over: Fuel depleted!");
                    GameOver();
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator LowFuelWarning()
    {
        Image fuelFill = fuelMeter.fillRect?.GetComponent<Image>();
        if (fuelFill == null) yield break;

        Color originalColor = fuelFill.color;

        for (int i = 0; i < 5; i++)
        {
            fuelFill.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            fuelFill.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator LowLifeWarning()
    {
        Image lifeFill = lifeMeter.fillRect?.GetComponent<Image>();
        if (lifeFill == null) yield break;

        Color originalColor = lifeFill.color;

        for (int i = 0; i < 3; i++)
        {
            lifeFill.color = Color.red;
            yield return new WaitForSeconds(0.3f);
            lifeFill.color = originalColor;
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator ManageDifficulty()
    {
        while (isGameActive)
        {
            if (!isGamePaused && isDifficultyActive && Time.time >= difficultyEndTime)
            {
                ResetDifficulty();
                Debug.Log("Difficulty reset to normal");
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    void ApplyDifficultyIncrease()
    {
        consecutiveWrongAnswers++;

        float additionalMultiplier = enableConsecutiveWrongPunishment ?
            1f + (consecutiveWrongAnswers - 1) * 0.3f : 1f;

        currentDifficultyMultiplier = Mathf.Min(
            wrongAnswerSpeedMultiplier * additionalMultiplier,
            maxDifficultyMultiplier
        );

        fallDuration = baseFallDuration / currentDifficultyMultiplier;
        spawnInterval = baseSpawnInterval * wrongAnswerSpawnMultiplier / currentDifficultyMultiplier;

        if (enablePlayerFollowing)
        {
            followStrength = Mathf.Min(baseFollowStrength + wrongAnswerFollowIncrease * consecutiveWrongAnswers, 1f);
        }

        isDifficultyActive = true;
        difficultyEndTime = Time.time + difficultyDuration;

        Debug.Log($"Difficulty increased! Multiplier: {currentDifficultyMultiplier:F2}, Consecutive wrong: {consecutiveWrongAnswers}");
        StartCoroutine(ShowDifficultyIncreaseEffect());
    }

    void ResetDifficulty()
    {
        currentDifficultyMultiplier = 1f;
        fallDuration = baseFallDuration;
        spawnInterval = baseSpawnInterval;
        followStrength = baseFollowStrength;
        isDifficultyActive = false;
    }

    IEnumerator ShowDifficultyIncreaseEffect()
    {
        Image gameAreaImage = gameArea.GetComponent<Image>();
        if (gameAreaImage != null)
        {
            Color originalColor = gameAreaImage.color;

            for (int i = 0; i < 3; i++)
            {
                gameAreaImage.color = new Color(1f, 0f, 0f, 0.4f);
                yield return new WaitForSeconds(0.2f);
                gameAreaImage.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void Update()
    {
        if (!isGameActive || isGamePaused) return;

        // Debug question state periodically
        if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
        {
            Debug.Log($"Game State - isQuestionActive: {isQuestionActive}, isWaitingForQuestionAnswer: {isWaitingForQuestionAnswer}, questionPanel active: {questionPanel != null && questionPanel.activeSelf}");
        }

        if (isQuestionActive && !wasPausedBeforeQuestion) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
            return;
        }

        HandleRocketMovement();
        HandleShooting();
        CheckCollisions();
        UpdateBullets();
    }
    void HandleRocketMovement()
    {
        float move = Input.GetAxis("Horizontal");
        Vector2 newRocketPos = rocket.anchoredPosition;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPos = touch.position;

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    gameArea, touchPos, null, out localPos);

                // Direct movement - no smooth acceleration
                newRocketPos = new Vector2(localPos.x, newRocketPos.y);
            }
        }
        else if (move != 0)
        {
            // Direct keyboard movement
            newRocketPos.x += move * rocketMoveSpeed * Time.deltaTime;

            // Optional: Keep tilt for keyboard input only
            if (enableSmoothRocketMovement)
            {
                targetRocketTilt = -move * rocketTiltAngle;
                currentRocketTilt = Mathf.LerpAngle(currentRocketTilt, targetRocketTilt, Time.deltaTime * 5f);

                Vector3 currentRotation = rocket.localEulerAngles;
                currentRotation.z = currentRocketTilt;
                rocket.localEulerAngles = currentRotation;
            }
        }
        else if (enableSmoothRocketMovement)
        {
            // Reset tilt when no input
            targetRocketTilt = 0f;
            currentRocketTilt = Mathf.LerpAngle(currentRocketTilt, targetRocketTilt, Time.deltaTime * 5f);

            Vector3 currentRotation = rocket.localEulerAngles;
            currentRotation.z = currentRocketTilt;
            rocket.localEulerAngles = currentRotation;
        }

        // Apply position changes and clamp
        if (move != 0 || Input.touchCount > 0)
        {
            float halfWidth = gameArea.rect.width / 2f;
            float rocketHalf = rocket.rect.width / 2f;
            newRocketPos.x = Mathf.Clamp(newRocketPos.x, -halfWidth + rocketHalf, halfWidth - rocketHalf);

            rocket.anchoredPosition = newRocketPos;

            if (rocketCatchZone != null)
            {
                rocketCatchZone.anchoredPosition = newRocketPos + catchZoneOffset;
            }
        }
    }

    void HandleShooting()
    {
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || shootingArea == null) return;

        GameObject bullet = Instantiate(bulletPrefab, shootingArea);
        if (bullet == null) return;

        RectTransform bulletRect = bullet.GetComponent<RectTransform>();
        if (bulletRect == null)
        {
            Destroy(bullet);
            return;
        }

        Image bulletImage = bullet.GetComponent<Image>();
        if (bulletImage != null)
        {
            bulletImage.color = Color.white;
            bulletRect.sizeDelta = new Vector2(20f, 40f);
        }

        Vector2 spawnPosition;
        if (firePoint != null)
        {
            Vector2 rocketPos = rocket.anchoredPosition;
            Vector2 firePointOffset = firePoint.anchoredPosition;
            spawnPosition = rocketPos + firePointOffset;
        }
        else
        {
            Vector2 rocketPos = rocket.anchoredPosition;
            spawnPosition = new Vector2(rocketPos.x, rocketPos.y + rocket.rect.height / 2 + 20f);
        }

        bulletRect.anchoredPosition = spawnPosition;

        // NEW: Animate bullet spawn
        bulletRect.localScale = Vector3.zero;
        StartCoroutine(AnimateBulletSpawn(bulletRect));

        bullet.SetActive(true);

        activeBullets.Add(bullet);
        StartCoroutine(MoveBullet(bulletRect, bullet));
    }

    // NEW: Smooth bullet spawn animation
    IEnumerator AnimateBulletSpawn(RectTransform bulletRect)
    {
        if (bulletRect == null) yield break;

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration && bulletRect != null)
        {
            float t = elapsed / duration;
            float scale = easeCurve.Evaluate(t);
            bulletRect.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (bulletRect != null)
            bulletRect.localScale = Vector3.one;
    }

    IEnumerator MoveBullet(RectTransform bulletRect, GameObject bulletObj)
    {
        if (bulletRect == null || bulletObj == null) yield break;

        float timeAlive = 0f;
        while (bulletObj != null && bulletRect != null && timeAlive < 5f)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            Vector2 currentPos = bulletRect.anchoredPosition;
            currentPos.y += bulletSpeed * Time.deltaTime;
            bulletRect.anchoredPosition = currentPos;
            timeAlive += Time.deltaTime;

            if (currentPos.y > gameArea.rect.height / 2 + 100f) break;
            yield return null;
        }

        if (bulletObj != null)
        {
            activeBullets.Remove(bulletObj);
            Destroy(bulletObj);
        }
    }

    void UpdateBullets()
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            if (activeBullets[i] == null)
            {
                activeBullets.RemoveAt(i);
            }
        }
    }

    IEnumerator SpawnObjects()
    {
        while (isGameActive)
        {
            if (isGamePaused || (isQuestionActive && !wasPausedBeforeQuestion))
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            SpawnSingleObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnSingleObject()
    {
        bool spawnAsteroid = Random.value < 0.6f;
        GameObject prefabToSpawn;

        if (spawnAsteroid && asteroidPrefab != null)
        {
            prefabToSpawn = asteroidPrefab;
        }
        else if (devicePrefabs.Length > 0)
        {
            int randomDeviceIndex = Random.Range(0, devicePrefabs.Length);
            prefabToSpawn = devicePrefabs[randomDeviceIndex];
        }
        else
        {
            return;
        }

        SpawnObject(prefabToSpawn, spawnAsteroid);
    }

    void SpawnObject(GameObject prefab, bool isAsteroid)
    {
        GameObject newObject = Instantiate(prefab, gameArea);

        isAsteroidDict[newObject] = isAsteroid;
        if (isAsteroid)
        {
            int randomHealth = Random.Range(asteroidMinHealth, asteroidMaxHealth + 1);
            asteroidHealthDict[newObject] = randomHealth;
            asteroidTimeDict[newObject] = 0f;

            // NEW: Add rotation speed for asteroids
            if (enableRotatingAsteroids)
            {
                float rotSpeed = Random.Range(asteroidRotationSpeed.x, asteroidRotationSpeed.y);
                if (Random.value < 0.5f) rotSpeed = -rotSpeed; // Random direction
                asteroidRotationSpeedDict[newObject] = rotSpeed;
            }

            Debug.Log($"Spawned asteroid with {randomHealth} health");
        }

        RectTransform objectRect = newObject.GetComponent<RectTransform>();
        float spawnX = Random.Range(-gameArea.rect.width / 2 + 50f, gameArea.rect.width / 2 - 50f);
        Vector2 startPos = new Vector2(spawnX, gameArea.rect.height / 2);
        objectRect.anchoredPosition = startPos;

        if (isAsteroid)
        {
            asteroidStartPosDict[newObject] = startPos;
        }

        activeObjects.Add(newObject);

        // NEW: Smooth spawn animation
        StartCoroutine(AnimateObjectSpawn(objectRect, newObject, isAsteroid));
    }

    // NEW: Smooth object spawn animation
    IEnumerator AnimateObjectSpawn(RectTransform objectRect, GameObject objectObj, bool isAsteroid)
    {
        if (objectRect == null || objectObj == null) yield break;

        // Start small and grow
        Vector3 targetScale = objectRect.localScale;
        objectRect.localScale = Vector3.zero;

        float growDuration = 0.3f;
        float elapsed = 0f;

        while (elapsed < growDuration && objectObj != null && objectRect != null)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsed / growDuration;
            float scale = easeCurve.Evaluate(t);
            objectRect.localScale = targetScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (objectRect != null)
            objectRect.localScale = targetScale;

        // Start the falling animation
        StartCoroutine(AnimateObjectFalling(objectRect, objectObj));
    }

    IEnumerator AnimateObjectFalling(RectTransform objectRect, GameObject objectObj)
    {
        float startY = gameArea.rect.height / 2;
        float endY = -gameArea.rect.height / 2 - 50f;
        Vector2 startPos = objectRect.anchoredPosition;
        float elapsedTime = 0f;
        bool isAsteroid = isAsteroidDict.ContainsKey(objectObj) ? isAsteroidDict[objectObj] : false;

        float currentFallDuration = fallDuration;

        while (elapsedTime < currentFallDuration && objectObj != null && objectRect != null)
        {
            if (isGamePaused || (isQuestionActive && !wasPausedBeforeQuestion))
            {
                yield return null;
                continue;
            }

            // NEW: Use animation curve for smoother falling
            float t = elapsedTime / currentFallDuration;
            float curveT = fallCurve.Evaluate(t);
            float currentY = Mathf.Lerp(startY, endY, curveT);
            float currentX = startPos.x;

            if (isAsteroid && asteroidTimeDict.ContainsKey(objectObj))
            {
                asteroidTimeDict[objectObj] = elapsedTime;

                if (enableZigzagMovement)
                {
                    float zigzagIntensity = isDifficultyActive ? zigzagAmplitude * 1.3f : zigzagAmplitude;
                    float zigzagFreq = isDifficultyActive ? zigzagSpeed * 1.5f : zigzagSpeed;
                    float zigzagOffset = Mathf.Sin(elapsedTime * zigzagFreq) * zigzagIntensity * t;
                    currentX += zigzagOffset;
                }

                if (enablePlayerFollowing && rocket != null)
                {
                    float rocketX = rocket.anchoredPosition.x;
                    float currentFollowStrength = followStrength;
                    float directionToPlayer = (rocketX - currentX) * currentFollowStrength * t;
                    currentX += directionToPlayer * Time.deltaTime;
                }

                // NEW: Rotate asteroids
                if (enableRotatingAsteroids && asteroidRotationSpeedDict.ContainsKey(objectObj))
                {
                    float rotSpeed = asteroidRotationSpeedDict[objectObj];
                    Vector3 currentRotation = objectRect.localEulerAngles;
                    currentRotation.z += rotSpeed * Time.deltaTime;
                    objectRect.localEulerAngles = currentRotation;
                }

                float halfWidth = gameArea.rect.width / 2f - 50f;
                currentX = Mathf.Clamp(currentX, -halfWidth, halfWidth);
            }

            Vector2 newPosition = new Vector2(currentX, currentY);
            objectRect.anchoredPosition = newPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (objectObj != null)
        {
            activeObjects.Remove(objectObj);
            RemoveObjectData(objectObj);
            Destroy(objectObj);
        }
    }

    void RemoveObjectData(GameObject obj)
    {
        if (isAsteroidDict.ContainsKey(obj))
            isAsteroidDict.Remove(obj);
        if (asteroidHealthDict.ContainsKey(obj))
            asteroidHealthDict.Remove(obj);
        if (asteroidTimeDict.ContainsKey(obj))
            asteroidTimeDict.Remove(obj);
        if (asteroidStartPosDict.ContainsKey(obj))
            asteroidStartPosDict.Remove(obj);
        if (asteroidRotationSpeedDict.ContainsKey(obj))
            asteroidRotationSpeedDict.Remove(obj);
        if (activeAnimations.ContainsKey(obj))
        {
            if (activeAnimations[obj] != null)
                StopCoroutine(activeAnimations[obj]);
            activeAnimations.Remove(obj);
        }
    }

    void CheckCollisions()
    {
        if (activeObjects.Count == 0) return;

        CheckRocketCollisions();
        CheckBulletCollisions();
    }

    void CheckRocketCollisions()
    {
        if (rocket == null) return;

        Vector2 rocketPos = rocket.anchoredPosition;
        Vector2 catchPos = rocketCatchZone != null ? rocketCatchZone.anchoredPosition : rocketPos;
        float catchRadius = rocketCatchZone != null ? rocketCatchZone.rect.width / 2 : rocket.rect.width / 2;

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];

            // Enhanced null checks
            if (obj == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            // Check if object still exists and has required components
            if (!isAsteroidDict.ContainsKey(obj))
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            RectTransform objectRect = obj.GetComponent<RectTransform>();
            if (objectRect == null)
            {
                activeObjects.RemoveAt(i);
                RemoveObjectData(obj);
                continue;
            }

            Vector2 objectPos = objectRect.anchoredPosition;
            float distance = Vector2.Distance(catchPos, objectPos);

            if (distance <= catchRadius + 40f)
            {
                bool isAsteroid = isAsteroidDict[obj];

                if (isAsteroid)
                {
                    StartCoroutine(ProcessAsteroidHit(obj));
                }
                else
                {
                    StartCoroutine(ProcessDeviceCatch(obj));
                }

                activeObjects.RemoveAt(i);
                break;
            }
        }
    }

    void CheckBulletCollisions()
    {
        for (int b = activeBullets.Count - 1; b >= 0; b--)
        {
            GameObject bullet = activeBullets[b];
            if (bullet == null)
            {
                activeBullets.RemoveAt(b);
                continue;
            }

            RectTransform bulletRect = bullet.GetComponent<RectTransform>();
            if (bulletRect == null)
            {
                activeBullets.RemoveAt(b);
                if (bullet != null) Destroy(bullet);
                continue;
            }

            Vector2 bulletPos = bulletRect.anchoredPosition;

            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeObjects[i];

                // Enhanced null checks
                if (obj == null)
                {
                    activeObjects.RemoveAt(i);
                    continue;
                }

                if (!isAsteroidDict.ContainsKey(obj))
                {
                    activeObjects.RemoveAt(i);
                    continue;
                }

                RectTransform objectRect = obj.GetComponent<RectTransform>();
                if (objectRect == null)
                {
                    activeObjects.RemoveAt(i);
                    RemoveObjectData(obj);
                    continue;
                }

                Vector2 objectPos = objectRect.anchoredPosition;
                float distance = Vector2.Distance(bulletPos, objectPos);

                if (distance < 60f)
                {
                    bool isAsteroid = isAsteroidDict[obj];

                    if (isAsteroid)
                    {
                        if (asteroidHealthDict.ContainsKey(obj))
                        {
                            int currentHealth = asteroidHealthDict[obj];
                            currentHealth--;
                            asteroidHealthDict[obj] = currentHealth;

                            if (currentHealth <= 0)
                            {
                                ProcessAsteroidDestroyed();
                                activeObjects.RemoveAt(i);
                                RemoveObjectData(obj);
                                StartCoroutine(AnimateAsteroidDestruction(obj));
                                Debug.Log("Asteroid destroyed by bullet!");
                            }
                            else
                            {
                                StartCoroutine(ShowEnhancedAsteroidDamage(obj));
                                Debug.Log($"Asteroid hit! Health remaining: {currentHealth}");
                            }
                        }

                        activeBullets.RemoveAt(b);
                        StartCoroutine(AnimateBulletDestruction(bullet));
                        StartCoroutine(ShowEnhancedExplosionEffect(objectPos));
                        return;
                    }
                }
            }
        }
    }

    // NEW: Enhanced asteroid damage animation
    IEnumerator ShowEnhancedAsteroidDamage(GameObject asteroid)
    {
        if (asteroid == null) yield break;

        RectTransform rect = asteroid.GetComponent<RectTransform>();
        Image img = asteroid.GetComponent<Image>();

        if (rect == null || img == null) yield break;

        Vector3 originalScale = rect.localScale;
        Color originalColor = img.color;
        Vector3 originalRotation = rect.localEulerAngles;

        // Scale punch effect
        float punchDuration = 0.15f;
        float elapsed = 0f;

        while (elapsed < punchDuration)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsed / punchDuration;

            // Scale animation - punch out then back
            float scaleT = t < 0.5f ? t * 2f : (1f - t) * 2f;
            float currentScale = 1f + (0.3f * easeCurve.Evaluate(scaleT));
            rect.localScale = originalScale * currentScale;

            // Rotation shake
            float shakeAngle = Mathf.Sin(t * Mathf.PI * 8f) * 10f * (1f - t);
            Vector3 newRotation = originalRotation;
            newRotation.z += shakeAngle;
            rect.localEulerAngles = newRotation;

            // Color flash
            Color flashColor = Color.Lerp(Color.red, originalColor, t);
            img.color = flashColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset to slightly damaged appearance
        if (asteroid != null && rect != null && img != null)
        {
            rect.localScale = originalScale;
            rect.localEulerAngles = originalRotation;
            Color damagedColor = originalColor * 0.8f;
            damagedColor.a = originalColor.a;
            img.color = damagedColor;
        }
    }

    // NEW: Enhanced asteroid destruction animation
    IEnumerator AnimateAsteroidDestruction(GameObject asteroid)
    {
        if (asteroid == null) yield break;

        RectTransform rect = asteroid.GetComponent<RectTransform>();
        Image img = asteroid.GetComponent<Image>();

        if (rect == null || img == null)
        {
            Destroy(asteroid);
            yield break;
        }

        Vector3 originalScale = rect.localScale;
        Color originalColor = img.color;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration && asteroid != null)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsed / duration;

            // Scale up then shrink
            float scaleT = t < 0.3f ? t / 0.3f : (1f - t) / 0.7f;
            float currentScale = scaleT < 1f ? Mathf.Lerp(1f, 1.5f, scaleT) : Mathf.Lerp(1.5f, 0f, (t - 0.3f) / 0.7f);
            rect.localScale = originalScale * currentScale;

            // Fade out
            Color fadeColor = originalColor;
            fadeColor.a = Mathf.Lerp(originalColor.a, 0f, t);
            img.color = fadeColor;

            // Rotation
            Vector3 rotation = rect.localEulerAngles;
            rotation.z += 360f * Time.deltaTime;
            rect.localEulerAngles = rotation;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (asteroid != null)
            Destroy(asteroid);
    }

    // NEW: Enhanced bullet destruction animation
    IEnumerator AnimateBulletDestruction(GameObject bullet)
    {
        if (bullet == null) yield break;

        RectTransform rect = bullet.GetComponent<RectTransform>();
        Image img = bullet.GetComponent<Image>();

        if (rect == null || img == null)
        {
            Destroy(bullet);
            yield break;
        }

        Vector3 originalScale = rect.localScale;
        Color originalColor = img.color;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration && bullet != null)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsed / duration;

            // Scale up quickly
            float scale = Mathf.Lerp(1f, 2f, t);
            rect.localScale = originalScale * scale;

            // Fade to yellow then white
            Color flashColor = Color.Lerp(originalColor, Color.yellow, t);
            flashColor.a = Mathf.Lerp(originalColor.a, 0f, t);
            img.color = flashColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (bullet != null)
            Destroy(bullet);
    }

    IEnumerator ProcessAsteroidHit(GameObject asteroid)
    {
        Debug.Log("ASTEROID HIT ROCKET - CHECKING IF CAN SHOW QUESTION!");

        // Prevent multiple questions from processing simultaneously
        if (isProcessingQuestion || isQuestionActive || isWaitingForQuestionAnswer)
        {
            Debug.LogWarning("Question already being processed, taking direct damage instead.");
            if (asteroid != null)
            {
                RemoveObjectData(asteroid);
                Destroy(asteroid);
            }
            yield return StartCoroutine(ProcessLifeLoss());
            yield break;
        }

        isProcessingQuestion = true;
        isWaitingForQuestionAnswer = true;

        // Safe cleanup
        if (asteroid != null)
        {
            RemoveObjectData(asteroid);
            Destroy(asteroid);
        }

        StartCoroutine(EnhancedScreenShakeEffect());

        // Wait a frame to ensure cleanup is complete
        yield return new WaitForEndOfFrame();

        // Try to show question, with fallback to damage
        bool questionShown = TryShowQuestion();

        if (!questionShown)
        {
            Debug.LogWarning("Failed to show question, taking damage instead.");
            isProcessingQuestion = false;
            isWaitingForQuestionAnswer = false;
            yield return StartCoroutine(ProcessLifeLoss());
        }
    }


    bool TryShowQuestion()
    {
        Debug.Log("ATTEMPTING TO SHOW QUESTION");

        // Validate all required UI components
        if (!ValidateQuestionUI())
        {
            Debug.LogError("Question UI validation failed!");
            return false;
        }

        // Validate database manager
        if (dbManager == null)
        {
            Debug.LogError("DatabaseManager is null!");
            return false;
        }

        try
        {
            currentQuestion = dbManager.GetRandomUnusedQuestion(quizId);
            Debug.Log($"Question retrieval result: {(currentQuestion != null ? "SUCCESS" : "NULL")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error getting question from database: {e.Message}");
            return false;
        }

        if (currentQuestion == null)
        {
            Debug.LogWarning("No questions available from database!");
            return false;
        }

        if (string.IsNullOrEmpty(currentQuestion.question))
        {
            Debug.LogWarning("Question text is empty!");
            return false;
        }

        if (currentQuestion.options == null || currentQuestion.options.Length < 2)
        {
            Debug.LogWarning("Question options are invalid!");
            return false;
        }

        Debug.Log($"Successfully loaded question: {currentQuestion.question}");

        // Set states
        isQuestionActive = true;
        wasPausedBeforeQuestion = isGamePaused;

        // Setup and show UI immediately - no animation
        SetupQuestionUI();
        questionPanel.SetActive(true);

        Debug.Log("Question panel shown successfully!");
        return true;
    }


    bool ValidateQuestionUI()
    {
        if (questionPanel == null)
        {
            Debug.LogError("questionPanel is null!");
            return false;
        }

        if (questionText == null)
        {
            Debug.LogError("questionText is null!");
            return false;
        }

        if (answerButtons == null || answerButtons.Length == 0)
        {
            Debug.LogError("answerButtons array is null or empty!");
            return false;
        }

        if (answerTexts == null || answerTexts.Length == 0)
        {
            Debug.LogError("answerTexts array is null or empty!");
            return false;
        }

        // Check individual button components
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null)
            {
                Debug.LogError($"answerButtons[{i}] is null!");
                return false;
            }

            if (answerTexts.Length > i && answerTexts[i] == null)
            {
                Debug.LogError($"answerTexts[{i}] is null!");
                return false;
            }
        }

        return true;
    }

    void SetupQuestionUI()
    {
        questionText.text = currentQuestion.question;

        // Setup answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < currentQuestion.options.Length && i < answerTexts.Length)
            {
                answerTexts[i].text = currentQuestion.options[i];
                answerButtons[i].interactable = true;
                answerButtons[i].gameObject.SetActive(true);

                // Reset colors
                if (answerButtonImages != null && i < answerButtonImages.Length && answerButtonImages[i] != null)
                {
                    answerButtonImages[i].color = Color.white;
                }
            }
            else
            {
                // Hide unused buttons
                answerButtons[i].interactable = false;
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // Replace the ProcessAsteroidHitAnswer method
    IEnumerator ProcessAsteroidHitAnswer(int selectedIndex)
    {
        // Disable all buttons immediately
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
                answerButtons[i].interactable = false;
        }

        if (currentQuestion == null)
        {
            Debug.LogError("Current question is null during answer processing!");
            yield return StartCoroutine(ForceCloseQuestion());
            yield break;
        }

        bool isCorrect = (selectedIndex == currentQuestion.correctIndex);
        int correctAnswerIndex = currentQuestion.correctIndex;

        Debug.Log($"Answer selected: {selectedIndex}, Correct: {correctAnswerIndex}, Is Correct: {isCorrect}");

        if (isCorrect)
        {
            Debug.Log("CORRECT ANSWER - NO LIFE LOST!");
            score += correctAnswerPoints;
            consecutiveWrongAnswers = 0;
            UpdateScoreAnimated();
            if (progressMeter != null) UpdateProgressMeterAnimated();

            // Color feedback
            SetAnswerButtonColors(selectedIndex, correctAnswerIndex, true);

            if (correct != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(correct);
        }
        else
        {
            Debug.Log("WRONG ANSWER - LIFE LOST!");
            score = Mathf.Max(0, score + wrongAnswerPenalty);
            UpdateScoreAnimated();
            if (progressMeter != null) UpdateProgressMeterAnimated();
            ApplyDifficultyIncrease();

            // Color feedback
            SetAnswerButtonColors(selectedIndex, correctAnswerIndex, false);

            if (wrong != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(wrong);

            // Process life loss
            StartCoroutine(ProcessLifeLoss());
        }

        // Wait for feedback
        yield return new WaitForSeconds(2f);

        // Close the question and reset ALL states
        yield return StartCoroutine(CompleteQuestionSequence());
    }

    IEnumerator CompleteQuestionSequence()
    {
        Debug.Log("Completing question sequence...");

        // Reset button colors first
        if (answerButtonImages != null)
        {
            for (int i = 0; i < answerButtonImages.Length; i++)
            {
                if (answerButtonImages[i] != null)
                    answerButtonImages[i].color = Color.white;
            }
        }

        // Animate panel hide
        if (questionPanel != null)
        {
            yield return StartCoroutine(SafeAnimateQuestionPanelHide());
        }

        // Reset ALL states at the end
        ResetAllQuestionStates();

        Debug.Log("Question sequence completed successfully.");
    }

    void ResetAllQuestionStates()
    {
        Debug.Log("Resetting ALL question states...");

        // Reset ALL flags
        isQuestionActive = false;
        isWaitingForQuestionAnswer = false;
        wasPausedBeforeQuestion = false;
        isProcessingQuestion = false; // This is crucial!

        currentQuestion = null;

        if (currentQuestionCoroutine != null)
        {
            StopCoroutine(currentQuestionCoroutine);
            currentQuestionCoroutine = null;
        }

        // Ensure UI is properly reset
        if (questionPanel != null && questionPanel.activeSelf)
        {
            questionPanel.SetActive(false);
        }

        // Re-enable all answer buttons for next time
        if (answerButtons != null)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    answerButtons[i].interactable = true;
                }
            }
        }
    }

    IEnumerator CloseQuestionPanel()
    {
        Debug.Log("Closing question panel...");

        // Reset button colors
        if (answerButtonImages != null)
        {
            for (int i = 0; i < answerButtonImages.Length; i++)
            {
                if (answerButtonImages[i] != null)
                    answerButtonImages[i].color = Color.white;
            }
        }

        // Simply hide the panel - no animation
        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
        }

        // Reset all states
        ResetAllQuestionStates();
        isProcessingQuestion = false;

        Debug.Log("Question panel closed and states reset.");
        yield break;
    }

    IEnumerator SafeAnimateQuestionPanelHide()
    {
        if (questionPanel == null) yield break;

        RectTransform panelRect = questionPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            questionPanel.SetActive(false);
            yield break;
        }

        Vector3 originalScale = panelRect.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Safety checks
            if (questionPanel == null || panelRect == null) yield break;

            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 0f, easeCurve != null ? easeCurve.Evaluate(t) : t);
            panelRect.localScale = originalScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
            if (panelRect != null)
                panelRect.localScale = originalScale;
        }
    }

    IEnumerator ForceCloseQuestion()
    {
        Debug.LogWarning("Force closing question due to error.");

        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
        }

        // Reset ALL states, not just some
        ResetAllQuestionStates();

        // Take damage as fallback
        yield return StartCoroutine(ProcessLifeLoss());
    }

    void SetAnswerButtonColors(int selectedIndex, int correctIndex, bool wasCorrect)
    {
        if (answerButtonImages == null) return;

        for (int i = 0; i < answerButtons.Length && i < answerButtonImages.Length; i++)
        {
            if (answerButtonImages[i] == null) continue;

            if (i == selectedIndex)
            {
                answerButtonImages[i].color = wasCorrect ? Color.green : Color.red;
            }
            else if (i == correctIndex && !wasCorrect)
            {
                answerButtonImages[i].color = Color.green;
            }
            else
            {
                answerButtonImages[i].color = Color.white;
            }
        }
    }
    IEnumerator ProcessDeviceCatch(GameObject device)
    {
        Debug.Log("DEVICE CAUGHT!");

        StartCoroutine(EnhancedDeviceCatchAnimation(device));

        devicesCaught++;
        devicesFuelCounter++;
        score += deviceScoreValue;
        UpdateScoreAnimated(); // NEW: Animated score update
        if (progressMeter != null) UpdateProgressMeterAnimated();

        if (devicesFuelCounter >= devicesForFuelRefill)
        {
            RefillFuel();
            devicesFuelCounter = 0;
        }

        StartCoroutine(EnhancedRocketCelebrationAnimation());

        yield return new WaitForSeconds(0.1f);

        RemoveObjectData(device);

        if (score >= targetScore)
        {
            Victory();
        }
    }

    // NEW: Enhanced device catch animation
    IEnumerator EnhancedDeviceCatchAnimation(GameObject device)
    {
        if (device == null) yield break;

        RectTransform deviceRect = device.GetComponent<RectTransform>();
        Image deviceImage = device.GetComponent<Image>();

        if (deviceRect == null) yield break;

        Vector3 originalScale = deviceRect.localScale;
        Color originalColor = deviceImage != null ? deviceImage.color : Color.white;
        Vector2 startPos = deviceRect.anchoredPosition;
        Vector2 endPos = rocket.anchoredPosition;

        float elapsedTime = 0f;

        while (elapsedTime < deviceCatchAnimDuration && device != null)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsedTime / deviceCatchAnimDuration;

            // NEW: Use easing curve for smoother movement
            float easedT = easeCurve.Evaluate(t);

            // Enhanced scale animation
            float scaleProgress = t < 0.3f ? t / 0.3f : t < 0.7f ? 1f : (1f - t) / 0.3f;
            float currentScale = 1f + (deviceCatchScaleMultiplier - 1f) * scaleProgress;
            deviceRect.localScale = originalScale * currentScale;

            Vector2 currentPos = Vector2.Lerp(startPos, endPos, easedT);
            deviceRect.anchoredPosition = currentPos;

            // NEW: Rotation during catch
            Vector3 rotation = deviceRect.localEulerAngles;
            rotation.z += 180f * Time.deltaTime;
            deviceRect.localEulerAngles = rotation;

            if (deviceImage != null)
            {
                // NEW: Color transition to green then fade
                Color catchColor = t < 0.5f ? Color.Lerp(originalColor, Color.green, t * 2f) : Color.green;
                catchColor.a = Mathf.Lerp(originalColor.a, 0f, easedT);
                deviceImage.color = catchColor;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (device != null)
        {
            Destroy(device);
        }
    }

    // NEW: Enhanced rocket celebration
    IEnumerator EnhancedRocketCelebrationAnimation()
    {
        if (rocket == null) yield break;

        Vector3 originalScale = rocket.localScale;
        Vector2 originalPos = rocket.anchoredPosition;
        Vector3 originalRotation = rocket.localEulerAngles;

        float animDuration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < animDuration)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsedTime / animDuration;

            // NEW: More dynamic celebration
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.15f;
            rocket.localScale = originalScale * scaleMultiplier;

            float yOffset = Mathf.Sin(t * Mathf.PI * 2f) * 15f;
            rocket.anchoredPosition = new Vector2(originalPos.x, originalPos.y + yOffset);

            // NEW: Slight rotation
            float rotationOffset = Mathf.Sin(t * Mathf.PI * 6f) * 5f;
            Vector3 newRotation = originalRotation;
            newRotation.z = rotationOffset;
            rocket.localEulerAngles = newRotation;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rocket.localScale = originalScale;
        rocket.anchoredPosition = originalPos;
        rocket.localEulerAngles = originalRotation;
    }

    // NEW: Enhanced screen shake
    IEnumerator EnhancedScreenShakeEffect()
    {
        if (gameArea == null) yield break;

        Vector2 originalPos = gameArea.anchoredPosition;
        float elapsedTime = 0f;
        float shakeDuration = asteroidHitAnimDuration * 0.6f;

        while (elapsedTime < shakeDuration)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsedTime / shakeDuration;
            float intensity = Mathf.Lerp(shakeIntensity, 0f, easeCurve.Evaluate(t));

            Vector2 randomOffset = new Vector2(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity)
            );

            gameArea.anchoredPosition = originalPos + randomOffset;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        gameArea.anchoredPosition = originalPos;
    }

    void ProcessAsteroidDestroyed()
    {
        asteroidsDestroyed++;
        asteroidsFuelCounter++;
        score += asteroidScoreValue;
        UpdateScoreAnimated(); // NEW: Animated score update
        if (progressMeter != null) UpdateProgressMeterAnimated();

        if (asteroidsFuelCounter >= asteroidsForFuelRefill)
        {
            RefillFuel();
            asteroidsFuelCounter = 0;
        }

        Debug.Log("Asteroid destroyed! Score: " + score);

        if (score >= targetScore)
        {
            Victory();
        }
    }

    void RefillFuel()
    {
        currentFuel = Mathf.Min(currentFuel + fuelRefillAmount, maxFuel);
        UpdateFuelMeterAnimated(); // NEW: Animated fuel update

        if (fuelRefill != null)
            AudioManager.Instance.PlaySFX(fuelRefill);

        StartCoroutine(ShowEnhancedFuelRefillEffect());
        Debug.Log($"Fuel refilled! Current fuel: {currentFuel:F1}/{maxFuel}");
    }

    // NEW: Enhanced fuel refill effect
    IEnumerator ShowEnhancedFuelRefillEffect()
    {
        Image fuelFill = fuelMeter.fillRect?.GetComponent<Image>();
        if (fuelFill == null) yield break;

        Color originalColor = fuelFill.color;

        for (int i = 0; i < 4; i++)
        {
            // Pulse to cyan
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                Color pulseColor = Color.Lerp(originalColor, Color.cyan, easeCurve.Evaluate(t));
                fuelFill.color = pulseColor;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Return to original
            elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                Color returnColor = Color.Lerp(Color.cyan, originalColor, easeCurve.Evaluate(t));
                fuelFill.color = returnColor;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    void ShowAsteroidHitQuestion()
    {
        Debug.Log("SHOWING ASTEROID HIT QUESTION");
        Debug.Log($"UI Check: questionPanel null? {questionPanel == null}, questionText null? {questionText == null}");

        // Add null checks for UI elements
        if (questionPanel == null || questionText == null || answerButtons == null || answerTexts == null)
        {
            Debug.LogWarning("Question UI elements are missing! Defaulting to life loss.");
            ResetQuestionStates();
            StartCoroutine(ProcessLifeLoss());
            return;
        }

        // Add null check for database manager
        if (dbManager == null)
        {
            Debug.LogWarning("DatabaseManager is null! Cannot get questions.");
            ResetQuestionStates();
            StartCoroutine(ProcessLifeLoss());
            return;
        }

        currentQuestion = dbManager.GetRandomUnusedQuestion(quizId);

        if (currentQuestion == null)
        {
            Debug.LogWarning("No questions available! Taking damage instead.");
            ResetQuestionStates();
            StartCoroutine(ProcessLifeLoss());
            return;
        }

        Debug.Log($"Setting up question: {currentQuestion.question}");

        // Set question states
        isQuestionActive = true;
        wasPausedBeforeQuestion = isGamePaused;

        questionText.text = currentQuestion.question;

        // Reset and setup answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < currentQuestion.options.Length && answerTexts[i] != null && answerButtons[i] != null)
            {
                answerTexts[i].text = currentQuestion.options[i];
                answerButtons[i].interactable = true;

                // Reset button colors
                if (answerButtonImages != null && i < answerButtonImages.Length)
                    answerButtonImages[i].color = Color.white;
            }
            else if (answerButtons[i] != null)
            {
                answerButtons[i].interactable = false;
            }
        }

        // Animate question panel appearance with null checks
        StartCoroutine(AnimateQuestionPanelShow());
    }

    void ResetQuestionStates()
    {
        Debug.Log("Resetting all question states...");
        isQuestionActive = false;
        isWaitingForQuestionAnswer = false;
        wasPausedBeforeQuestion = false;
        isProcessingQuestion = false;
        currentQuestion = null;

        if (currentQuestionCoroutine != null)
        {
            StopCoroutine(currentQuestionCoroutine);
            currentQuestionCoroutine = null;
        }
    }

    // NEW: Smooth question panel animation
    IEnumerator AnimateQuestionPanelShow()
    {
        if (questionPanel == null)
        {
            Debug.LogError("Cannot animate show - questionPanel is null!");
            yield break;
        }

        questionPanel.SetActive(true);

        RectTransform panelRect = questionPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        Vector3 originalScale = panelRect.localScale;
        panelRect.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Check if objects still exist
            if (questionPanel == null || panelRect == null)
            {
                Debug.LogWarning("Question panel destroyed during animation!");
                yield break;
            }

            float t = elapsed / duration;
            float scale = easeCurve != null ? easeCurve.Evaluate(t) : t;
            panelRect.localScale = originalScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (panelRect != null)
            panelRect.localScale = originalScale;
    }

    void OnAnswerSelected(int selectedIndex)
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = false;
        }

        StartCoroutine(ProcessAsteroidHitAnswer(selectedIndex));
    }

    // NEW: Smooth question panel hide animation
    IEnumerator AnimateQuestionPanelHide()
    {
        if (questionPanel == null) yield break;

        RectTransform panelRect = questionPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            questionPanel.SetActive(false);
            yield break;
        }

        Vector3 originalScale = panelRect.localScale;

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration && questionPanel != null && panelRect != null)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 0f, easeCurve.Evaluate(t));

            if (panelRect != null)
                panelRect.localScale = originalScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
            if (panelRect != null)
                panelRect.localScale = originalScale;
        }

        // DON'T reset states here - they're handled in ProcessAsteroidHitAnswer
    }



    IEnumerator ProcessLifeLoss()
    {
        currentLife -= lifeLossOnWrongAnswer;
        currentLife = Mathf.Clamp(currentLife, 0f, maxLife);

        UpdateLifeMeterAnimated(); // NEW: Animated life update
        UpdateRocketSpriteAnimated(); // NEW: Animated sprite change

        if (currentLife <= maxLife * 0.25f && lifeLow != null)
        {
            AudioManager.Instance.PlaySFX(lifeLow);
            StartCoroutine(LowLifeWarning());
        }

        StartCoroutine(EnhancedRocketHitAnimation());

        if (currentLife <= 0f)
        {
            Debug.Log("Game Over: Rocket destroyed!");
            GameOver();
        }

        yield return null;
    }

    // NEW: Enhanced rocket hit animation
    IEnumerator EnhancedRocketHitAnimation()
    {
        if (rocketImage == null) yield break;

        RectTransform rocketRect = rocket;
        Color originalColor = rocketImage.color;
        Vector3 originalScale = rocketRect.localScale;
        Vector3 originalRotation = rocketRect.localEulerAngles;
        float flashInterval = asteroidHitAnimDuration / invulnerabilityFlashes;

        for (int i = 0; i < invulnerabilityFlashes; i++)
        {
            if (isGamePaused)
            {
                yield return null;
                i--;
                continue;
            }

            // Flash red with scale punch
            rocketImage.color = Color.red;
            rocketRect.localScale = originalScale * 1.1f;

            // Small rotation shake
            Vector3 shakeRotation = originalRotation;
            shakeRotation.z += Random.Range(-5f, 5f);
            rocketRect.localEulerAngles = shakeRotation;

            yield return new WaitForSeconds(flashInterval * 0.3f);

            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            // Fade to transparent
            Color flashColor = originalColor;
            flashColor.a = 0.3f;
            rocketImage.color = flashColor;
            rocketRect.localScale = originalScale;
            rocketRect.localEulerAngles = originalRotation;

            yield return new WaitForSeconds(flashInterval * 0.7f);
        }

        rocketImage.color = originalColor;
        rocketRect.localScale = originalScale;
        rocketRect.localEulerAngles = originalRotation;
    }

    // NEW: Animated sprite change
    IEnumerator UpdateRocketSpriteAnimated()
    {
        if (rocketImage == null) yield break;

        float lifePercentage = currentLife / maxLife;
        Sprite newSprite = null;

        if (lifePercentage >= 0.75f)
            newSprite = rocketHealthySprite;
        else if (lifePercentage >= 0.5f)
            newSprite = rocketDamagedSprite;
        else if (lifePercentage >= 0.25f)
            newSprite = rocketBadlyDamagedSprite;
        else
            newSprite = rocketCriticalSprite;

        if (newSprite != null && rocketImage.sprite != newSprite)
        {
            // Brief flash effect during sprite change
            Color originalColor = rocketImage.color;
            rocketImage.color = Color.white;
            yield return new WaitForSeconds(0.1f);

            rocketImage.sprite = newSprite;
            rocketImage.color = originalColor;
        }
    }

    void UpdateRocketSprite()
    {
        StartCoroutine(UpdateRocketSpriteAnimated());
    }

    // NEW: Animated meter updates
    void UpdateFuelMeterAnimated()
    {
        if (fuelMeter != null)
        {
            StartCoroutine(AnimateSliderValue(fuelMeter, currentFuel));
            UpdateFuelMeterColor();
        }
    }

    void UpdateLifeMeterAnimated()
    {
        if (lifeMeter != null)
        {
            StartCoroutine(AnimateSliderValue(lifeMeter, currentLife));
            UpdateLifeMeterColor();
        }
    }

    void UpdateProgressMeterAnimated()
    {
        if (progressMeter != null)
        {
            float clampedScore = Mathf.Clamp(score, 0, targetScore);
            StartCoroutine(AnimateSliderValue(progressMeter, clampedScore));
            UpdateProgressMeterColor();
        }
    }

    // NEW: Generic slider animation
    IEnumerator AnimateSliderValue(Slider slider, float targetValue)
    {
        if (slider == null) yield break;

        float startValue = slider.value;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsed / duration;
            float currentValue = Mathf.Lerp(startValue, targetValue, easeCurve.Evaluate(t));
            slider.value = currentValue;

            elapsed += Time.deltaTime;
            yield return null;
        }

        slider.value = targetValue;
    }

    // NEW: Animated score update with scale punch
    void UpdateScoreAnimated()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
            StartCoroutine(AnimateScorePunch());
        }
        UpdateWaveDisplay();
    }

    IEnumerator AnimateScorePunch()
    {
        if (scoreText == null) yield break;

        RectTransform textRect = scoreText.GetComponent<RectTransform>();
        if (textRect == null) yield break;

        Vector3 originalScale = textRect.localScale;

        float punchDuration = 0.2f;
        float elapsed = 0f;

        while (elapsed < punchDuration)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsed / punchDuration;
            float scaleT = t < 0.5f ? t * 2f : (1f - t) * 2f;
            float currentScale = 1f + (0.3f * easeCurve.Evaluate(scaleT));
            textRect.localScale = originalScale * currentScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        textRect.localScale = originalScale;
    }

    // NEW: Enhanced explosion effect
    IEnumerator ShowEnhancedExplosionEffect(Vector2 position)
    {
        Image gameAreaImage = gameArea.GetComponent<Image>();
        if (gameAreaImage != null)
        {
            Color originalColor = gameAreaImage.color;

            // Flash effect
            gameAreaImage.color = new Color(1f, 1f, 0f, 0.4f);

            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!isGamePaused)
                {
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }

            gameAreaImage.color = originalColor;
        }

        // Create explosion particles effect
        StartCoroutine(CreateExplosionParticles(position));
    }

    // NEW: Simple explosion particles
    IEnumerator CreateExplosionParticles(Vector2 center)
    {
        List<GameObject> particles = new List<GameObject>();

        // Create simple particle objects
        for (int i = 0; i < 8; i++)
        {
            GameObject particle = new GameObject("ExplosionParticle");
            particle.transform.SetParent(gameArea.transform, false);

            RectTransform particleRect = particle.AddComponent<RectTransform>();
            Image particleImage = particle.AddComponent<Image>();

            particleRect.sizeDelta = new Vector2(10f, 10f);
            particleRect.anchoredPosition = center;
            particleImage.color = Color.yellow;

            particles.Add(particle);
        }

        // Animate particles
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration && particles.Count > 0)
        {
            if (isGamePaused)
            {
                yield return null;
                continue;
            }

            float t = elapsed / duration;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                if (particles[i] == null)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                RectTransform rect = particles[i].GetComponent<RectTransform>();
                Image img = particles[i].GetComponent<Image>();

                if (rect == null || img == null) continue;

                // Move outward
                float angle = (i * 45f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 currentPos = center + direction * (t * 100f);
                rect.anchoredPosition = currentPos;

                // Fade out
                Color color = img.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                img.color = color;

                // Scale down
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0f, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Clean up particles
        foreach (GameObject particle in particles)
        {
            if (particle != null)
                Destroy(particle);
        }
    }

    void UpdateFuelMeter()
    {
        UpdateFuelMeterAnimated();
    }

    void UpdateLifeMeter()
    {
        UpdateLifeMeterAnimated();
    }

    void UpdateProgressMeter()
    {
        UpdateProgressMeterAnimated();
    }

    void UpdateFuelMeterColor()
    {
        if (fuelMeter != null)
        {
            Image fuelFill = fuelMeter.fillRect?.GetComponent<Image>();
            if (fuelFill != null)
            {
                float fuelPercentage = currentFuel / maxFuel;

                if (fuelPercentage <= 0.2f)
                    fuelFill.color = Color.red;
                else if (fuelPercentage <= 0.4f)
                    fuelFill.color = new Color(1f, 0.5f, 0f);
                else if (fuelPercentage <= 0.6f)
                    fuelFill.color = Color.yellow;
                else
                    fuelFill.color = Color.green;
            }
        }
    }

    void UpdateLifeMeterColor()
    {
        if (lifeMeter != null)
        {
            Image lifeFill = lifeMeter.fillRect?.GetComponent<Image>();
            if (lifeFill != null)
            {
                float lifePercentage = currentLife / maxLife;

                if (lifePercentage <= 0.25f)
                    lifeFill.color = Color.red;
                else if (lifePercentage <= 0.5f)
                    lifeFill.color = new Color(1f, 0.5f, 0f);
                else if (lifePercentage <= 0.75f)
                    lifeFill.color = Color.yellow;
                else
                    lifeFill.color = Color.green;
            }
        }
    }

    void UpdateProgressMeterColor()
    {
        if (progressMeter != null)
        {
            Image progressFill = progressMeter.fillRect?.GetComponent<Image>();
            if (progressFill != null)
            {
                float progress = (float)score / targetScore;

                if (progress < 0.33f)
                {
                    progressFill.color = Color.Lerp(Color.red, Color.yellow, progress * 3f);
                }
                else if (progress < 0.66f)
                {
                    progressFill.color = Color.Lerp(Color.yellow, Color.green, (progress - 0.33f) * 3f);
                }
                else
                {
                    progressFill.color = Color.Lerp(Color.green, new Color(0f, 1f, 0f, 1f), (progress - 0.66f) * 3f);
                }

                if (progress >= 0.9f)
                {
                    float pulse = Mathf.Sin(Time.time * 8f) * 0.3f + 0.7f;
                    progressFill.color = Color.Lerp(Color.green, Color.yellow, pulse);
                }
            }
        }
    }

    IEnumerator ShowExplosionEffect()
    {
        // Use enhanced explosion effect instead
        yield return StartCoroutine(ShowEnhancedExplosionEffect(Vector2.zero));
    }

    void UpdateScore()
    {
        UpdateScoreAnimated();
    }

    void UpdateWaveDisplay()
    {
        if (waveText != null)
        {
            string difficultyStatus = isDifficultyActive ? " [HARD MODE]" : "";
            string pauseStatus = isGamePaused ? " [PAUSED]" : "";

            float progressPercentage = ((float)score / targetScore) * 100f;
            progressPercentage = Mathf.Clamp(progressPercentage, 0f, 100f);

            float fuelPercentage = (currentFuel / maxFuel) * 100f;
            float lifePercentage = (currentLife / maxLife) * 100f;

            waveText.text = $"Score: {score}/{targetScore} ({progressPercentage:F0}%) | Fuel: {fuelPercentage:F0}% | Life: {lifePercentage:F0}%{difficultyStatus}{pauseStatus}";
        }
    }

    void Victory()
    {
        dbManager.AddUserItem(userID, 15);
        dbManager.AddUserItem(userID, 17);
        dbManager.MarkLessonAsCompleted(userID, quizId);
        dbManager.CheckAndUnlockAllLessons(userID);
        lessonHandler.RefreshLessonLocks();
        dbManager.AddCoin(userID, 100);
        dbManager.SaveQuizAndScore(userID, quizId, score);
        dbManager.CheckAndUnlockBadges(userID);
        Debug.Log("VICTORY!");
        isGameActive = false;
        isGamePaused = false;
        CleanupGame();

        gameUI.SetActive(false);
        victoryPanel.SetActive(true);
        AudioManager.Instance.PlaySFX(passed);

        if (settingsModal != null)
            settingsModal.SetActive(false);
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        if (dialogues != null)
        {
            dialogues.StartDialogue(2);
        }
    }

    void GameOver()
    {
        dbManager.AddCoin(userID, 50);
        dbManager.SaveQuizAndScore(userID, quizId, score);
        dbManager.CheckAndUnlockBadges(userID);
        Debug.Log("GAME OVER!");
        isGameActive = false;
        isGamePaused = false;
        CleanupGame();

        gameUI.SetActive(false);
        gameOverPanel.SetActive(true);
        AudioManager.Instance.PlaySFX(failed);

        if (settingsModal != null)
            settingsModal.SetActive(false);
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        if (dialogues != null)
        {
            dialogues.StartDialogue(1);
        }
    }

    void CleanupGame()
    {
        Debug.Log("Cleaning up game...");

        // Stop all coroutines to prevent MissingReferenceExceptions
        StopAllCoroutines();

        // Reset question states first
        ResetQuestionStates();

        // Clean up active objects safely
        if (activeObjects != null)
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeObjects[i];
                if (obj != null)
                {
                    RemoveObjectData(obj);
                    Destroy(obj);
                }
            }
            activeObjects.Clear();
        }

        // Clean up bullets safely
        if (activeBullets != null)
        {
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                GameObject bullet = activeBullets[i];
                if (bullet != null)
                    Destroy(bullet);
            }
            activeBullets.Clear();
        }

        // Clear dictionaries
        if (isAsteroidDict != null) isAsteroidDict.Clear();
        if (asteroidHealthDict != null) asteroidHealthDict.Clear();
        if (asteroidTimeDict != null) asteroidTimeDict.Clear();
        if (asteroidStartPosDict != null) asteroidStartPosDict.Clear();
        if (asteroidRotationSpeedDict != null) asteroidRotationSpeedDict.Clear();
        if (activeAnimations != null) activeAnimations.Clear();

        // Hide question panel safely
        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        Debug.Log("Game cleanup complete.");
    }
    public void RestartGame()
    {
        score = 0;
        isInvulnerable = false;
        asteroidsDestroyed = 0;
        devicesCaught = 0;
        asteroidsFuelCounter = 0;
        devicesFuelCounter = 0;
        consecutiveWrongAnswers = 0;
        isGamePaused = false;
        isWaitingForQuestionAnswer = false;

        // Reset rocket movement
        rocketVelocity = Vector2.zero;
        currentRocketTilt = 0f;
        targetRocketTilt = 0f;

        currentFuel = maxFuel;
        currentLife = maxLife;

        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
        questionPanel.SetActive(false);

        if (settingsModal != null)
            settingsModal.SetActive(false);
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        isAsteroidDict.Clear();
        asteroidHealthDict.Clear();
        asteroidTimeDict.Clear();
        asteroidStartPosDict.Clear();
        asteroidRotationSpeedDict.Clear();
        activeAnimations.Clear();

        Time.timeScale = 1f;

        BeginGame();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        Debug.Log("Returning to menu...");
    }

    public void OpenModal()
    {
        if (isGameActive && !isGamePaused)
        {
            PauseGame();
        }
    }

    public void CloseModal()
    {
        if (isGameActive && isGamePaused)
        {
            ResumeGame();
        }
    }

    public bool IsGamePaused()
    {
        return isGamePaused;
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }
}
