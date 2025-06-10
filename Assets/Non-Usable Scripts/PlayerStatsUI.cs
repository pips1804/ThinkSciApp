using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    public Text coinsText;
    public Text experienceText;
    public Text levelText;
    public Text energyText;

    public Text coinsText2;
    public Text experienceText2;
    public Text levelText2;
    public Text energyText2;

    public Slider experienceSlider;

    public Slider experienceSlider2;

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

            coinsText2.text = "" + PlayerStats.Instance.coins;
            experienceText2.text = PlayerStats.Instance.experience + "/100";
            levelText2.text = "" + PlayerStats.Instance.level;
            energyText2.text = PlayerStats.Instance.energy + "/" + PlayerStats.Instance.maxEnergy;

            experienceSlider2.maxValue = 100;
            experienceSlider2.value = PlayerStats.Instance.experience;

            if (petHealthSlider != null)
            {
                petHealthSlider.maxValue = PlayerStats.Instance.maxPetHealth;
                petHealthSlider.value = PlayerStats.Instance.petHealth;
            }
        }
    }

}
