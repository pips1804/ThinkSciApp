using UnityEngine;

public class PetAnimationController : MonoBehaviour
{
    public static PetAnimationController Instance;  // Singleton instance

    private Animator animator;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);  // Prevent duplicates

        animator = GetComponent<Animator>();  // Get the Animator
    }

    public void HurtPet(bool isDead = false)
    {
        if (isDead)
        {
            animator.SetBool("isDead", true); // Stay in dead state
        }
        else
        {
            animator.SetTrigger("Hurt"); // Normal hurt
        }
    }

    public void HealPet()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Healed");
            animator.SetTrigger("Healed");
            PlayerStats.Instance.AddPetHealth(10);
            animator.SetBool("isDead", false);
        }
    }
}
