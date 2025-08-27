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
    public int asteroidScoreValue = 5;     // Score points per destroyed asteroid

    [Header("Wave Settings")]
    public int currentLevel = 1;
    public float asteroidSpawnChance = 0.6f; // 60% chance to spawn asteroid, 40% device

    [Header("Game State")]
    private int currentHealth = 3;
    private int score = 0;
    private int energyPoints = 0;
    private List<GameObject> activeObjects = new List<GameObject>();
    private bool isGameActive = false;
    private bool isSpawning = false;

    [Header("Dialogue System")]
    public Dialogues dialogues;

    [Header("UI Panels")]
    public GameObject gameUI;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    // Store the relative position of catch zone to rocket for proper syncing
    private Vector2 catchZoneOffset;

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
        if (!isGameActive) return;

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
        Debug.Log("=== SHOOT METHOD CALLED ===");

        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab is NULL!");
            return;
        }

        if (shootingArea == null)
        {
            Debug.LogError("Shooting area is NULL!");
            return;
        }

        Debug.Log($"Creating bullet from prefab: {bulletPrefab.name}");
        GameObject bullet = Instantiate(bulletPrefab, shootingArea);

        if (bullet == null)
        {
            Debug.LogError("Failed to instantiate bullet!");
            return;
        }

        Debug.Log($"Bullet created: {bullet.name}");

        RectTransform bulletRect = bullet.GetComponent<RectTransform>();
        if (bulletRect == null)
        {
            Debug.LogError("Bullet has no RectTransform component!");
            Destroy(bullet);
            return;
        }

        // Check if bullet has Image component
        Image bulletImage = bullet.GetComponent<Image>();
        if (bulletImage == null)
        {
            Debug.LogError("Bullet has no Image component!");
        }
        else
        {
            Debug.Log($"Bullet sprite: {(bulletImage.sprite != null ? bulletImage.sprite.name : "NULL")}");
            Debug.Log($"Bullet color: {bulletImage.color}");

            // Make sure bullet is visible
            bulletImage.color = Color.white;
            bulletRect.sizeDelta = new Vector2(20f, 40f); // Force size
        }

        // Position bullet at fire point (or rocket position if no fire point)
        Vector2 spawnPosition;
        if (firePoint != null)
        {
            // EASIEST FIX: Make sure firePoint and rocket are in same coordinate space
            // If firePoint is child of rocket, just add rocket position + firePoint offset
            Vector2 rocketPos = rocket.anchoredPosition;
            Vector2 firePointOffset = firePoint.anchoredPosition; // This should be relative to rocket
            spawnPosition = rocketPos + firePointOffset;
            Debug.Log($"Rocket pos: {rocketPos}, Fire offset: {firePointOffset}, Final: {spawnPosition}");
        }
        else
        {
            // If no fire point, spawn from rocket top
            Vector2 rocketPos = rocket.anchoredPosition;
            spawnPosition = new Vector2(rocketPos.x, rocketPos.y + rocket.rect.height / 2 + 20f);
            Debug.Log($"Using rocket top position: {spawnPosition}");
        }

        bulletRect.anchoredPosition = spawnPosition;

        // Make sure bullet is active and enabled
        bullet.SetActive(true);

        Debug.Log($"Bullet positioned at: {bulletRect.anchoredPosition}");
        Debug.Log($"Bullet active: {bullet.activeInHierarchy}");
        Debug.Log($"Bullet parent: {bullet.transform.parent.name}");

        activeBullets.Add(bullet);

        // Start bullet movement
        StartCoroutine(MoveBullet(bulletRect, bullet));
    }

    IEnumerator MoveBullet(RectTransform bulletRect, GameObject bulletObj)
    {
        if (bulletRect == null || bulletObj == null)
        {
            Debug.LogError("Bullet or bulletRect is null in MoveBullet!");
            yield break;
        }

        Vector2 startPos = bulletRect.anchoredPosition;
        Debug.Log($"=== BULLET MOVEMENT STARTED ===");
        Debug.Log($"Start position: {startPos}");
        Debug.Log($"Game area height: {gameArea.rect.height}");
        Debug.Log($"Target Y position: {gameArea.rect.height / 2}");

        float timeAlive = 0f;

        while (bulletObj != null && bulletRect != null && timeAlive < 5f) // Max 5 seconds alive
        {
            Vector2 currentPos = bulletRect.anchoredPosition;
            currentPos.y += bulletSpeed * Time.deltaTime;
            bulletRect.anchoredPosition = currentPos;

            timeAlive += Time.deltaTime;

            // Debug bullet position every 0.5 seconds
            if ((int)(timeAlive * 2) != (int)((timeAlive - Time.deltaTime) * 2))
            {
                Debug.Log($"Bullet at: {currentPos}, Time alive: {timeAlive:F1}s");
            }

            // Check if bullet went off screen (above game area)
            if (currentPos.y > gameArea.rect.height / 2 + 100f)
            {
                Debug.Log("Bullet reached top of screen");
                break;
            }

            yield return null;
        }

        // Remove bullet
        if (bulletObj != null)
        {
            Debug.Log($"Removing bullet at position: {bulletRect.anchoredPosition}");
            activeBullets.Remove(bulletObj);
            Destroy(bulletObj);
        }
    }

    void UpdateBullets()
    {
        // Clean up null bullets
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
            // Determine what to spawn based on chance
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

        // Get or add ObjectType component
        ObjectType objectType = newObject.GetComponent<ObjectType>();
        if (objectType == null)
        {
            objectType = newObject.AddComponent<ObjectType>();
        }

        objectType.isAsteroid = isAsteroid;

        // Position at random X, top of screen
        float randomX = Random.Range(-gameArea.rect.width / 2 + 50f, gameArea.rect.width / 2 - 50f);
        RectTransform objectRect = newObject.GetComponent<RectTransform>();
        objectRect.anchoredPosition = new Vector2(randomX, gameArea.rect.height / 2);

        // Add to active objects list
        activeObjects.Add(newObject);

        // Start falling animation
        StartCoroutine(AnimateObjectFalling(objectRect, newObject));

        Debug.Log($"Spawned {(isAsteroid ? "asteroid" : "device")} at {objectRect.anchoredPosition}");
    }

    IEnumerator AnimateObjectFalling(RectTransform objectRect, GameObject objectObj)
    {
        float startY = gameArea.rect.height / 2;
        float endY = -gameArea.rect.height / 2 - 50f; // Below screen

        float elapsedTime = 0f;
        Vector2 startPos = objectRect.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, endY);

        while (elapsedTime < fallDuration && objectObj != null && objectRect != null)
        {
            float t = elapsedTime / fallDuration;
            float easedT = t * t; // Ease-in for gravity effect

            Vector2 currentPos = Vector2.Lerp(startPos, endPos, easedT);
            objectRect.anchoredPosition = currentPos;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Remove object if it reached the bottom
        if (objectObj != null)
        {
            activeObjects.Remove(objectObj);
            Destroy(objectObj);
        }
    }

    void CheckCollisions()
    {
        if (activeObjects.Count == 0) return;

        // Check rocket collision with objects (for catching devices or getting hit by asteroids)
        CheckRocketCollisions();

        // Check bullet collision with asteroids
        CheckBulletCollisions();
    }

    void CheckRocketCollisions()
    {
        Vector2 catchPos = rocketCatchZone != null ? rocketCatchZone.anchoredPosition : rocket.anchoredPosition;
        float catchWidth = rocketCatchZone != null ? rocketCatchZone.rect.width : rocket.rect.width;

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

            // Check vertical alignment
            float catchY = catchPos.y;
            float verticalTolerance = 50f;
            bool isAtCatchLevel = objectPos.y <= catchY + verticalTolerance && objectPos.y >= catchY - verticalTolerance;

            if (isAtCatchLevel)
            {
                // Check horizontal alignment
                float horizontalDistance = Mathf.Abs(objectPos.x - catchPos.x);
                float horizontalTolerance = (catchWidth / 2) + 30f;
                bool isInHorizontalRange = horizontalDistance <= horizontalTolerance;

                if (isInHorizontalRange)
                {
                    if (objectType.isAsteroid)
                    {
                        Debug.Log("Rocket collided with asteroid!");
                        ProcessAsteroidHit();  // ✅ now health decreases
                    }
                    else
                    {
                        Debug.Log("Rocket caught a device!");
                        ProcessDeviceCatch();
                    }

                    activeObjects.RemoveAt(i);
                    Destroy(obj);
                    break;
                }
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

                if (distance < 50f) // collision radius
                {
                    if (objectType.isAsteroid)
                    {
                        ProcessAsteroidDestroyed();
                        Debug.Log("Asteroid destroyed by bullet!");
                    }
                    else
                    {
                        Debug.Log("Device accidentally shot - no energy gained!");
                    }

                    activeObjects.RemoveAt(i);
                    Destroy(obj);

                    activeBullets.RemoveAt(b);
                    Destroy(bullet);
                    return; // exit after one collision
                }
            }
        }
    }


    void ProcessAsteroidHit()
    {
        Debug.Log("=== HIT BY ASTEROID ===");
        currentHealth--;
        UpdateHealthDisplay();

        StartCoroutine(ShowDamageEffect());

        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    void ProcessDeviceCatch()
    {
        Debug.Log("=== DEVICE CAUGHT ===");
        energyPoints += deviceEnergyValue;
        score += deviceEnergyValue;

        UpdateEnergyMeter();
        UpdateScore();

        StartCoroutine(ShowCollectEffect());

        if (energyPoints >= maxEnergyPoints)
        {
            Victory();
        }
    }

    void ProcessAsteroidDestroyed()
    {
        Debug.Log("=== ASTEROID DESTROYED ===");
        score += asteroidScoreValue;
        UpdateScore();

        StartCoroutine(ShowExplosionEffect());
    }

    void UpdateHealthDisplay()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(i < currentHealth);
        }
        Debug.Log($"Health: {currentHealth}/3");
    }

    void UpdateEnergyMeter()
    {
        if (energyMeter != null)
        {
            energyMeter.value = energyPoints;
        }
        Debug.Log($"Energy: {energyPoints}/{maxEnergyPoints}");
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
        Debug.Log($"Score: {score}");
    }

    void UpdateWaveDisplay()
    {
        if (waveText != null)
        {
            waveText.text = "Level " + currentLevel;
        }
    }

    // Visual feedback effects
    IEnumerator ShowDamageEffect()
    {
        // Red flash effect on rocket
        Image rocketImage = rocket.GetComponent<Image>();
        if (rocketImage != null)
        {
            Color originalColor = rocketImage.color;
            rocketImage.color = Color.red;
            yield return new WaitForSeconds(0.3f);
            rocketImage.color = originalColor;
        }
    }

    IEnumerator ShowCollectEffect()
    {
        // Green flash effect on rocket
        Image rocketImage = rocket.GetComponent<Image>();
        if (rocketImage != null)
        {
            Color originalColor = rocketImage.color;
            rocketImage.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            rocketImage.color = originalColor;
        }
    }

    IEnumerator ShowExplosionEffect()
    {
        // You can add particle effects or screen shake here
        Debug.Log("Explosion effect!");
        yield return null;
    }

    void Victory()
    {
        Debug.Log("=== VICTORY! ===");
        isGameActive = false;

        // Clean up
        CleanupGame();

        // Show victory panel
        gameUI.SetActive(false);
        victoryPanel.SetActive(true);

        // Start victory dialogue
        if (dialogues != null)
        {
            dialogues.StartDialogue(2); // Victory dialogue
        }
    }

    void GameOver()
    {
        Debug.Log("=== GAME OVER! ===");
        isGameActive = false;

        // Clean up
        CleanupGame();

        // Show game over panel
        gameUI.SetActive(false);
        gameOverPanel.SetActive(true);

        // Start game over dialogue
        if (dialogues != null)
        {
            dialogues.StartDialogue(1); // Game over dialogue
        }
    }

    void CleanupGame()
    {
        // Destroy all active objects
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        activeObjects.Clear();

        // Destroy all active bullets
        foreach (GameObject bullet in activeBullets)
        {
            if (bullet != null)
                Destroy(bullet);
        }
        activeBullets.Clear();
    }

    // Public methods for UI buttons
    public void RestartGame()
    {
        // Reset game state
        currentHealth = 3;
        energyPoints = 0;
        score = 0;

        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);

        BeginGame();
    }

    public void ReturnToMenu()
    {
        // Implement menu return logic
        Debug.Log("Returning to menu...");
    }

    // DEBUG METHOD - Call this to test bullet creation
    [System.Obsolete("For debugging only")]
    public void TestBulletCreation()
    {
        Debug.Log("=== MANUAL BULLET TEST ===");

        if (bulletPrefab == null)
        {
            Debug.LogError("No bullet prefab assigned!");
            return;
        }

        if (gameArea == null)
        {
            Debug.LogError("No game area assigned!");
            return;
        }

        // Create a test bullet manually
        GameObject testBullet = Instantiate(bulletPrefab, gameArea);
        RectTransform testRect = testBullet.GetComponent<RectTransform>();

        if (testRect == null)
        {
            Debug.LogError("Test bullet has no RectTransform!");
            Destroy(testBullet);
            return;
        }

        // Position it in the center of screen
        testRect.anchoredPosition = Vector2.zero;
        testRect.sizeDelta = new Vector2(30f, 60f); // Make it bigger for visibility

        // Make sure it has an image and is white
        Image img = testBullet.GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.red; // Make it red for visibility
        }
        else
        {
            Debug.LogError("Test bullet has no Image component!");
        }

        Debug.Log($"Test bullet created at center: {testRect.anchoredPosition}");
        Debug.Log($"Test bullet size: {testRect.sizeDelta}");
        Debug.Log($"Test bullet active: {testBullet.activeInHierarchy}");
    }
}

// Helper component to identify object types
[System.Serializable]
public class ObjectType : MonoBehaviour
{
    public bool isAsteroid;
}
