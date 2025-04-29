using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    public int coins = 0;
    public int energy = 100;
    public int experience = 0;
    public int level = 1;

    private DatabaseManager db;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        db = FindObjectOfType<DatabaseManager>();
        LoadStats();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        SaveStats();
    }

    public void UseEnergy(int amount)
    {
        if (energy >= amount)
        {
            energy -= amount;
            SaveStats();
        }
    }

    public void AddExperience(int amount)
    {
        experience += amount;

        while (experience >= 100)
        {
            experience -= 100;
            level++;
        }

        SaveStats();
    }

    public void SaveStats()
    {
        db.SavePlayerStats(coins, energy, experience, level);
    }

    public void LoadStats()
    {
        var stats = db.LoadPlayerStats();
        coins = stats.coins;
        energy = stats.energy;
        experience = stats.experience;
        level = stats.level;
    }
}
