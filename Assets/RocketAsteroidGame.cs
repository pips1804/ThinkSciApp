using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RocketAsteroidGame : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider energyMeter;              // Energy collection meter
    public Text scoreText;                  // UI text for score display
    public GameObject[] hearts;             // Array of 3 heart UI objects
    public Text waveText;                   // UI text to show current wave/level

    [Header("Player Rocket")]
    public RectTransform rocket;            // The player's rocket
    public float rocketMoveSpeed = 600f;    // Rocket movement speed
    public RectTransform rocketCatchZone;   // Catch zone for collecting devices

    [Header("Game Area")]
    public RectTransform gameArea;          // Parent canvas/area for spawning
    public RectTransform shootingArea;      // Area where bullets can exist

    [Header("Prefabs")]
    public GameObject asteroidPrefab;       // Asteroid prefab
    public GameObject[] devicePrefabs;      // Array of technology device prefabs
    public GameObject bulletPrefab;         // Bullet prefab

    [Header("Shooting")]
    public RectTransform firePoint;         // Where bullets spawn from rocket (UI space)
    public float bulletSpeed = 800f;        // Bullet movement speed
    public float fireRate = 0.3f;          // Time between shots
    private float nextFireTime = 0f;
    private List<GameObject> activeBullets = new List<GameObject>();

    [Header("Game Settings")]
    public float fallDuration = 4f;        // Time for objects to fall from top to bottom
    public float spawnInterval = 1.5f;     // Time between object spawns
    public int maxEnergyPoints = 100;      // Points needed to fill energy meter
    public int deviceEnergyValue = 10;     // Energy points per collected device
    public int asteroidScoreValue = 3;     // Score points per destroyed asteroid (CHANGED from 5)
    public int deviceScoreValue = 5;       // Score points per caught device (NEW)

    [Header("Wave Settings")]
    public int currentLevel = 1;
    public float asteroidSpawnChance = 0.6f; // 60% chance to spawn asteroid, 40% device

    [Header("Smart AI Settings")]
    public float asteroidTargetingChance = 0.4f; // 40% chance asteroids target rocket
    public float targetingAccuracy = 0.7f; // How accurate the targeting is (0-1)
    public float predictionTime = 1.5f; // How far ahead to predict rocket movement

    [Header("Question System")]
    public GameObject questionPanel;        // Panel that shows the question
    public Text questionText;              // Text component for the question
    public Button[] answerButtons;         // Array of 4 answer buttons
    public Text[] answerTexts;             // Text components for the answer buttons
    public Image[] answerButtonImages;     // NEW: Array of 4 button image components (separate GameObjects)
    public int correctAnswerPoints = 15;   // Points for correct answer
    public int wrongAnswerPenalty = -5;    // Points deducted for wrong answer
    public int asteroidsNeededForQuestion = 7;  // Asteroids destroyed to trigger question
    public int devicesNeededForQuestion = 3;    // Devices caught to trigger question

    [Header("Game State")]
    private int currentHealth = 3;
    private int score = 0;
    private int energyPoints = 0;
    private int asteroidsDestroyed = 0;     // NEW: Counter for destroyed asteroids
    private int devicesCaught = 0;          // NEW: Counter for caught devices
    private List<GameObject> activeObjects = new List<GameObject>();
    private bool isGameActive = false;
    private bool isSpawning = false;
    private bool isInvulnerable = false;
    private bool isQuestionActive = false;  // NEW: Flag to pause game during questions
    private float invulnerabilityTime = 1.0f;

    [Header("Dialogue System")]
    public Dialogues dialogues;

    [Header("UI Panels")]
    public GameObject gameUI;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("Animation & Effects")]
    public float deviceCatchAnimationTime = 0.8f;
    public float asteroidHitAnimationTime = 0.6f;
    public float explosionEffectTime = 0.4f;

    // Store the relative position of catch zone to rocket for proper syncing
    private Vector2 catchZoneOffset;

    // Sample questions and answers (you can expand this)
    // [Header("Questions Database")]
    [System.Serializable]
    public class GameQuestion
    {
        public string question;
        public string[] answers = new string[4];
        public int correctAnswerIndex; // 0-3
    }

    public GameQuestion[] questions = new GameQuestion[]
    {
        new GameQuestion
        {
            question = "What planet is known as the Red Planet?",
            answers = new string[] { "Venus", "Mars", "Jupiter", "Saturn" },
            correctAnswerIndex = 1
        },
        new GameQuestion
        {
            question = "What is the largest planet in our solar system?",
            answers = new string[] { "Earth", "Saturn", "Jupiter", "Neptune" },
            correctAnswerIndex = 2
        },
        new GameQuestion
        {
            question = "How many moons does Earth have?",
            answers = new string[] { "0", "1", "2", "3" },
            correctAnswerIndex = 1
        },
        new GameQuestion
        {
            question = "What is the closest star to Earth?",
            answers = new string[] { "Alpha Centauri", "Sirius", "The Sun", "Polaris" },
            correctAnswerIndex = 2
        },
        new GameQuestion
        {
            question = "What force keeps planets in orbit around the Sun?",
            answers = new string[] { "Magnetism", "Gravity", "Friction", "Inertia" },
            correctAnswerIndex = 1
        }
    };

    void Start()
    {
        Debug.Log("=== ROCKET ASTEROID GAME STARTED ===");

        // Calculate catch zone offset relative to rocket
        if (rocketCatchZone != null && rocket != null)
        {
            catchZoneOffset = rocketCatchZone.anchoredPosition - rocket.anchoredPosition;
            Debug.Log($"Catch zone offset calculated: {catchZoneOffset}");
        }

        // Initialize UI
        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
        questionPanel.SetActive(false); // NEW: Hide question panel initially

        // Initialize answer buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int buttonIndex = i; // Capture for closure
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(buttonIndex));
        }

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

        gameUI.SetActive(true);
        isGameActive = true;
        isInvulnerable = false;
        isQuestionActive = false;

        // Reset counters
        asteroidsDestroyed = 0;
        devicesCaught = 0;

        // Initialize energy meter
        if (energyMeter != null)
        {
            energyMeter.minValue = 0;
            energyMeter.maxValue = maxEnergyPoints;
            energyMeter.value = 0;
        }

        // Initialize health display
        UpdateHealthDisplay();
        UpdateScore();
        UpdateWaveDisplay();

        // Start spawning objects
        StartCoroutine(SpawnObjects());
    }

    void Update()
    {
        if (!isGameActive || isQuestionActive) return; // NEW: Don't update game during questions

        HandleRocketMovement();
        HandleShooting();
        CheckCollisions();
        UpdateBullets();
    }

    void HandleRocketMovement()
    {
        float move = 0;
        Vector2 newRocketPos = rocket.anchoredPosition;

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
                    gameArea, touchPos, null, out localPos);

                // Move rocket directly towards touch X
                newRocketPos = new Vector2(localPos.x, newRocketPos.y);
            }
        }
        else if (move != 0)
        {
            // Apply keyboard movement
            newRocketPos.x += move * rocketMoveSpeed * Time.deltaTime;
        }

        // Clamp inside game area
        if (move != 0 || Input.touchCount > 0)
        {
            float halfWidth = gameArea.rect.width / 2f;
            float rocketHalf = rocket.rect.width / 2f;
            newRocketPos.x = Mathf.Clamp(newRocketPos.x, -halfWidth + rocketHalf, halfWidth - rocketHalf);

            rocket.anchoredPosition = newRocketPos;

            // Update catch zone position
            if (rocketCatchZone != null)
            {
                rocketCatchZone.anchoredPosition = newRocketPos + catchZoneOffset;
            }
        }
    }

    void HandleShooting()
    {
        // AUTO-SHOOTING MODE! 🚀🚀🚀
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

        // Ensure bullet visibility
        Image bulletImage = bullet.GetComponent<Image>();
        if (bulletImage != null)
        {
            bulletImage.color = Color.white;
            bulletRect.sizeDelta = new Vector2(20f, 40f);
        }

        // Position bullet at fire point
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
        bullet.SetActive(true);

        activeBullets.Add(bullet);
        StartCoroutine(MoveBullet(bulletRect, bullet));
    }

    IEnumerator MoveBullet(RectTransform bulletRect, GameObject bulletObj)
    {
        if (bulletRect == null || bulletObj == null) yield break;

        float timeAlive = 0f;
        while (bulletObj != null && bulletRect != null && timeAlive < 5f)
        {
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
        isSpawning = true;

        while (isGameActive)
        {
            // Don't spawn during questions
            if (isQuestionActive)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            bool spawnAsteroid = Random.value < asteroidSpawnChance;
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
                yield return new WaitForSeconds(spawnInterval);
                continue;
            }

            SpawnObject(prefabToSpawn, spawnAsteroid);
            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
    }

    void SpawnObject(GameObject prefab, bool isAsteroid)
    {
        GameObject newObject = Instantiate(prefab, gameArea);

        // Set up object type
        ObjectType objectType = newObject.GetComponent<ObjectType>();
        if (objectType == null)
        {
            objectType = newObject.AddComponent<ObjectType>();
        }
        objectType.isAsteroid = isAsteroid;

        RectTransform objectRect = newObject.GetComponent<RectTransform>();

        // IMPROVED ASTEROID TARGETING - Calculate spawn position
        float spawnX;
        if (isAsteroid && Random.value < asteroidTargetingChance)
        {
            // Get current rocket position
            float currentRocketX = rocket.anchoredPosition.x;

            // Predict where rocket might be in the future (simple prediction)
            float rocketVelocityX = 0f;
            if (Input.GetAxis("Horizontal") != 0)
            {
                rocketVelocityX = Input.GetAxis("Horizontal") * rocketMoveSpeed;
            }

            // Predict future position
            float predictedRocketX = currentRocketX + (rocketVelocityX * predictionTime);

            // Add some inaccuracy based on targeting accuracy
            float maxInaccuracy = 150f * (1f - targetingAccuracy);
            float inaccuracy = Random.Range(-maxInaccuracy, maxInaccuracy);
            spawnX = predictedRocketX + inaccuracy;

            // Clamp to game bounds
            float halfWidth = gameArea.rect.width / 2f;
            spawnX = Mathf.Clamp(spawnX, -halfWidth + 50f, halfWidth - 50f);

            Debug.Log($"🎯 TARGETING ASTEROID: Current rocket X: {currentRocketX:F1}, Predicted X: {predictedRocketX:F1}, Spawn X: {spawnX:F1}");
        }
        else
        {
            // Random spawn for devices or non-targeting asteroids
            spawnX = Random.Range(-gameArea.rect.width / 2 + 50f, gameArea.rect.width / 2 - 50f);
            Debug.Log($"📍 Random spawn at X: {spawnX:F1}");
        }

        objectRect.anchoredPosition = new Vector2(spawnX, gameArea.rect.height / 2);
        activeObjects.Add(newObject);
        StartCoroutine(AnimateObjectFalling(objectRect, newObject));
    }

    IEnumerator AnimateObjectFalling(RectTransform objectRect, GameObject objectObj)
    {
        float startY = gameArea.rect.height / 2;
        float endY = -gameArea.rect.height / 2 - 50f;

        float elapsedTime = 0f;
        Vector2 startPos = objectRect.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, endY);

        while (elapsedTime < fallDuration && objectObj != null && objectRect != null)
        {
            // Pause falling during questions
            if (isQuestionActive)
            {
                yield return null;
                continue;
            }

            float t = elapsedTime / fallDuration;
            float easedT = t * t; // Gravity effect

            Vector2 currentPos = Vector2.Lerp(startPos, endPos, easedT);
            objectRect.anchoredPosition = currentPos;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (objectObj != null)
        {
            activeObjects.Remove(objectObj);
            Destroy(objectObj);
        }
    }

    void CheckCollisions()
    {
        if (activeObjects.Count == 0) return;

        // Check rocket collisions (both catching and getting hit)
        CheckRocketCollisions();

        // Check bullet collisions with asteroids only
        CheckBulletCollisions();
    }

    void CheckRocketCollisions()
    {
        Vector2 rocketPos = rocket.anchoredPosition;
        Vector2 catchPos = rocketCatchZone != null ? rocketCatchZone.anchoredPosition : rocketPos;
        float catchRadius = rocketCatchZone != null ? rocketCatchZone.rect.width / 2 : rocket.rect.width / 2;

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            if (obj == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            RectTransform objectRect = obj.GetComponent<RectTransform>();
            ObjectType objectType = obj.GetComponent<ObjectType>();

            if (objectRect == null || objectType == null) continue;

            Vector2 objectPos = objectRect.anchoredPosition;
            float distance = Vector2.Distance(catchPos, objectPos);

            // FIXED COLLISION DETECTION - Use distance-based collision
            if (distance <= catchRadius + 40f) // 40f is tolerance
            {
                Debug.Log($"🔥 COLLISION DETECTED! Distance: {distance:F1}, Object: {(objectType.isAsteroid ? "ASTEROID" : "DEVICE")}");

                if (objectType.isAsteroid)
                {
                    // Asteroid hit - only if not invulnerable
                    if (!isInvulnerable)
                    {
                        Debug.Log("💥 ASTEROID HIT ROCKET!");
                        StartCoroutine(ProcessAsteroidHit(obj));
                    }
                }
                else
                {
                    // Device caught
                    Debug.Log("⭐ DEVICE CAUGHT!");
                    StartCoroutine(ProcessDeviceCatch(obj));
                }

                activeObjects.RemoveAt(i);
                break; // Only one collision per frame
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
            Vector2 bulletPos = bulletRect.anchoredPosition;

            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeObjects[i];
                if (obj == null)
                {
                    activeObjects.RemoveAt(i);
                    continue;
                }

                RectTransform objectRect = obj.GetComponent<RectTransform>();
                ObjectType objectType = obj.GetComponent<ObjectType>();

                if (objectRect == null || objectType == null) continue;

                Vector2 objectPos = objectRect.anchoredPosition;
                float distance = Vector2.Distance(bulletPos, objectPos);

                if (distance < 60f) // Collision radius
                {
                    // ONLY destroy asteroids with bullets, not devices
                    if (objectType.isAsteroid)
                    {
                        Debug.Log("🚀 BULLET HIT ASTEROID!");
                        ProcessAsteroidDestroyed();

                        activeObjects.RemoveAt(i);
                        Destroy(obj);

                        activeBullets.RemoveAt(b);
                        Destroy(bullet);

                        StartCoroutine(ShowExplosionEffect());
                        return;
                    }
                    // Bullets pass through devices - no collision
                }
            }
        }
    }

    IEnumerator ProcessAsteroidHit(GameObject asteroid)
    {
        Debug.Log("🔥 === PROCESSING ASTEROID HIT ===");

        // Start invulnerability immediately
        isInvulnerable = true;

        // Reduce health FIRST
        currentHealth--;
        Debug.Log($"💔 Health reduced to: {currentHealth}");
        UpdateHealthDisplay();

        // Animate the hit
        RectTransform asteroidRect = asteroid.GetComponent<RectTransform>();
        Image rocketImage = rocket.GetComponent<Image>();

        Vector2 originalRocketPos = rocket.anchoredPosition;
        Color originalRocketColor = rocketImage != null ? rocketImage.color : Color.white;

        float animTime = 0f;

        // Hit animation
        while (animTime < asteroidHitAnimationTime)
        {
            float t = animTime / asteroidHitAnimationTime;

            // Screen shake effect
            Vector2 shake = Random.insideUnitCircle * 20f * (1f - t);
            rocket.anchoredPosition = originalRocketPos + shake;

            // Flash effect
            if (rocketImage != null)
            {
                Color flashColor = Color.Lerp(Color.red, originalRocketColor, t);
                rocketImage.color = flashColor;
            }

            // Scale asteroid on impact
            if (asteroidRect != null)
            {
                float scale = Mathf.Lerp(1f, 1.5f, Mathf.Sin(t * Mathf.PI * 3));
                asteroidRect.localScale = Vector3.one * scale;
            }

            animTime += Time.deltaTime;
            yield return null;
        }

        // Reset effects
        rocket.anchoredPosition = originalRocketPos;
        if (rocketImage != null) rocketImage.color = originalRocketColor;

        // Destroy asteroid
        Destroy(asteroid);

        // Check game over
        if (currentHealth <= 0)
        {
            GameOver();
        }
        else
        {
            // End invulnerability after delay
            yield return new WaitForSeconds(invulnerabilityTime);
            isInvulnerable = false;
            Debug.Log("🛡️ Invulnerability ended");
        }
    }

    IEnumerator ProcessDeviceCatch(GameObject device)
    {
        Debug.Log("⭐ === PROCESSING DEVICE CATCH ===");

        RectTransform deviceRect = device.GetComponent<RectTransform>();
        Image rocketImage = rocket.GetComponent<Image>();

        Vector2 startPos = deviceRect.anchoredPosition;
        Vector2 rocketPos = rocket.anchoredPosition;
        Color originalRocketColor = rocketImage != null ? rocketImage.color : Color.white;

        float animTime = 0f;

        // Catch animation
        while (animTime < deviceCatchAnimationTime)
        {
            float t = animTime / deviceCatchAnimationTime;

            // Move device towards rocket
            deviceRect.anchoredPosition = Vector2.Lerp(startPos, rocketPos, t);

            // Scale device down
            float scale = Mathf.Lerp(1f, 0.2f, t);
            deviceRect.localScale = Vector3.one * scale;

            // Green flash on rocket
            if (rocketImage != null)
            {
                float flash = Mathf.Sin(t * Mathf.PI * 4) * 0.5f + 0.5f;
                rocketImage.color = Color.Lerp(originalRocketColor, Color.green, flash * 0.7f);
            }

            animTime += Time.deltaTime;
            yield return null;
        }

        // Reset rocket color
        if (rocketImage != null) rocketImage.color = originalRocketColor;

        // Destroy device
        Destroy(device);

        // NEW: Update game state with new scoring system
        devicesCaught++;
        energyPoints += deviceEnergyValue;
        score += deviceScoreValue; // NEW: 5 points for catching device
        UpdateEnergyMeter();
        UpdateScore();

        Debug.Log($"📊 Devices caught: {devicesCaught}/{devicesNeededForQuestion}");

        // NEW: Check if we should show a question
        if (devicesCaught >= devicesNeededForQuestion)
        {
            ShowQuestion();
        }

        if (energyPoints >= maxEnergyPoints)
        {
            Victory();
        }
    }

    void ProcessAsteroidDestroyed()
    {
        // NEW: Update counters and scoring
        asteroidsDestroyed++;
        score += asteroidScoreValue; // NEW: 3 points for destroying asteroid
        UpdateScore();

        Debug.Log($"💥 Asteroid destroyed! Score: {score}, Asteroids destroyed: {asteroidsDestroyed}/{asteroidsNeededForQuestion}");

        // NEW: Check if we should show a question
        if (asteroidsDestroyed >= asteroidsNeededForQuestion)
        {
            ShowQuestion();
        }
    }

    // NEW: Show question system
    void ShowQuestion()
    {
        Debug.Log("❓ === SHOWING QUESTION ===");

        // Reset counters
        asteroidsDestroyed = 0;
        devicesCaught = 0;

        // Pause the game
        isQuestionActive = true;

        // Select a random question
        GameQuestion selectedQuestion = questions[Random.Range(0, questions.Length)];

        // Set up the UI
        questionText.text = selectedQuestion.question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerTexts[i].text = selectedQuestion.answers[i];
            answerButtons[i].interactable = true;
        }

        // Show the question panel
        questionPanel.SetActive(true);
    }

    // NEW: Handle answer selection
    void OnAnswerSelected(int selectedIndex)
    {
        Debug.Log($"🤔 Answer selected: {selectedIndex}");

        // Disable all buttons to prevent multiple clicks
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = false;
        }

        // Find the current question (we need to store it)
        GameQuestion currentQuestion = questions[Random.Range(0, questions.Length)];

        // For now, let's use a simple approach - we'll improve this
        StartCoroutine(ProcessAnswer(selectedIndex));
    }

    // NEW: Process the selected answer
    IEnumerator ProcessAnswer(int selectedIndex)
    {
        // We need a better way to track the current question
        // For now, let's assume we can determine correctness
        // You should modify this to properly track the current question

        bool isCorrect = false; // This should be determined based on the current question

        // Simple check (you should improve this by storing the current question)
        GameQuestion[] possibleQuestions = questions;
        foreach (var q in possibleQuestions)
        {
            if (questionText.text == q.question)
            {
                isCorrect = (selectedIndex == q.correctAnswerIndex);
                break;
            }
        }

        if (isCorrect)
        {
            Debug.Log($"✅ CORRECT! +{correctAnswerPoints} points");
            score += correctAnswerPoints;

            // Visual feedback for correct answer - use separate image if available
            if (answerButtonImages != null && selectedIndex < answerButtonImages.Length && answerButtonImages[selectedIndex] != null)
            {
                answerButtonImages[selectedIndex].color = Color.green;
            }
            else if (answerButtons[selectedIndex].GetComponent<Image>() != null)
            {
                answerButtons[selectedIndex].GetComponent<Image>().color = Color.green;
            }
        }
        else
        {
            Debug.Log($"❌ WRONG! {wrongAnswerPenalty} points");
            score += wrongAnswerPenalty; // This is negative, so it subtracts

            // Visual feedback for wrong answer - use separate image if available
            if (answerButtonImages != null && selectedIndex < answerButtonImages.Length && answerButtonImages[selectedIndex] != null)
            {
                answerButtonImages[selectedIndex].color = Color.red;
            }
            else if (answerButtons[selectedIndex].GetComponent<Image>() != null)
            {
                answerButtons[selectedIndex].GetComponent<Image>().color = Color.red;
            }

            // Show correct answer
            foreach (var q in questions)
            {
                if (questionText.text == q.question)
                {
                    if (answerButtonImages != null && q.correctAnswerIndex < answerButtonImages.Length && answerButtonImages[q.correctAnswerIndex] != null)
                    {
                        answerButtonImages[q.correctAnswerIndex].color = Color.green;
                    }
                    else if (answerButtons[q.correctAnswerIndex].GetComponent<Image>() != null)
                    {
                        answerButtons[q.correctAnswerIndex].GetComponent<Image>().color = Color.green;
                    }
                    break;
                }
            }
        }

        UpdateScore();

        // Wait for player to see the result
        yield return new WaitForSeconds(2f);

        // Reset button colors
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtonImages != null && i < answerButtonImages.Length && answerButtonImages[i] != null)
            {
                answerButtonImages[i].color = Color.white;
            }
            else if (answerButtons[i].GetComponent<Image>() != null)
            {
                answerButtons[i].GetComponent<Image>().color = Color.white;
            }
        }

        // Hide question panel and resume game
        questionPanel.SetActive(false);
        isQuestionActive = false;

        Debug.Log("🎮 Game resumed after question");
    }

    IEnumerator ShowExplosionEffect()
    {
        // Screen flash for explosion
        Image gameAreaImage = gameArea.GetComponent<Image>();
        if (gameAreaImage != null)
        {
            Color originalColor = gameAreaImage.color;
            gameAreaImage.color = new Color(1f, 1f, 0f, 0.3f); // Yellow flash

            yield return new WaitForSeconds(0.1f);

            gameAreaImage.color = originalColor;
        }

        Debug.Log("💥 Explosion effect shown!");
    }

    void UpdateHealthDisplay()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(i < currentHealth);
        }
        Debug.Log($"❤️ Health display updated: {currentHealth}/3");
    }

    void UpdateEnergyMeter()
    {
        if (energyMeter != null)
        {
            energyMeter.value = energyPoints;
        }
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    void UpdateWaveDisplay()
    {
        if (waveText != null)
        {
            waveText.text = "Level " + currentLevel;
        }
    }

    void Victory()
    {
        Debug.Log("🎉 === VICTORY! ===");
        isGameActive = false;
        CleanupGame();

        gameUI.SetActive(false);
        victoryPanel.SetActive(true);

        if (dialogues != null)
        {
            dialogues.StartDialogue(2);
        }
    }

    void GameOver()
    {
        Debug.Log("💀 === GAME OVER! ===");
        isGameActive = false;
        CleanupGame();

        gameUI.SetActive(false);
        gameOverPanel.SetActive(true);

        if (dialogues != null)
        {
            dialogues.StartDialogue(1);
        }
    }

    void CleanupGame()
    {
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null) Destroy(obj);
        }
        activeObjects.Clear();

        foreach (GameObject bullet in activeBullets)
        {
            if (bullet != null) Destroy(bullet);
        }
        activeBullets.Clear();

        // Hide question panel if active
        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
        }
        isQuestionActive = false;
    }

    public void RestartGame()
    {
        currentHealth = 3;
        energyPoints = 0;
        score = 0;
        isInvulnerable = false;
        asteroidsDestroyed = 0;  // NEW: Reset counters
        devicesCaught = 0;       // NEW: Reset counters

        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
        questionPanel.SetActive(false); // NEW: Hide question panel

        BeginGame();
    }

    public void ReturnToMenu()
    {
        Debug.Log("🏠 Returning to menu...");
    }
}

public class ObjectType : MonoBehaviour
{
    public bool isAsteroid;
}
