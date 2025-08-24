using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HeatHeroRescue : MonoBehaviour
{
    [Header("UI References")]
    public Image petImage;
    public Slider powerSlider; // ✨ Changed from Image to Slider
    public Text timerText;
    public RectTransform petPanel;
    public GameObject gamePanel;
    public GameObject petpanel;

    [Header("Rocket & Effects")]
    public GameObject rocketObject; // The rocket GameObject
    public GameObject flameEffect; // Flame sprite/particle system
    public Transform rocketTransform; // For launch animations

    [Header("Settings")]
    public float maxPower = 10f;
    public float powerGain = 1f;
    public float powerLoss = 0.5f;
    public float gameTime = 30f;

    [Header("Launch Settings")]
    public float launchSpeed = 500f;
    public float rotationSpeed = 720f; // degrees per second
    public float launchHeight = 1000f;

    [Header("Panel Animation")]
    public float slideDistance = 300f;
    public float slideDuration = 0.5f;

    [Header("Visual Feedback")]
    public ParticleSystem successParticles;
    public ParticleSystem failParticles;
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    public AudioClip launchSound;

    public Dialogues dialogues;

    private float currentPower;
    private float timeLeft;
    private bool isGameOver = false;
    private bool gameStarted = false;
    private Vector3 rocketStartPosition;
    private Quaternion rocketStartRotation;

    // Launch direction variations
    private List<LaunchPattern> launchPatterns = new List<LaunchPattern>();

    [System.Serializable]
    public class LaunchPattern
    {
        public string name;
        public Vector3 direction;
        public bool shouldSpin;
        public float spinMultiplier;
        public AnimationCurve trajectorycurve;
    }

    void Start()
    {
        gamePanel.SetActive(false);
        petpanel.SetActive(false);

        currentPower = 0;
        timeLeft = gameTime;

        // Initialize power slider
        if (powerSlider != null)
        {
            powerSlider.minValue = 0;
            powerSlider.maxValue = maxPower;
            powerSlider.value = 0;
        }

        // Store rocket's initial position and rotation
        if (rocketTransform != null)
        {
            rocketStartPosition = rocketTransform.position;
            rocketStartRotation = rocketTransform.rotation;
        }

        // Initialize flame effect (start disabled)
        if (flameEffect != null)
        {
            flameEffect.SetActive(false);
        }

        InitializeLaunchPatterns();

        // Start intro dialogue (index 0)
        dialogues.StartDialogue(0);
    }

    void InitializeLaunchPatterns()
    {
        // Create different launch patterns for variety
        launchPatterns.Add(new LaunchPattern
        {
            name = "Straight Up",
            direction = Vector3.up,
            shouldSpin = false,
            spinMultiplier = 0f
        });

        // launchPatterns.Add(new LaunchPattern
        // {
        //     name = "Spinning Launch",
        //     direction = Vector3.up,
        //     shouldSpin = true,
        //     spinMultiplier = 2f
        // });

        // launchPatterns.Add(new LaunchPattern
        // {
        //     name = "Diagonal Right",
        //     direction = new Vector3(0.3f, 1f, 0).normalized,
        //     shouldSpin = true,
        //     spinMultiplier = 1.5f
        // });

        // launchPatterns.Add(new LaunchPattern
        // {
        //     name = "Diagonal Left",
        //     direction = new Vector3(-0.3f, 1f, 0).normalized,
        //     shouldSpin = true,
        //     spinMultiplier = 1.5f
        // });

        // launchPatterns.Add(new LaunchPattern
        // {
        //     name = "Spiral Launch",
        //     direction = Vector3.up,
        //     shouldSpin = true,
        //     spinMultiplier = 3f
        // });
    }

    void Update()
    {
        // Wait until intro dialogue finishes before starting
        if (!gameStarted && dialogues.dialogueFinished)
        {
            gameStarted = true;
            gamePanel.SetActive(true);
            petpanel.SetActive(true);
        }

        if (isGameOver || !gameStarted) return;

        timeLeft -= Time.deltaTime;
        timerText.text = Mathf.CeilToInt(timeLeft).ToString();

        // Update timer color based on remaining time
        UpdateTimerVisuals();

        if (currentPower >= maxPower)
        {
            EndGame(true);
        }
        else if (timeLeft <= 0)
        {
            EndGame(false);
        }
    }

    void UpdateTimerVisuals()
    {
        if (timerText != null)
        {
            float timeRatio = timeLeft / gameTime;
            if (timeRatio > 0.5f)
                timerText.color = Color.white;
            else if (timeRatio > 0.25f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.red;
        }
    }

    public void Answer(bool isHeatDevice, bool correctAnswer)
    {
        if (!gameStarted || isGameOver) return;

        bool isCorrect = (isHeatDevice == correctAnswer);

        if (isCorrect)
        {
            currentPower += powerGain;
            ShowFeedback(true);
            PlaySound(correctSound);
        }
        else
        {
            currentPower = Mathf.Max(0, currentPower - powerLoss);
            ShowFeedback(false);
            PlaySound(incorrectSound);
        }

        // Update slider with smooth animation
        StartCoroutine(AnimatePowerSlider(currentPower / maxPower));
    }

    private IEnumerator AnimatePowerSlider(float targetValue)
    {
        if (powerSlider == null) yield break;

        float startValue = powerSlider.value;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            powerSlider.value = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            yield return null;
        }

        powerSlider.value = targetValue;
    }

    void ShowFeedback(bool isCorrect)
    {
        if (isCorrect && successParticles != null)
        {
            successParticles.Play();
        }
        else if (!isCorrect && failParticles != null)
        {
            failParticles.Play();
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
            StartCoroutine(SlidePetPanelUpperRight(win));
        }
    }

    private IEnumerator LaunchRocketSequence()
    {
        // Play launch sound
        PlaySound(launchSound);

        // Activate flame effect
        if (flameEffect != null)
        {
            flameEffect.SetActive(true);
        }

        // Wait a moment for anticipation
        yield return new WaitForSeconds(0.5f);

        // Choose random launch pattern
        LaunchPattern pattern = launchPatterns[Random.Range(0, launchPatterns.Count)];

        // Launch the rocket with selected pattern
        yield return StartCoroutine(LaunchRocket(pattern));

        // Deactivate flame effect
        if (flameEffect != null)
        {
            flameEffect.SetActive(false);
        }

        // Continue with pet panel animation
        // yield return StartCoroutine(SlidePetPanelUpperRight(true));
    }

    private IEnumerator LaunchRocket(LaunchPattern pattern)
    {
        if (rocketTransform == null) yield break;

        float launchDuration = 2f;
        Vector3 startPos = rocketTransform.position;
        Vector3 targetPos = startPos + (pattern.direction * launchHeight);

        float elapsed = 0f;

        while (elapsed < launchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / launchDuration;

            // Apply easing curve for more dynamic movement
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            // Move rocket
            rocketTransform.position = Vector3.Lerp(startPos, targetPos, easedT);

            // Apply spinning if specified
            if (pattern.shouldSpin)
            {
                float spinAmount = rotationSpeed * pattern.spinMultiplier * Time.deltaTime;
                rocketTransform.Rotate(0, 0, spinAmount);
            }

            // Scale rocket down as it gets higher (distance effect)
            float scale = Mathf.Lerp(1f, 0.3f, easedT);
            rocketTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        // Reset rocket for next game
        rocketTransform.position = rocketStartPosition;
        rocketTransform.rotation = rocketStartRotation;
        rocketTransform.localScale = Vector3.one;
    }

    private IEnumerator SlidePetPanelUpperRight(bool win)
    {
        Vector2 startPos = petPanel.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(slideDistance, slideDistance);

        float elapsed = 0;
        while (elapsed < slideDuration)
        {
            float t = elapsed / slideDuration;
            // Use smooth step for more polished animation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            petPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            elapsed += Time.deltaTime;
            yield return null;
        }

        petPanel.anchoredPosition = endPos;
        yield return new WaitForSeconds(1);

        petpanel.SetActive(false);
        gamePanel.SetActive(false);

        // Show end dialogue
        if (win)
            dialogues.StartDialogue(1);
        else
            dialogues.StartDialogue(2);
    }

    // ✨ New Method: Manual power adjustment (for testing or power-ups)
    public void SetPowerLevel(float powerLevel)
    {
        currentPower = Mathf.Clamp(powerLevel, 0, maxPower);
        if (powerSlider != null)
        {
            powerSlider.value = currentPower;
        }
    }

    // ✨ New Method: Add time bonus
    public void AddTimeBonus(float bonusTime)
    {
        timeLeft = Mathf.Min(timeLeft + bonusTime, gameTime);
    }

    // ✨ New Method: Reset rocket position (useful for multiple rounds)
    public void ResetRocket()
    {
        if (rocketTransform != null)
        {
            rocketTransform.position = rocketStartPosition;
            rocketTransform.rotation = rocketStartRotation;
            rocketTransform.localScale = Vector3.one;
        }

        if (flameEffect != null)
        {
            flameEffect.SetActive(false);
        }
    }
}
