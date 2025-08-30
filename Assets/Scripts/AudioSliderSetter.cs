using UnityEngine;
using UnityEngine.UI;

public class AudioSliderSetter : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance.RegisterBgmSlider(bgmSlider);
        AudioManager.Instance.RegisterSfxSlider(sfxSlider);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
