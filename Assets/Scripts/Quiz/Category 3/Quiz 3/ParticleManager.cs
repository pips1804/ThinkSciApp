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

    private Rigidbody2D[] particles;
    private Image[] particleImages;
    private Vector2[] gridPositions;

    private bool isHot = false;
    private float currentSpeed;

    void Start()
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

        // Hide flames at start
        foreach (var flame in flameImages)
        {
            var c = flame.color;
            flame.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    void Update()
    {
        HandleBounds();

        // 🔥 Animate flames only when hot
        if (isHot && flameImages != null)
        {
            float scale = 1f + Mathf.Sin(Time.time * flamePulseSpeed) * flameScaleAmount;

            foreach (var flame in flameImages)
            {
                // Pulse size
                flame.transform.localScale = Vector3.one * scale;

                // Flicker brightness
                float flicker = 1f - Random.Range(0f, flameFlickerStrength);
                Color baseColor = Color.red; // you can also use gradient here
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
        Debug.Log("Heating Up");
        if (isHot) return;
        isHot = true;

        StopAllCoroutines();
        StartCoroutine(HeatUpRoutine());
        StartCoroutine(FadeFlamesIn());
    }

    public void CoolDown()
    {
        Debug.Log("Cooling Down");
        if (!isHot) return;
        isHot = false;

        StopAllCoroutines();
        StartCoroutine(CoolDownRoutine());
        StartCoroutine(FadeFlamesOut());
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
