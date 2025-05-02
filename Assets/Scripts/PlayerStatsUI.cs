using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    public Text coinsText;
    public Text experienceText;
    public Text levelText;
    public Text energyText;
    public Slider experienceSlider;
    public Slider petHealthSlider;


    void Update()
    {
        if (PlayerStats.Instance != null)
        {
            coinsText.text = "" + PlayerStats.Instance.coins;
            experienceText.text = PlayerStats.Instance.experience + "/100";
            levelText.text = "" + PlayerStats.Instance.level;
            energyText.text = PlayerStats.Instance.energy + "/" + PlayerStats.Instance.maxEnergy;

            experienceSlider.maxValue = 100;
            experienceSlider.value = PlayerStats.Instance.experience;

            if (petHealthSlider != null)
            {
                petHealthSlider.maxValue = PlayerStats.Instance.maxPetHealth;
                petHealthSlider.value = PlayerStats.Instance.petHealth;
            }
        }
    }

}
