using UnityEngine;

public class HeatParticle : MonoBehaviour
{
    private Rigidbody2D rb;
    private float baseSpeed = 50f;
    private static string currentMaterial = "Brick"; // default material

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Random initial direction
        rb.linearVelocity = Random.insideUnitCircle.normalized * baseSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("House"))
        {
            if (currentMaterial == "Glass")
            {
                Destroy(gameObject); // disappears
            }
            else if (currentMaterial == "Foam")
            {
                rb.linearVelocity *= 0.5f; // slows down
            }
            else if (currentMaterial == "Metal")
            {
                rb.linearVelocity *= 1.5f; // gets faster
            }
            // Brick → let physics handle it normally
        }
    }

    public static void SetMaterial(string materialName)
    {
        currentMaterial = materialName;
        Debug.Log("Material set to: " + materialName);
    }
}
