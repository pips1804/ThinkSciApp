using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    public int coins = 200;
    public int energy = 20;
    public int experience = 0;
    public int level = 1;
    public int maxEnergy = 20;
    public int petHealth = 100;
    public int maxPetHealth = 100;

    private DatabaseManager db;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

    public void AddEnergy(int amount)
    {
        energy += amount;
        if (energy > maxEnergy)
            energy = maxEnergy;

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

    public void AddPetHealth(int amount)
    {
        petHealth += amount;
        if (petHealth > 100) petHealth = 100; // max pet health
        SaveStats();
    }

    public void DamagePet(int amount)
    {
        petHealth -= amount;
        if (petHealth < 0) petHealth = 0;
        SaveStats();
    }

    public void LoadStats()
    {
        var stats = db.LoadPlayerStats();
        coins = stats.coins;
        energy = stats.energy;
        maxEnergy = stats.maxEnergy;
        experience = stats.experience;
        level = stats.level;
        petHealth = stats.petHealth;

        while (experience >= 100)
        {
            experience -= 100;
            level++;
        }

        SaveStats();
    }

    public void SaveStats()
    {
        db.SavePlayerStats(coins, energy, maxEnergy, experience, level, petHealth);
    }
}
