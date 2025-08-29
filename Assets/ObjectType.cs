using UnityEngine;

public class ObjectType : MonoBehaviour
{
    [Header("Object Properties")]
    public bool isAsteroid = false;
    public bool isTracking = false;

    [Header("Health System")]
    public int currentHealth = 1;
    public int maxHealth = 1;

    void Start()
    {
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
    }

    public void TakeDamage(int damage = 1)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
    }

    public bool IsDestroyed()
    {
        return currentHealth <= 0;
    }

    public float GetHealthPercentage()
    {
        if (maxHealth <= 0) return 0f;
        return (float)currentHealth / maxHealth;
    }
}
