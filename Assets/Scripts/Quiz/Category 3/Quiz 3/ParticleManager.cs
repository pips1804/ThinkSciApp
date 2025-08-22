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
    public GameObject petImage;

    [Header("Panel")]
    public GameObject potBackground;
    public GameObject controllerBackground;
    public GameObject quizPanel;

    [Header("Timer")]
    public float simulationDuration = 10f; // ⏳ set in inspector

    [Header("Quiz Manager")]
    public QuizManager quizManager;

    private Rigidbody2D[] particles;
    private Image[] particleImages;
    private Vector2[] gridPositions;

    private bool isHot = false;
    private float currentSpeed;

    private Coroutine heatRoutine;
    private Coroutine coolRoutine;
    private Coroutine flameRoutine;

    private bool initialized = false;

    void Start()
    {
        potBackground.SetActive(false);
        controllerBackground.SetActive(false);

        // Show intro message first
        petImage.SetActive(true);
        messagePanel.SetActive(true);
        messageText.text = "Watch closely and take note of the changes";
        messageButton.onClick.RemoveAllListeners();
        messageButton.onClick.AddListener(OnMessageConfirmed);

        // Hide flames at start
        foreach (var flame in flameImages)
        {
            var c = flame.color;
            flame.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    void OnMessageConfirmed()
    {
        petImage.SetActive(false);
        messagePanel.SetActive(false);
        potBackground.SetActive(true);
        controllerBackground.SetActive(true);

        InitializeParticles();
        initialized = true;

        Debug.Log("Message confirmed → ParticleManager ready!");

        // ⏳ Start simulation timer
        StartCoroutine(SimulationTimer());
    }

    IEnumerator SimulationTimer()
    {
        yield return new WaitForSeconds(simulationDuration);

        // After duration → hide backgrounds, show dialogue
        potBackground.SetActive(false);
        controllerBackground.SetActive(false);

        messagePanel.SetActive(true);
        messageText.text = "Experiment done! Let’s check your understanding";

        messageButton.onClick.RemoveAllListeners();
        messageButton.onClick.AddListener(() =>
        {
            messagePanel.SetActive(false);
            Debug.Log("Dialogue finished → Next step can happen here.");
            quizPanel.SetActive(true);
            quizManager.StartQuiz();
        });
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
