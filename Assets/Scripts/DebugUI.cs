using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    public Text coinsText;
    public Text expText;
    public Text levelText;
    public Text energyText;

    public Button addCoinsButton;
    public Button addExpButton;
    public Button useEnergyButton;

    void Start()
    {
        addCoinsButton.onClick.AddListener(() => PlayerStats.Instance.AddCoins(10));
        addExpButton.onClick.AddListener(() => PlayerStats.Instance.AddExperience(20));
        useEnergyButton.onClick.AddListener(() => PlayerStats.Instance.UseEnergy(10));
    }

    void Update()
    {
        if (PlayerStats.Instance != null)
        {
            coinsText.text = "Coins: " + PlayerStats.Instance.coins;
            expText.text = "EXP: " + PlayerStats.Instance.experience + "/100";
            levelText.text = "Level: " + PlayerStats.Instance.level;
            energyText.text = "Energy: " + PlayerStats.Instance.energy;
        }
    }
}
