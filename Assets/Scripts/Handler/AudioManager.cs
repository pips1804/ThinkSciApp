using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource bgmSource;
    public AudioSource sfxSource;

    // List of all sliders in the scene
    public List<Slider> bgmSliders = new List<Slider>();
    public List<Slider> sfxSliders = new List<Slider>();

    private void Awake()
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

    private void Start()
    {
        if (!bgmSource.isPlaying)
        {
            bgmSource.loop = true;
            bgmSource.Play(); // Make sure a clip is assigned in Inspector
        }
        LoadVolumeSettings();
    }

    public void RegisterBgmSlider(Slider slider)
    {
        bgmSliders.Add(slider);
        slider.value = bgmSource.volume;
        slider.onValueChanged.AddListener(SetBGMVolume);
    }

    public void RegisterSfxSlider(Slider slider)
    {
        sfxSliders.Add(slider);
        slider.value = sfxSource.volume;
        slider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
        PlayerPrefs.SetFloat("BGMVolume", volume);

        foreach (Slider s in bgmSliders)
            if (s.value != volume) s.value = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);

        foreach (Slider s in sfxSliders)
            if (s.value != volume) s.value = volume;
    }

    void LoadVolumeSettings()
    {
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }
}
