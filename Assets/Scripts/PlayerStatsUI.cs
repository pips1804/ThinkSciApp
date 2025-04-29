using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    public Text coinsText;
    public Text experienceText;
    public Text levelText;
    public Text energyText;

    void Update()
    {
        if (PlayerStats.Instance != null)
        {
            coinsText.text = "Coins: " + PlayerStats.Instance.coins;
            experienceText.text = "EXP: " + PlayerStats.Instance.experience + "/100";
            levelText.text = "Level: " + PlayerStats.Instance.level;
            energyText.text = "Energy: " + PlayerStats.Instance.energy;
        }
    }
}
