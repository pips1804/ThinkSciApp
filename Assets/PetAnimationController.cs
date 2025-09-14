using UnityEngine;
using UnityEngine.UI;

public class PetAnimationController : MonoBehaviour
{
    public Animator animator;       // Pet's Animator
    public DatabaseManager db;      // Reference to DatabaseManager

    [Header("Energy thresholds")]
    public int lowEnergyThreshold = 5;
    public int midEnergyThreshold = 10;
    public Text moodText;
    public Text playerEnnergyCount; // Text object to show the name
    public int userID = 1;
    private int energy;

    void Update()
    {
        // Load current energy from DB
        energy = db.LoadPlayerEnergy();
        playerEnnergyCount.text = $"{energy}";
        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        var (name, baseHealth, baseDamage) = db.GetPetStats(userID);
        if (energy <= lowEnergyThreshold)
        {
            moodText.text = $"{name} is tired. Feed it to restore energy!";
            animator.Play("Hurt");
        }
        else if (energy <= midEnergyThreshold)
        {
            moodText.text = $"{name} is doing okay. Keep going!";
            animator.Play("Idle");
        }
        else
        {
            moodText.text = $"{name} is energized! Answer some quizzes!";
            animator.Play("Healed");
        }
    }
}
