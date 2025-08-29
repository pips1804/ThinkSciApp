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
    public int asteroidScoreValue = 2;
    public int deviceScoreValue = 3;
    public int asteroidMaxHealth = 3;
    public int targetScore = 100;

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

    [Header("UI Panels")]
    public GameObject gameUI;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("Optional Dialogue")]
    public Dialogues dialogues;

    // Simple object tracking
    private Dictionary<GameObject, bool> isAsteroidDict = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, int> asteroidHealthDict = new Dictionary<GameObject, int>();
    private Vector2 catchZoneOffset;

    [System.Serializable]
    public class GameQuestion
    {
        public string question;
        public string[] answers = new string[4];
        public int correctAnswerIndex;
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

        if (rocketCatchZone != null && rocket != null)
        {
            catchZoneOffset = rocketCatchZone.anchoredPosition - rocket.anchoredPosition;
        }

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
        }

        RectTransform objectRect = newObject.GetComponent<RectTransform>();
        float spawnX = Random.Range(-gameArea.rect.width / 2 + 50f, gameArea.rect.width / 2 - 50f);
        objectRect.anchoredPosition = new Vector2(spawnX, gameArea.rect.height / 2);
        activeObjects.Add(newObject);

        StartCoroutine(AnimateObjectFalling(objectRect, newObject));
    }

    IEnumerator AnimateObjectFalling(RectTransform objectRect, GameObject objectObj)
    {
        float startY = gameArea.rect.height / 2;
        float endY = -gameArea.rect.height / 2 - 50f;
        Vector2 startPos = objectRect.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < fallDuration && objectObj != null && objectRect != null)
        {
            if (isQuestionActive)
            {
                yield return null;
                continue;
            }

            float t = elapsedTime / fallDuration;
            float currentY = Mathf.Lerp(startY, endY, t);
            Vector2 newPosition = new Vector2(startPos.x, currentY);
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
                activeObjects.RemoveAt(i);
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
            Vector2 bulletPos = bulletRect.anchoredPosition;

            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeObjects[i];
                if (obj == null || !isAsteroidDict.ContainsKey(obj))
                {
                    activeObjects.RemoveAt(i);
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
                            activeObjects.RemoveAt(i);
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
            asteroidImage.color = originalColor;
        }
    }

    IEnumerator ProcessAsteroidHit(GameObject asteroid)
    {
        Debug.Log("ASTEROID HIT ROCKET!");

        isInvulnerable = true;
        currentHealth--;
        UpdateHealthDisplay();

        // Simple hit animation
        Image rocketImage = rocket.GetComponent<Image>();
        if (rocketImage != null)
        {
            Color originalColor = rocketImage.color;
            rocketImage.color = Color.red;
            yield return new WaitForSeconds(0.3f);
            rocketImage.color = originalColor;
        }

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

    IEnumerator ProcessDeviceCatch(GameObject device)
    {
        Debug.Log("DEVICE CAUGHT!");

        RemoveObjectData(device);
        Destroy(device);

        devicesCaught++;
        energyPoints += deviceEnergyValue;
        score += deviceScoreValue;
        UpdateEnergyMeter();
        UpdateScore();

        if (devicesCaught >= devicesNeededForQuestion)
        {
            ShowQuestion();
        }

        if (energyPoints >= maxEnergyPoints)
        {
            Victory();
        }

        yield return null;
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

        GameQuestion selectedQuestion = questions[Random.Range(0, questions.Length)];
        questionText.text = selectedQuestion.question;

        for (int i = 0; i < answerButtons.Length; i++)
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

        BeginGame();
    }

    public void ReturnToMenu()
    {
        Debug.Log("Returning to menu...");
    }
}
