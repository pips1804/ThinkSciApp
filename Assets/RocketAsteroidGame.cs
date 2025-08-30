using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RocketAsteroidGame : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider energyMeter;
    public Text scoreText;
    public GameObject[] hearts;
    public Text waveText;

    [Header("Player Rocket")]
    public RectTransform rocket;
    public float rocketMoveSpeed = 600f;
    public RectTransform rocketCatchZone;

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
    public int maxEnergyPoints = 100;
    public int deviceEnergyValue = 10;
    public int asteroidMaxHealth = 3;
    public int targetScore = 100;

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
    public int correctAnswerPoints = 15;
    public int wrongAnswerPenalty = -5;
    public int asteroidsNeededForQuestion = 7;
    public int devicesNeededForQuestion = 3;

    [Header("Game State")]
    private int currentHealth = 3;
    private int score = 0;
    private int energyPoints = 0;
    private int asteroidsDestroyed = 0;
    private int devicesCaught = 0;
    private List<GameObject> activeObjects = new List<GameObject>();
    private bool isGameActive = false;
    private bool isInvulnerable = false;
    private bool isQuestionActive = false;
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
    private Vector2 catchZoneOffset;

    [System.Serializable]
    public class GameQuestion
    {
        public string question;
        public string[] answers = new string[4];
        public int correctAnswerIndex;
    }

    public GameQuestion[] questions = new GameQuestion[]
    {    new GameQuestion
    {
        question = "What device converts heat directly into electricity using the Seebeck effect?",
        answers = new string[] { "Solar Panel", "Thermoelectric Generator", "Geothermal Plant", "Battery" },
        correctAnswerIndex = 1
    },
    new GameQuestion
    {
        question = "Which device captures heat from beneath the Earth’s surface to generate electricity?",
        answers = new string[] { "Solar Panel", "Wind Turbine", "Geothermal Plant", "Thermoelectric Generator" },
        correctAnswerIndex = 2
    },
    new GameQuestion
    {
        question = "What type of energy transformation happens in a solar panel?",
        answers = new string[] { "Heat to Mechanical", "Heat to Sound", "Heat to Chemical", "Heat to Electrical" },
        correctAnswerIndex = 3
    },
    new GameQuestion
    {
        question = "What is the main energy source used by geothermal power plants?",
        answers = new string[] { "Sunlight", "Wind", "Heat from Earth’s interior", "Chemical Reactions" },
        correctAnswerIndex = 2
    },
    new GameQuestion
    {
        question = "Which technological device often powers spacecraft by converting heat from radioisotopes into electricity?",
        answers = new string[] { "Solar Panel", "RTG (Radioisotope Thermoelectric Generator)", "Battery", "Wind Turbine" },
        correctAnswerIndex = 1
    },
    new GameQuestion
    {
        question = "What is the role of the Seebeck effect in thermoelectric generators?",
        answers = new string[] { "Converts heat into sound", "Converts temperature differences into electricity", "Stores heat as chemical energy", "Transfers heat to the environment" },
        correctAnswerIndex = 1
    },
    new GameQuestion
    {
        question = "Which device captures sunlight and transforms it into electricity using photovoltaic cells?",
        answers = new string[] { "Solar Panel", "Geothermal Plant", "Wind Turbine", "Heat Engine" },
        correctAnswerIndex = 0
    },
    new GameQuestion
    {
        question = "Which of the following is a disadvantage of geothermal power plants?",
        answers = new string[] { "They release large amounts of smoke", "They depend on radioactive materials", "They are location-dependent", "They cannot run at night" },
        correctAnswerIndex = 2
    },
    new GameQuestion
    {
        question = "What is the main advantage of thermoelectric generators?",
        answers = new string[] { "They require moving parts", "They are silent and reliable", "They use wind as input", "They only work at night" },
        correctAnswerIndex = 1
    },
    new GameQuestion
    {
        question = "In a geothermal plant, what is typically used to turn turbines and generate electricity?",
        answers = new string[] { "Steam from heated water", "Direct sunlight", "Nuclear fuel rods", "Chemical batteries" },
        correctAnswerIndex = 0
    }
    };

    void Start()
    {
        Debug.Log("=== ROCKET ASTEROID GAME STARTED ===");

        if (rocketCatchZone != null && rocket != null)
        {
            catchZoneOffset = rocketCatchZone.anchoredPosition - rocket.anchoredPosition;
        }

        // Store original rocket position for animations
        originalRocketPosition = rocket.anchoredPosition;

        gameUI.SetActive(false);
        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
        questionPanel.SetActive(false);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int buttonIndex = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(buttonIndex));
        }

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
        isQuestionActive = false;
        asteroidsDestroyed = 0;
        devicesCaught = 0;

        if (energyMeter != null)
        {
            energyMeter.minValue = 0;
            energyMeter.maxValue = maxEnergyPoints;
            energyMeter.value = 0;
        }

        UpdateHealthDisplay();
        UpdateScore();
        UpdateWaveDisplay();
        StartCoroutine(SpawnObjects());
    }

    void Update()
    {
        if (!isGameActive || isQuestionActive) return;

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
                newRocketPos = new Vector2(localPos.x, newRocketPos.y);
            }
        }
        else if (move != 0)
        {
            newRocketPos.x += move * rocketMoveSpeed * Time.deltaTime;
        }

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
        while (isGameActive)
        {
            if (isQuestionActive)
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

        // Track object data
        isAsteroidDict[newObject] = isAsteroid;
        if (isAsteroid)
        {
            asteroidHealthDict[newObject] = asteroidMaxHealth;
            asteroidTimeDict[newObject] = 0f;
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

        StartCoroutine(AnimateObjectFalling(objectRect, newObject));
    }

    IEnumerator AnimateObjectFalling(RectTransform objectRect, GameObject objectObj)
    {
        float startY = gameArea.rect.height / 2;
        float endY = -gameArea.rect.height / 2 - 50f;
        Vector2 startPos = objectRect.anchoredPosition;
        float elapsedTime = 0f;
        bool isAsteroid = isAsteroidDict.ContainsKey(objectObj) ? isAsteroidDict[objectObj] : false;

        while (elapsedTime < fallDuration && objectObj != null && objectRect != null)
        {
            if (isQuestionActive)
            {
                yield return null;
                continue;
            }

            float t = elapsedTime / fallDuration;
            float currentY = Mathf.Lerp(startY, endY, t);
            float currentX = startPos.x;

            // Enhanced movement for asteroids
            if (isAsteroid && asteroidTimeDict.ContainsKey(objectObj))
            {
                asteroidTimeDict[objectObj] = elapsedTime;

                // Zigzag movement
                if (enableZigzagMovement)
                {
                    float zigzagOffset = Mathf.Sin(elapsedTime * zigzagSpeed) * zigzagAmplitude * t;
                    currentX += zigzagOffset;
                }

                // Player following
                if (enablePlayerFollowing && rocket != null)
                {
                    float rocketX = rocket.anchoredPosition.x;
                    float directionToPlayer = (rocketX - currentX) * followStrength * t;
                    currentX += directionToPlayer * Time.deltaTime;
                }

                // Keep within bounds
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
    }

    void CheckCollisions()
    {
        if (activeObjects.Count == 0) return;

        CheckRocketCollisions();
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
            if (obj == null || !isAsteroidDict.ContainsKey(obj))
            {
                if (obj != null && activeObjects.Count > i)
                {
                    activeObjects.RemoveAt(i);
                }
                continue;
            }

            RectTransform objectRect = obj.GetComponent<RectTransform>();
            if (objectRect == null) continue;

            Vector2 objectPos = objectRect.anchoredPosition;
            float distance = Vector2.Distance(catchPos, objectPos);

            if (distance <= catchRadius + 40f)
            {
                bool isAsteroid = isAsteroidDict[obj];

                if (isAsteroid)
                {
                    if (!isInvulnerable)
                    {
                        StartCoroutine(ProcessAsteroidHit(obj));
                    }
                }
                else
                {
                    StartCoroutine(ProcessDeviceCatch(obj));
                }

                if (activeObjects.Count > i)
                {
                    activeObjects.RemoveAt(i);
                }
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
            Vector2 bulletPos = bulletRect.anchoredPosition;

            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeObjects[i];
                if (obj == null || !isAsteroidDict.ContainsKey(obj))
                {
                    if (obj != null && activeObjects.Count > i)
                    {
                        activeObjects.RemoveAt(i);
                    }
                    continue;
                }

                RectTransform objectRect = obj.GetComponent<RectTransform>();
                if (objectRect == null) continue;

                Vector2 objectPos = objectRect.anchoredPosition;
                float distance = Vector2.Distance(bulletPos, objectPos);

                if (distance < 60f)
                {
                    bool isAsteroid = isAsteroidDict[obj];

                    if (isAsteroid)
                    {
                        // Damage asteroid
                        int currentHealth = asteroidHealthDict[obj];
                        currentHealth--;
                        asteroidHealthDict[obj] = currentHealth;

                        if (currentHealth <= 0)
                        {
                            // Asteroid destroyed
                            ProcessAsteroidDestroyed();
                            if (activeObjects.Count > i)
                            {
                                activeObjects.RemoveAt(i);
                            }
                            RemoveObjectData(obj);
                            Destroy(obj);
                        }
                        else
                        {
                            // Show damage feedback
                            StartCoroutine(ShowAsteroidDamage(obj));
                        }

                        activeBullets.RemoveAt(b);
                        Destroy(bullet);
                        StartCoroutine(ShowExplosionEffect());
                        return;
                    }
                }
            }
        }
    }

    IEnumerator ShowAsteroidDamage(GameObject asteroid)
    {
        Image asteroidImage = asteroid.GetComponent<Image>();
        if (asteroidImage != null)
        {
            Color originalColor = asteroidImage.color;
            asteroidImage.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            if (asteroidImage != null)
            {
                asteroidImage.color = originalColor;
            }
        }
    }

    // ENHANCED ASTEROID HIT ANIMATION
    IEnumerator ProcessAsteroidHit(GameObject asteroid)
    {
        Debug.Log("ASTEROID HIT ROCKET!");

        isInvulnerable = true;
        currentHealth--;
        UpdateHealthDisplay();

        // Start screen shake and rocket hit animation simultaneously
        StartCoroutine(ScreenShakeEffect());
        StartCoroutine(RocketHitAnimation());

        RemoveObjectData(asteroid);
        Destroy(asteroid);

        if (currentHealth <= 0)
        {
            GameOver();
        }
        else
        {
            yield return new WaitForSeconds(invulnerabilityTime);
            isInvulnerable = false;
        }
    }

    // ENHANCED DEVICE CATCH ANIMATION
    IEnumerator ProcessDeviceCatch(GameObject device)
    {
        Debug.Log("DEVICE CAUGHT!");

        // Start the catch animation before destroying the device
        StartCoroutine(DeviceCatchAnimation(device));

        devicesCaught++;
        energyPoints += deviceEnergyValue;
        score += deviceScoreValue;
        UpdateEnergyMeter();
        UpdateScore();

        // Start rocket celebration animation
        StartCoroutine(RocketCelebrationAnimation());

        // Wait a bit before removing the object data and checking conditions
        yield return new WaitForSeconds(0.1f);

        RemoveObjectData(device);

        // Check for victory based on SCORE, not energy points
        if (score >= targetScore)
        {
            Victory();
        }

        if (devicesCaught >= devicesNeededForQuestion)
        {
            ShowQuestion();
        }
    }

    // NEW: Device catch animation with scaling and fade effect
    IEnumerator DeviceCatchAnimation(GameObject device)
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
            float t = elapsedTime / deviceCatchAnimDuration;
            float easedT = 1f - (1f - t) * (1f - t); // Ease out quad

            // Scale animation - grow then shrink
            float scaleProgress = t < 0.5f ? t * 2f : (1f - t) * 2f;
            float currentScale = 1f + (deviceCatchScaleMultiplier - 1f) * scaleProgress;
            deviceRect.localScale = originalScale * currentScale;

            // Move towards rocket
            Vector2 currentPos = Vector2.Lerp(startPos, endPos, easedT);
            deviceRect.anchoredPosition = currentPos;

            // Fade out
            if (deviceImage != null)
            {
                Color fadeColor = originalColor;
                fadeColor.a = Mathf.Lerp(originalColor.a, 0f, easedT);
                deviceImage.color = fadeColor;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the device is destroyed
        if (device != null)
        {
            Destroy(device);
        }
    }

    // NEW: Rocket celebration animation when catching device
    IEnumerator RocketCelebrationAnimation()
    {
        if (rocket == null) yield break;

        Vector3 originalScale = rocket.localScale;
        Vector2 originalPos = rocket.anchoredPosition;

        float animDuration = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < animDuration)
        {
            float t = elapsedTime / animDuration;

            // Bounce scale effect
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.1f;
            rocket.localScale = originalScale * scaleMultiplier;

            // Subtle upward bounce
            float yOffset = Mathf.Sin(t * Mathf.PI) * 10f;
            rocket.anchoredPosition = new Vector2(originalPos.x, originalPos.y + yOffset);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset to original state
        rocket.localScale = originalScale;
        rocket.anchoredPosition = originalPos;
    }

    // NEW: Screen shake effect for asteroid hits
    IEnumerator ScreenShakeEffect()
    {
        if (gameArea == null) yield break;

        Vector2 originalPos = gameArea.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < asteroidHitAnimDuration * 0.5f) // Shake for half the hit duration
        {
            float intensity = Mathf.Lerp(shakeIntensity, 0f, elapsedTime / (asteroidHitAnimDuration * 0.5f));

            Vector2 randomOffset = new Vector2(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity)
            );

            gameArea.anchoredPosition = originalPos + randomOffset;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset position
        gameArea.anchoredPosition = originalPos;
    }

    // NEW: Enhanced rocket hit animation with flashing
    IEnumerator RocketHitAnimation()
    {
        if (rocket == null) yield break;

        Image rocketImage = rocket.GetComponent<Image>();
        if (rocketImage == null) yield break;

        Color originalColor = rocketImage.color;
        float flashInterval = asteroidHitAnimDuration / invulnerabilityFlashes;

        for (int i = 0; i < invulnerabilityFlashes; i++)
        {
            // Flash red
            rocketImage.color = Color.red;
            yield return new WaitForSeconds(flashInterval * 0.3f);

            // Flash semi-transparent
            Color flashColor = originalColor;
            flashColor.a = 0.3f;
            rocketImage.color = flashColor;
            yield return new WaitForSeconds(flashInterval * 0.7f);
        }

        // Reset to original color
        rocketImage.color = originalColor;
    }

    void ProcessAsteroidDestroyed()
    {
        asteroidsDestroyed++;
        score += asteroidScoreValue;
        UpdateScore();

        Debug.Log("Asteroid destroyed! Score: " + score);

        if (score >= targetScore)
        {
            Victory();
            return;
        }

        if (asteroidsDestroyed >= asteroidsNeededForQuestion)
        {
            ShowQuestion();
        }
    }

    void ShowQuestion()
    {
        Debug.Log("SHOWING QUESTION");

        asteroidsDestroyed = 0;
        devicesCaught = 0;
        isQuestionActive = true;

        if (questions.Length == 0)
        {
            Debug.LogWarning("No questions available!");
            isQuestionActive = false;
            return;
        }

        GameQuestion selectedQuestion = questions[Random.Range(0, questions.Length)];
        questionText.text = selectedQuestion.question;

        for (int i = 0; i < answerButtons.Length && i < selectedQuestion.answers.Length; i++)
        {
            answerTexts[i].text = selectedQuestion.answers[i];
            answerButtons[i].interactable = true;
        }

        questionPanel.SetActive(true);
    }

    void OnAnswerSelected(int selectedIndex)
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].interactable = false;
        }

        StartCoroutine(ProcessAnswer(selectedIndex));
    }

    IEnumerator ProcessAnswer(int selectedIndex)
    {
        bool isCorrect = false;

        foreach (var q in questions)
        {
            if (questionText.text == q.question)
            {
                isCorrect = (selectedIndex == q.correctAnswerIndex);
                break;
            }
        }

        if (isCorrect)
        {
            Debug.Log("CORRECT ANSWER!");
            score += correctAnswerPoints;
            if (answerButtonImages != null && selectedIndex < answerButtonImages.Length)
            {
                answerButtonImages[selectedIndex].color = Color.green;
            }
        }
        else
        {
            Debug.Log("WRONG ANSWER!");
            score += wrongAnswerPenalty;
            if (answerButtonImages != null && selectedIndex < answerButtonImages.Length)
            {
                answerButtonImages[selectedIndex].color = Color.red;
            }
        }

        UpdateScore();
        yield return new WaitForSeconds(2f);

        // Reset button colors
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtonImages != null && i < answerButtonImages.Length)
            {
                answerButtonImages[i].color = Color.white;
            }
        }

        questionPanel.SetActive(false);
        isQuestionActive = false;
    }

    IEnumerator ShowExplosionEffect()
    {
        Image gameAreaImage = gameArea.GetComponent<Image>();
        if (gameAreaImage != null)
        {
            Color originalColor = gameAreaImage.color;
            gameAreaImage.color = new Color(1f, 1f, 0f, 0.3f);
            yield return new WaitForSeconds(0.1f);
            gameAreaImage.color = originalColor;
        }
    }

    void UpdateHealthDisplay()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(i < currentHealth);
        }
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
        UpdateWaveDisplay(); // Update wave display when score changes
    }

    void UpdateWaveDisplay()
    {
        if (waveText != null)
        {
            waveText.text = "Score: " + score + "/" + targetScore;
        }
    }

    void Victory()
    {
        Debug.Log("VICTORY!");
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
        Debug.Log("GAME OVER!");
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
            if (obj != null)
            {
                RemoveObjectData(obj);
                Destroy(obj);
            }
        }
        activeObjects.Clear();

        foreach (GameObject bullet in activeBullets)
        {
            if (bullet != null) Destroy(bullet);
        }
        activeBullets.Clear();

        isAsteroidDict.Clear();
        asteroidHealthDict.Clear();
        asteroidTimeDict.Clear();
        asteroidStartPosDict.Clear();

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
        asteroidsDestroyed = 0;
        devicesCaught = 0;

        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
        questionPanel.SetActive(false);

        isAsteroidDict.Clear();
        asteroidHealthDict.Clear();
        asteroidTimeDict.Clear();
        asteroidStartPosDict.Clear();

        BeginGame();
    }

    public void ReturnToMenu()
    {
        Debug.Log("Returning to menu...");
    }
}
