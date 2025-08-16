using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ParticleCollisionHandler : MonoBehaviour
{
    public float collisionShakeImpulse = 20f; // tweak this value
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Only apply shake if both are dynamic (we want shake while hot)
        if (rb.bodyType != RigidbodyType2D.Dynamic) return;

        // pick contact point to determine direction
        if (collision.contactCount > 0)
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            Vector2 dir = (rb.position - contactPoint).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle.normalized;
            rb.AddForce(dir * collisionShakeImpulse, ForceMode2D.Impulse);
        }
        else
        {
            // fallback random shake
            rb.AddForce(Random.insideUnitCircle.normalized * collisionShakeImpulse, ForceMode2D.Impulse);
        }
    }
}
