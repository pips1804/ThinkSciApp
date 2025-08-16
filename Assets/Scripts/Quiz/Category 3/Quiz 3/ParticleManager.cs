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
                rb.linearVelocity = Vector2.zero; // stay still at start

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
        HandleBounds();
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
    }

    public void CoolDown()
    {
        Debug.Log("Cooling Down");
        if (!isHot) return;
        isHot = false;

        StopAllCoroutines();
        StartCoroutine(CoolDownRoutine());
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

                // Always set a new random direction at the current speed
                particles[i].linearVelocity = Random.insideUnitCircle.normalized * currentSpeed;
            }

            yield return null;
        }

        // After transition, keep them moving at max hot speed
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

                // Move back to grid slowly
                Vector2 targetPos = Vector2.Lerp(startPositions[i], gridPositions[i], t);
                particles[i].MovePosition(container.TransformPoint(targetPos));
                particles[i].linearVelocity = Vector2.zero;
            }

            yield return null;
        }
    }
}
