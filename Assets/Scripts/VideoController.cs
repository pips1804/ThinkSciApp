using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button playPauseButton, fullscreenButton;
    public Image playPauseIcon;
    public Sprite playSprite, pauseSprite;

    public Slider progressSlider;
    public Text currentTimeText, remainingTimeText;

    public Button centerPlayButton;

    // ?? NEW: Fullscreen support
    public RectTransform videoPanel;
    private bool isFullscreen = false;
    private Vector2 originalSize;
    private Vector2 originalPosition;
    private Vector2 originalAnchorMin, originalAnchorMax;

    private bool isDragging = false;

    public Button takeQuizButton;

    public AudioSource bgMusic;

    void Start()
    {
        playPauseButton.onClick.AddListener(TogglePlayPause);
        fullscreenButton.onClick.AddListener(ToggleFullscreen);

        progressSlider.onValueChanged.AddListener(OnSliderChanged);

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();

        UpdatePlayPauseIcon(); // Set initial icon

        centerPlayButton.onClick.AddListener(OnCenterPlayClicked);
        centerPlayButton.gameObject.SetActive(true); // Ensure it's visible initially

        videoPlayer.loopPointReached += OnVideoFinished;

        // ?? Save original layout for fullscreen toggle
        originalSize = videoPanel.sizeDelta;
        originalPosition = videoPanel.anchoredPosition;
        originalAnchorMin = videoPanel.anchorMin;
        originalAnchorMax = videoPanel.anchorMax;

        takeQuizButton.interactable = true; // Enable quiz button at start
        // takeQuizButton.interactable = false; // Disable quiz button at start
    }

    void Update()
    {
        if (videoPlayer.isPlaying && !isDragging)
        {
            progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);
            UpdateTimeUI();
        }
    }

    void TogglePlayPause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            StartCoroutine(FadeAudio(bgMusic, 1f, 1f)); // Fade in over 1 sec
        }
        else
        {
            videoPlayer.Play();
            StartCoroutine(FadeAudio(bgMusic, 0f, 1f)); // Fade out over 1 sec
        }

        UpdatePlayPauseIcon();
    }



    void UpdatePlayPauseIcon()
    {
        if (videoPlayer.isPlaying)
            playPauseIcon.sprite = pauseSprite;
        else
            playPauseIcon.sprite = playSprite;
    }

    void OnSliderChanged(float value)
    {
        if (isDragging)
            videoPlayer.time = value * videoPlayer.length;
    }

    public void OnBeginDrag() => isDragging = true;
    public void OnEndDrag() => isDragging = false;

    void UpdateTimeUI()
    {
        currentTimeText.text = FormatTime(videoPlayer.time);
        remainingTimeText.text = FormatTime(videoPlayer.length - videoPlayer.time);
    }

    string FormatTime(double time)
    {
        int minutes = Mathf.FloorToInt((float)time / 60);
        int seconds = Mathf.FloorToInt((float)time % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }

    void OnCenterPlayClicked()
    {
        videoPlayer.Play();
        StartCoroutine(FadeAudio(bgMusic, 0f, 1f)); // Fade out
        UpdatePlayPauseIcon();
        centerPlayButton.gameObject.SetActive(false);
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        centerPlayButton.gameObject.SetActive(true);
        // takeQuizButton.interactable = true;
        StartCoroutine(FadeAudio(bgMusic, 1f, 1f)); // Fade in
    }

    void OnVideoPrepared(VideoPlayer vp) => UpdateTimeUI();

    // ? NEW: Simulated fullscreen toggle for mobile
    void ToggleFullscreen()
    {
        if (!isFullscreen)
        {
            // Expand to full canvas
            videoPanel.anchorMin = new Vector2(0, 0);
            videoPanel.anchorMax = new Vector2(1, 1);
            videoPanel.pivot = new Vector2(0, 0);
            videoPanel.anchoredPosition = Vector2.zero;
            videoPanel.offsetMin = Vector2.zero;
            videoPanel.offsetMax = Vector2.zero;

            isFullscreen = true;

            Screen.orientation = ScreenOrientation.LandscapeLeft; // Optional
        }
        else
        {
            // Restore original layout
            videoPanel.anchorMin = originalAnchorMin;
            videoPanel.anchorMax = originalAnchorMax;
            videoPanel.pivot = new Vector2(0.5f, 0.5f); // if originally centered
            videoPanel.sizeDelta = originalSize;
            videoPanel.anchoredPosition = originalPosition;
            isFullscreen = false;

            Screen.orientation = ScreenOrientation.Portrait; // Optional
        }
    }

    IEnumerator FadeAudio(AudioSource audioSource, float targetVolume, float duration)
    {
        float currentTime = 0f;
        float startVolume = audioSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;

        if (Mathf.Approximately(targetVolume, 0f))
        {
            audioSource.Pause(); // Pause fully when volume is 0
        }
        else if (!audioSource.isPlaying)
        {
            audioSource.Play(); // Resume only if not already playing
        }
    }

}
