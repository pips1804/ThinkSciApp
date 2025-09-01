using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ParticleManager : MonoBehaviour
{
    [Header("Container & Prefab")]
    public RectTransform container;
    public GameObject particlePrefab;

    [Header("Grid Settings")]
    public int rows = 5;
    public int cols = 5;
    public float spacing = 25f;

    [Header("Temperature Settings")]
    public float coldSpeed = 0.5f;
    public float hotSpeed = 5f;
    public float transitionTime = 1.5f; // seconds

    [Header("Flame Settings")]
    public Image[] flameImages; // Drag your 6 flame UI Images here
    public float flamePulseSpeed = 2f;
    public float flameScaleAmount = 0.1f;
    public float flameFlickerStrength = 0.3f; // brightness flicker range

    [Header("UI Panels")]
    public GameObject messagePanel;   // drag a simple panel with text + button
    public Text messageText;
    public Button messageButton;

    [Header("Panel")]
    public GameObject potBackground;
    public GameObject controllerBackground;
    public GameObject quizPanel;

    [Header("Timer & Score UI")]
    public Text simulationTimerText; // UI Text for simulation timer
    public GameObject scorePanel; // Panel containing score UI
    public Text scoreText; // UI Text for quiz score
    public Text quizTimerText; // UI Text for quiz question timer

    [Header("Progress UI")]
    public Slider progressSlider; // Reference to progress slider (optional, for direct control)
    public Text progressText; // Reference to progress text (optional, for direct control)

    [Header("Pass/Fail Modals")]
    public GameObject passModal; // Modal shown when quiz is passed
    public GameObject failModal; // Modal shown when quiz is failed
    public Button restartButton; // Restart button (can be in either modal)
    public Button exitButton; // Exit button (optional)

    [Header("Timer")]
    public float simulationDuration = 10f; // ⏳ set in inspector
    public float quizQuestionTime = 30f; // Time per quiz question

    [Header("Quiz Manager")]
    public QuizManager quizManager;

    [Header("Score Settings")]
    public int passingScore = 70; // Minimum score to pass (out of 100)

    private Rigidbody2D[] particles;
    private Image[] particleImages;
    private Vector2[] gridPositions;

    private bool isHot = false;
    private float currentSpeed;

    private Coroutine heatRoutine;
    private Coroutine coolRoutine;
    private Coroutine flameRoutine;
    private Coroutine simulationTimerRoutine;
    private Coroutine quizTimerRoutine;

    private bool initialized = false;
    private bool isFirstTime = true;

    // Quiz tracking
    private int currentScore = 0;
    private int totalQuestions = 0;
    private float currentQuestionTimeLeft = 0f;

    [Header("Knob Reference")]
    public StoveKnob stoveKnob; // Drag your knob here in inspector

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        potBackground.SetActive(false);
        controllerBackground.SetActive(false);
        quizPanel.SetActive(false);
        passModal.SetActive(false);
        failModal.SetActive(false);
        scorePanel.SetActive(false);

        // Hide UI elements
        if (simulationTimerText != null) simulationTimerText.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (quizTimerText != null) quizTimerText.gameObject.SetActive(false);
        // if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        // if (progressText != null) progressText.gameObject.SetActive(false);

        // Setup restart button
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        // Show intro message only on first time
        if (isFirstTime)
        {
            messagePanel.SetActive(true);
            messageText.text = "Watch closely and take note of the changes";
            messageButton.onClick.RemoveAllListeners();
            messageButton.onClick.AddListener(OnMessageConfirmed);
        }
        else
        {
            // Skip intro for restart
            messagePanel.SetActive(false);
            OnMessageConfirmed();
        }

        // Hide flames at start
        foreach (var flame in flameImages)
        {
            var c = flame.color;
            flame.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    void OnMessageConfirmed()
    {
        messagePanel.SetActive(false);
        potBackground.SetActive(true);
        controllerBackground.SetActive(true);

        InitializeParticles();
        initialized = true;
        isFirstTime = false;

        // Show simulation timer
        if (simulationTimerText != null)
        {
            simulationTimerText.gameObject.SetActive(true);
            simulationTimerText.text = "" + simulationDuration.ToString("F0");
        }

        Debug.Log("Message confirmed → ParticleManager ready!");

        // ⏳ Start simulation timer
        simulationTimerRoutine = StartCoroutine(SimulationTimer());
    }

    IEnumerator SimulationTimer()
    {
        float timeLeft = simulationDuration;

        while (timeLeft > 0)
        {
            if (simulationTimerText != null)
            {
                simulationTimerText.text = "" + Mathf.Ceil(timeLeft).ToString("F0");
            }

            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // After duration → hide backgrounds, show dialogue
        if (simulationTimerText != null) simulationTimerText.gameObject.SetActive(false);
        potBackground.SetActive(false);
        controllerBackground.SetActive(false);

        messagePanel.SetActive(true);
        messageText.text = "Experiment done! Let's check your understanding";

        messageButton.onClick.RemoveAllListeners();
        messageButton.onClick.AddListener(() =>
        {
            messagePanel.SetActive(false);
            Debug.Log("Dialogue finished → Starting Quiz");
            StartQuizMode();
        });
    }

    public void StartQuestionTimer()
    {
        if (quizTimerRoutine != null)
            StopCoroutine(quizTimerRoutine);

        quizTimerRoutine = StartCoroutine(QuestionTimerCoroutine());
    }

    IEnumerator QuestionTimerCoroutine()
    {
        currentQuestionTimeLeft = quizQuestionTime;

        if (quizTimerText != null)
            quizTimerText.gameObject.SetActive(true);

        while (currentQuestionTimeLeft > 0)
        {
            if (quizTimerText != null)
            {
                quizTimerText.text = "" + Mathf.Ceil(currentQuestionTimeLeft).ToString("F0");

                // Change color when time is running out
                if (currentQuestionTimeLeft <= 10f)
                    quizTimerText.color = Color.red;
            }

            currentQuestionTimeLeft -= Time.deltaTime;
            yield return null;
        }

        // Time's up - automatically mark as wrong
        if (quizManager != null)
        {
            quizManager.OnTimeUp(); // You'll need to implement this method in QuizManager
        }

        OnQuestionAnswered(false); // Mark as incorrect
    }

    // Update your StartQuizMode method
    void StartQuizMode()
    {
        quizPanel.SetActive(true);
        scorePanel.SetActive(true);

        // Show score display
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            UpdateScoreDisplay();
        }

        // Show and initialize progress display
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = 0f; // Start at 0
        }
        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
        }

        // Initialize quiz tracking
        currentScore = 0;
        totalQuestions = 0;

        // Start quiz
        if (quizManager != null)
        {
            quizManager.StartQuiz();
            UpdateProgressDisplay(); // Initialize progress display
            StartQuestionTimer();
        }
    }

    void UpdateProgressDisplay()
    {
        if (quizManager != null)
        {
            int currentQuestionIndex = quizManager.GetCurrentQuestionIndex(); // 1-based
            int totalQuestions = quizManager.GetTotalQuestions();

            // Update progress slider (0 to 1 range)
            if (progressSlider != null)
            {
                float progress = totalQuestions > 0 ? (float)currentQuestionIndex / totalQuestions : 0f;
                progressSlider.value = progress;

                Debug.Log($"Progress: {currentQuestionIndex}/{totalQuestions} = {progress:F2}");
            }

            // Update progress text
            if (progressText != null)
            {
                progressText.text = $"{currentQuestionIndex}/{totalQuestions}";
            }
        }
    }

    // Update your existing OnQuestionAnswered method
    public void OnQuestionAnswered(bool isCorrect)
    {
        // Stop the timer
        if (quizTimerRoutine != null)
        {
            StopCoroutine(quizTimerRoutine);
            quizTimerRoutine = null;
        }

        // Get total questions from QuizManager for more accurate tracking
        if (quizManager != null)
        {
            totalQuestions = quizManager.GetTotalQuestions();
            currentScore = quizManager.GetCurrentScore();
        }
        else
        {
            // Fallback method
            totalQuestions++;
            if (isCorrect)
                currentScore++;
        }

        UpdateScoreDisplay();
        UpdateProgressDisplay(); // ADD THIS LINE

        // Hide question timer temporarily
        if (quizTimerText != null)
            quizTimerText.gameObject.SetActive(false);
    }

    // Update your existing OnNextQuestion method
    public void OnNextQuestion()
    {
        // Update progress when moving to next question
        UpdateProgressDisplay(); // ADD THIS LINE

        // Start timer for next question
        StartQuestionTimer();
    }



    public void OnQuizCompleted()
    {
        // Stop any running timers
        if (quizTimerRoutine != null)
        {
            StopCoroutine(quizTimerRoutine);
            quizTimerRoutine = null;
        }

        // Hide quiz UI
        quizPanel.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (quizTimerText != null) quizTimerText.gameObject.SetActive(false);

        // Get final score from QuizManager for accuracy
        float scorePercentage = 0f;
        if (quizManager != null)
        {
            scorePercentage = quizManager.GetScorePercentage();
            currentScore = quizManager.GetCurrentScore();
            totalQuestions = quizManager.GetTotalQuestions();
        }
        else
        {
            // Fallback calculation
            scorePercentage = totalQuestions > 0 ? (float)currentScore / totalQuestions * 100f : 0f;
        }

        // Show appropriate modal
        if (scorePercentage >= passingScore)
        {
            ShowPassModal(scorePercentage);
        }
        else
        {
            ShowFailModal(scorePercentage);
        }
    }

    void ShowPassModal(float scorePercentage)
    {
        passModal.SetActive(true);

        // Update pass modal text (assuming it has a Text component)
        // Text passText = passModal.GetComponentInChildren<Text>();
        // if (passText != null)
        // {
        //     passText.text = $"Congratulations!\nYou passed with {scorePercentage:F1}%\nScore: {currentScore}/{totalQuestions}";
        // }
    }

    void ShowFailModal(float scorePercentage)
    {
        failModal.SetActive(true);

        // Update fail modal text (assuming it has a Text component)
        // Text failText = failModal.GetComponentInChildren<Text>();
        // if (failText != null)
        // {
        //     failText.text = $"Better luck next time!\nYou scored {scorePercentage:F1}%\nScore: {currentScore}/{totalQuestions}\nYou need {passingScore}% to pass.";
        // }
    }

    public void RestartGame()
    {
        // Stop all coroutines
        StopAllCoroutines();

        // Hide modals
        passModal.SetActive(false);
        failModal.SetActive(false);

        // Clear particles
        if (particles != null)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null)
                    DestroyImmediate(particles[i].gameObject);
            }
        }

        // Reset knob position
        if (stoveKnob != null)
        {
            stoveKnob.ResetToDefaults();
        }

        // Reset variables
        particles = null;
        particleImages = null;
        initialized = false;
        isHot = false;
        currentScore = 0;
        totalQuestions = 0;

        // Restart from simulation (skip intro)
        isFirstTime = false;
        InitializeGame();
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            float percentage = totalQuestions > 0 ? (float)currentScore / totalQuestions * 100f : 0f;
            scoreText.text = $"{currentScore}";
        }
    }

    void InitializeParticles()
    {
        int total = rows * cols;
        particles = new Rigidbody2D[total];
        particleImages = new Image[total];
        gridPositions = new Vector2[total];

        currentSpeed = coldSpeed;

        // Calculate grid positions
        float startX = -(cols - 1) * spacing / 2f;
        float startY = -(rows - 1) * spacing / 2f;

        int index = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Vector2 pos = new Vector2(startX + x * spacing, startY + y * spacing);
                gridPositions[index] = pos;

                GameObject p = Instantiate(particlePrefab, container);
                p.transform.localPosition = pos;

                Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.linearVelocity = Vector2.zero;

                Image img = p.GetComponent<Image>();
                img.color = Color.blue;

                particles[index] = rb;
                particleImages[index] = img;
                index++;
            }
        }
    }

    void Update()
    {
        if (!initialized) return;

        HandleBounds();

        // 🔥 Animate flames only when hot
        if (isHot && flameImages != null)
        {
            float scale = 1f + Mathf.Sin(Time.time * flamePulseSpeed) * flameScaleAmount;

            foreach (var flame in flameImages)
            {
                flame.transform.localScale = Vector3.one * scale;

                float flicker = 1f - Random.Range(0f, flameFlickerStrength);
                Color baseColor = Color.red;
                flame.color = new Color(baseColor.r * flicker, baseColor.g * flicker * 0.8f, baseColor.b * flicker * 0.5f, flame.color.a);
            }
        }
    }

    void HandleBounds()
    {
        if (particles == null) return;

        float halfWidth = container.rect.width / 2f;
        float halfHeight = container.rect.height / 2f;

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null) continue;

            Vector3 pos = particles[i].transform.localPosition;
            Vector2 vel = particles[i].linearVelocity;

            if (pos.x < -halfWidth || pos.x > halfWidth) vel.x *= -1;
            if (pos.y < -halfHeight || pos.y > halfHeight) vel.y *= -1;

            particles[i].linearVelocity = vel;
        }
    }

    public void HeatUp()
    {
        if (!initialized) return;
        if (isHot) return;
        isHot = true;

        if (heatRoutine != null) StopCoroutine(heatRoutine);
        if (coolRoutine != null) StopCoroutine(coolRoutine);
        if (flameRoutine != null) StopCoroutine(flameRoutine);

        heatRoutine = StartCoroutine(HeatUpRoutine());
        flameRoutine = StartCoroutine(FadeFlamesIn());
    }

    public void CoolDown()
    {
        if (!initialized) return;
        if (!isHot) return;
        isHot = false;

        if (heatRoutine != null) StopCoroutine(heatRoutine);
        if (coolRoutine != null) StopCoroutine(coolRoutine);
        if (flameRoutine != null) StopCoroutine(flameRoutine);

        coolRoutine = StartCoroutine(CoolDownRoutine());
        flameRoutine = StartCoroutine(FadeFlamesOut());
    }

    IEnumerator HeatUpRoutine()
    {
        float t = 0f;
        float startSpeed = currentSpeed;

        while (t < 1f)
        {
            t += Time.deltaTime / transitionTime;
            currentSpeed = Mathf.Lerp(startSpeed, hotSpeed, t);

            for (int i = 0; i < particles.Length; i++)
            {
                particleImages[i].color = Color.Lerp(particleImages[i].color, Color.red, t);
                particles[i].linearVelocity = Random.insideUnitCircle.normalized * currentSpeed;
            }

            yield return null;
        }

        while (isHot)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].linearVelocity = Random.insideUnitCircle.normalized * hotSpeed;
            }
            yield return null;
        }
    }

    IEnumerator CoolDownRoutine()
    {
        float t = 0f;
        float startSpeed = currentSpeed;

        Vector2[] startPositions = new Vector2[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            startPositions[i] = particles[i].transform.localPosition;
        }

        while (t < 1f)
        {
            t += Time.deltaTime / transitionTime;
            currentSpeed = Mathf.Lerp(startSpeed, coldSpeed, t);

            for (int i = 0; i < particles.Length; i++)
            {
                particleImages[i].color = Color.Lerp(particleImages[i].color, Color.blue, t);

                Vector2 targetPos = Vector2.Lerp(startPositions[i], gridPositions[i], t);
                particles[i].MovePosition(container.TransformPoint(targetPos));
                particles[i].linearVelocity = Vector2.zero;
            }

            yield return null;
        }
    }

    IEnumerator FadeFlamesIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.8f;
            foreach (var flame in flameImages)
            {
                var c = flame.color;
                flame.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 1f, t));
            }
            yield return null;
        }
    }

    IEnumerator FadeFlamesOut()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.8f;
            foreach (var flame in flameImages)
            {
                var c = flame.color;
                flame.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, t));
            }
            yield return null;
        }
    }
}
