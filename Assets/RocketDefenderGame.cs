using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RocketDefenderGame : MonoBehaviour
{
    [Header("UI References")]
    public Slider energyMeter; // Energy collection meter
    public Text timerText;
    public GameObject gamePanel;
    public GameObject petpanel;

    [Header("Health System")]
    public GameObject[] hearts; // Array of 3 heart UI objects
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Mobile Controls")]
    public bool autoShoot = true; // Auto-shooting for easier mobile play
    public bool useDirectTouch = true; // Touch to move directly vs drag control
    public float touchSensitivity = 1f;
    public GameObject playerRocket; // The controllable rocket
    public Transform rocketTransform;
    public float rocketSpeed = 300f;
    public float screenBoundary = 400f; // Screen width boundary

    [Header("Shooting System")]
    public GameObject bulletPrefab;
    public Transform firePoint; // Where bullets spawn from
    public float bulletSpeed = 500f;
    public float fireRate = 0.3f; // Time between shots
    private float nextFireTime = 0f;
    private List<GameObject> activeBullets = new List<GameObject>();

    [Header("Falling Objects")]
    public GameObject[] asteroidPrefabs; // Different asteroid types
    public GameObject[] devicePrefabs; // Different technological devices
    public RectTransform canvasRect; // Main Canvas RectTransform
    public RectTransform spawnArea; // UI spawn area at top of canvas
    public float fallSpeed = 200f;
    public float spawnRate = 2f; // Objects per second
    private float nextSpawnTime = 0f;
    private List<FallingObject> activeFallingObjects = new List<FallingObject>();

    [Header("Game Settings")]
    public float maxEnergy = 100f;
    public float energyPerDevice = 20f;
    public float gameTime = 120f; // 2 minutes

    [Header("Launch Settings")]
    public float launchSpeed = 500f;
    public float rotationSpeed = 720f;
    public float launchHeight = 1000f;
    public GameObject flameEffect;

    [Header("Visual Feedback")]
    public ParticleSystem hitParticles;
    public ParticleSystem collectParticles;
    public ParticleSystem explosionParticles;
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip collectSound;
    public AudioClip hitSound;
    public AudioClip launchSound;

    public Dialogues dialogues;

    private float currentEnergy;
    private float timeLeft;
    private bool isGameOver = false;
    private bool gameStarted = false;
    private Vector3 rocketStartPosition;

    // Falling object types
    public enum ObjectType
    {
        Asteroid,
        Device
    }

    [System.Serializable]
    public class FallingObject
    {
        public GameObject gameObject;
        public ObjectType type;
        public float speed;
        public bool isHeatDevice; // For device classification logic
    }

    void Start()
    {
        gamePanel.SetActive(false);
        petpanel.SetActive(false);

        currentEnergy = 0;
        currentHealth = maxHealth;
        timeLeft = gameTime;

        // Initialize energy meter
        if (energyMeter != null)
        {
            energyMeter.minValue = 0;
            energyMeter.maxValue = maxEnergy;
            energyMeter.value = 0;
        }

        // Store rocket's initial position
        if (rocketTransform != null)
        {
            rocketStartPosition = rocketTransform.position;
        }

        // Initialize flame effect (start disabled)
        if (flameEffect != null)
        {
            flameEffect.SetActive(false);
        }

        UpdateHealthDisplay();

        // Start intro dialogue
        dialogues.StartDialogue(0);
    }

    void Update()
    {
        // Wait until intro dialogue finishes
        if (!gameStarted)
        {
            gameStarted = true;
            gamePanel.SetActive(true);
            petpanel.SetActive(true);
        }

        if (isGameOver || !gameStarted) return;

        HandleInput();
        UpdateTimer();
        SpawnObjects();
        UpdateFallingObjects();
        UpdateBullets();
        CheckCollisions();
        CheckWinCondition();
    }

    void HandleInput()
    {
        // Mobile touch controls
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (useDirectTouch)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    Input.mousePosition,
                    Camera.main,
                    out localPoint
                );

                float targetX = Mathf.Clamp(localPoint.x, -screenBoundary, screenBoundary);

                Vector2 currentPos = rocketTransform.GetComponent<RectTransform>().anchoredPosition;
                Vector2 targetPos = new Vector2(targetX, currentPos.y);

                rocketTransform.GetComponent<RectTransform>().anchoredPosition =
                    Vector2.Lerp(currentPos, targetPos, rocketSpeed * Time.deltaTime / 100f);

            }
            else
            {
                // Drag control - rocket follows touch movement
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 deltaPosition = touch.deltaPosition;
                    float moveAmount = deltaPosition.x * touchSensitivity * Time.deltaTime;

                    Vector3 newPosition = rocketTransform.position + Vector3.right * moveAmount;
                    newPosition.x = Mathf.Clamp(newPosition.x, -screenBoundary, screenBoundary);
                    rocketTransform.position = newPosition;
                }
            }
        }

        // Alternative: Mouse for desktop testing
        else if (Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
            float targetX = mouseWorldPos.x;
            targetX = Mathf.Clamp(targetX, -screenBoundary, screenBoundary);

            Vector3 currentPos = rocketTransform.position;
            Vector3 targetPos = new Vector3(targetX, currentPos.y, currentPos.z);

            rocketTransform.position = Vector3.Lerp(currentPos, targetPos, rocketSpeed * Time.deltaTime / 100f);
        }

        // Shooting system
        if (autoShoot)
        {
            // Auto-shooting for easier mobile gameplay
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
        else
        {
            // Manual shooting - tap anywhere to shoot
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began && Time.time >= nextFireTime)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
            }
            else if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            // Instantiate as UI element under canvas
            GameObject bullet = Instantiate(bulletPrefab, canvasRect);

            // Position relative to rocket’s fire point (UI space)
            RectTransform bulletRect = bullet.GetComponent<RectTransform>();
            bulletRect.anchoredPosition = firePoint.GetComponent<RectTransform>().anchoredPosition;

            activeBullets.Add(bullet);
            PlaySound(shootSound);

            // Destroy bullet after 3 seconds
            StartCoroutine(DestroyBulletAfterTime(bullet, 3f));
        }
    }


    IEnumerator DestroyBulletAfterTime(GameObject bullet, float time)
    {
        yield return new WaitForSeconds(time);
        if (bullet != null)
        {
            activeBullets.Remove(bullet);
            Destroy(bullet);
        }
    }

    void UpdateTimer()
    {
        timeLeft -= Time.deltaTime;
        timerText.text = Mathf.CeilToInt(timeLeft).ToString();

        // Update timer color
        float timeRatio = timeLeft / gameTime;
        if (timeRatio > 0.5f)
            timerText.color = Color.white;
        else if (timeRatio > 0.25f)
            timerText.color = Color.yellow;
        else
            timerText.color = Color.red;

        if (timeLeft <= 0)
        {
            EndGame(false);
        }
    }

    void SpawnObjects()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnRandomObject();
            nextSpawnTime = Time.time + (1f / spawnRate);
        }
    }

    void SpawnRandomObject()
    {
        // 60% chance for asteroid, 40% chance for device
        bool spawnAsteroid = Random.Range(0f, 1f) < 0.6f;

        GameObject prefab;
        ObjectType type;

        if (spawnAsteroid)
        {
            prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            type = ObjectType.Asteroid;
        }
        else
        {
            prefab = devicePrefabs[Random.Range(0, devicePrefabs.Length)];
            type = ObjectType.Device;
        }

        // Random spawn position across the top
        float spawnX = Random.Range(-screenBoundary, screenBoundary);
        Vector3 spawnPos = new Vector3(spawnX, spawnArea.position.y, 0);

        GameObject obj = Instantiate(prefab, spawnArea);
        obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(spawnX, 0);

        FallingObject fallingObj = new FallingObject
        {
            gameObject = obj,
            type = type,
            speed = fallSpeed + Random.Range(-50f, 50f), // Slight speed variation
            isHeatDevice = type == ObjectType.Device ? Random.Range(0f, 1f) < 0.7f : false // 70% of devices are heat devices
        };

        activeFallingObjects.Add(fallingObj);

        // Add identifying component
        ObjectIdentifier identifier = obj.GetComponent<ObjectIdentifier>();
        if (identifier == null)
            identifier = obj.AddComponent<ObjectIdentifier>();
        identifier.objectType = type;
        identifier.isHeatDevice = fallingObj.isHeatDevice;
    }

    void UpdateFallingObjects()
    {
        for (int i = activeFallingObjects.Count - 1; i >= 0; i--)
        {
            FallingObject obj = activeFallingObjects[i];
            if (obj.gameObject == null) continue;

            RectTransform objRect = obj.gameObject.GetComponent<RectTransform>();
            if (objRect != null)
            {
                // Move object down using UI anchored position
                Vector2 currentPos = objRect.anchoredPosition;
                currentPos.y -= obj.speed * Time.deltaTime;
                objRect.anchoredPosition = currentPos;

                // Remove if off screen (bottom of canvas)
                if (currentPos.y < -canvasRect.rect.height / 2 - 100f)
                {
                    activeFallingObjects.RemoveAt(i);
                    Destroy(obj.gameObject);
                }
            }
            else
            {
                // Fallback to Transform movement if no RectTransform
                obj.gameObject.transform.position += Vector3.down * obj.speed * Time.deltaTime;

                if (obj.gameObject.transform.position.y < -600f)
                {
                    activeFallingObjects.RemoveAt(i);
                    Destroy(obj.gameObject);
                }
            }
        }
    }

    void UpdateBullets()
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            if (activeBullets[i] == null)
            {
                activeBullets.RemoveAt(i);
                continue;
            }

            RectTransform bulletRect = activeBullets[i].GetComponent<RectTransform>();
            if (bulletRect != null)
            {
                Vector2 pos = bulletRect.anchoredPosition;
                pos.y += bulletSpeed * Time.deltaTime;
                bulletRect.anchoredPosition = pos;

                // Remove if off screen
                if (pos.y > canvasRect.rect.height / 2 + 100f)
                {
                    Destroy(activeBullets[i]);
                    activeBullets.RemoveAt(i);
                }
            }
        }
    }


    void CheckCollisions()
    {
        // Check bullet-object collisions
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            if (activeBullets[i] == null) continue;

            for (int j = activeFallingObjects.Count - 1; j >= 0; j--)
            {
                if (activeFallingObjects[j].gameObject == null) continue;

                float distance = Vector2.Distance(
                    activeBullets[i].GetComponent<RectTransform>().anchoredPosition,
                    activeFallingObjects[j].gameObject.GetComponent<RectTransform>().anchoredPosition
                );

                if (distance < 50f) // Collision threshold
                {
                    HandleBulletHit(activeBullets[i], activeFallingObjects[j]);
                    break;
                }
            }
        }

        // Check rocket-object collisions (for catching devices and asteroid hits)
        for (int i = activeFallingObjects.Count - 1; i >= 0; i--)
        {
            if (activeFallingObjects[i].gameObject == null) continue;
            float distance = Vector2.Distance(
                rocketTransform.GetComponent<RectTransform>().anchoredPosition,
                activeFallingObjects[i].gameObject.GetComponent<RectTransform>().anchoredPosition
            );


            if (distance < 60f) // Collision threshold
            {
                HandleRocketCollision(activeFallingObjects[i]);
            }
        }
    }

    void HandleBulletHit(GameObject bullet, FallingObject target)
    {
        if (target.type == ObjectType.Asteroid)
        {
            // Good! Destroyed an asteroid
            ShowExplosion(target.gameObject.transform.position);
            PlaySound(hitSound);
        }
        else if (target.type == ObjectType.Device)
        {
            // Bad! Shot a device - no energy gained
            ShowExplosion(target.gameObject.transform.position);
            PlaySound(hitSound);
        }

        // Remove bullet and object
        activeBullets.Remove(bullet);
        activeFallingObjects.Remove(target);
        Destroy(bullet);
        Destroy(target.gameObject);
    }

    void HandleRocketCollision(FallingObject target)
    {
        if (target.type == ObjectType.Asteroid)
        {
            // Hit by asteroid - lose health
            TakeDamage();
            ShowExplosion(rocketTransform.position);
        }
        else if (target.type == ObjectType.Device)
        {
            // Caught a device - gain energy
            if (target.isHeatDevice)
            {
                AddEnergy(energyPerDevice);
                ShowCollectEffect(target.gameObject.transform.position);
                PlaySound(collectSound);
            }
        }

        // Remove object
        activeFallingObjects.Remove(target);
        Destroy(target.gameObject);
    }

    void TakeDamage()
    {
        currentHealth--;
        UpdateHealthDisplay();
        PlaySound(hitSound);

        if (currentHealth <= 0)
        {
            EndGame(false);
        }
    }

    void AddEnergy(float amount)
    {
        currentEnergy += amount;
        StartCoroutine(AnimateEnergyMeter(currentEnergy / maxEnergy));
    }

    IEnumerator AnimateEnergyMeter(float targetValue)
    {
        if (energyMeter == null) yield break;

        float startValue = energyMeter.value;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            energyMeter.value = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            yield return null;
        }

        energyMeter.value = targetValue;
    }

    void UpdateHealthDisplay()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(i < currentHealth);
        }
    }

    void CheckWinCondition()
    {
        if (currentEnergy >= maxEnergy)
        {
            EndGame(true);
        }
    }

    void ShowExplosion(Vector3 position)
    {
        if (explosionParticles != null)
        {
            explosionParticles.transform.position = position;
            explosionParticles.Play();
        }
    }

    void ShowCollectEffect(Vector3 position)
    {
        if (collectParticles != null)
        {
            collectParticles.transform.position = position;
            collectParticles.Play();
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void EndGame(bool win)
    {
        isGameOver = true;

        if (win)
        {
            StartCoroutine(LaunchRocketSequence());
        }
        else
        {
            StartCoroutine(GameOverSequence());
        }
    }

    IEnumerator LaunchRocketSequence()
    {
        // Play launch sound
        PlaySound(launchSound);

        // Activate flame effect
        if (flameEffect != null)
        {
            flameEffect.SetActive(true);
        }

        // Launch the rocket
        yield return StartCoroutine(LaunchRocket());

        // Deactivate flame effect
        if (flameEffect != null)
        {
            flameEffect.SetActive(false);
        }

        // Show win dialogue
        gamePanel.SetActive(false);
        petpanel.SetActive(false);
        dialogues.StartDialogue(1);
    }

    IEnumerator LaunchRocket()
    {
        float launchDuration = 2f;
        Vector3 startPos = rocketTransform.position;
        Vector3 targetPos = startPos + Vector3.up * launchHeight;

        float elapsed = 0f;

        while (elapsed < launchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / launchDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            rocketTransform.position = Vector3.Lerp(startPos, targetPos, easedT);
            rocketTransform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

            float scale = Mathf.Lerp(1f, 0.3f, easedT);
            rocketTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        // Reset rocket
        rocketTransform.position = rocketStartPosition;
        rocketTransform.localScale = Vector3.one;
        rocketTransform.rotation = Quaternion.identity;
    }

    IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(1f);

        gamePanel.SetActive(false);
        petpanel.SetActive(false);

        // Show game over dialogue
        dialogues.StartDialogue(2);
    }

    // Reset game for replay
    public void ResetGame()
    {
        currentEnergy = 0;
        currentHealth = maxHealth;
        timeLeft = gameTime;
        isGameOver = false;
        gameStarted = false;

        // Clear all active objects
        foreach (var bullet in activeBullets)
        {
            if (bullet != null) Destroy(bullet);
        }
        activeBullets.Clear();

        foreach (var obj in activeFallingObjects)
        {
            if (obj.gameObject != null) Destroy(obj.gameObject);
        }
        activeFallingObjects.Clear();

        // Reset UI
        if (energyMeter != null) energyMeter.value = 0;
        UpdateHealthDisplay();

        // Reset rocket position
        rocketTransform.position = rocketStartPosition;
        rocketTransform.rotation = Quaternion.identity;
        rocketTransform.localScale = Vector3.one;

        if (flameEffect != null) flameEffect.SetActive(false);
    }
}

// Component to identify objects
public class ObjectIdentifier : MonoBehaviour
{
    public RocketDefenderGame.ObjectType objectType;
    public bool isHeatDevice;
}
