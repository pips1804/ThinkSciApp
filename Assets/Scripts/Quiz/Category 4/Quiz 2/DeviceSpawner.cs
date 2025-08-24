using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DeviceSpawner : MonoBehaviour
{
    [Header("Game References")]
    public HeatHeroRescue HeatHeroRescue;
    public DeviceData[] devices;
    public Transform spawnArea;
    public GameObject devicePrefab;
    public Button yesButton;
    public Button noButton;

    [Header("Conveyor Belt Animation")]
    public float slideInDuration = 0.8f;
    public float slideOutDuration = 0.5f;
    public float conveyorDistance = 500f; // How far off-screen devices start
    public AnimationCurve slideInCurve;
    public AnimationCurve slideOutCurve;

    [Header("Visual Effects")]
    public ParticleSystem spawnParticles;
    public AudioSource audioSource;
    public AudioClip conveyorSound;
    public AudioClip deviceArriveSound;

    private int currentIndex = 0;
    private GameObject currentDeviceGO;
    private bool isAnimating = false;
    private RectTransform currentDeviceRect;

    void Start()
    {
        currentIndex = 0;

        // Initialize animation curves if not set in inspector
        if (slideInCurve == null || slideInCurve.keys.Length == 0)
        {
            slideInCurve = AnimationCurve.Linear(0, 0, 1, 1);
            // Add ease out effect
            slideInCurve.keys[0].outTangent = 0;
            slideInCurve.keys[1].inTangent = 0;
        }

        if (slideOutCurve == null || slideOutCurve.keys.Length == 0)
        {
            slideOutCurve = AnimationCurve.Linear(0, 0, 1, 1);
            // Add ease in effect
            slideOutCurve.keys[0].outTangent = 2;
            slideOutCurve.keys[1].inTangent = 2;
        }

        // Disable buttons during initial spawn
        SetButtonsInteractable(false);
        StartCoroutine(SpawnDeviceWithAnimation());
    }

    IEnumerator SpawnDeviceWithAnimation()
    {
        // If we've shown all devices, end spawning
        if (currentIndex >= devices.Length)
        {
            HeatHeroRescue.EndGame(true);
            yield break;
        }

        isAnimating = true;
        SetButtonsInteractable(false);

        // Slide out old device first (if exists)
        if (currentDeviceGO != null)
        {
            yield return StartCoroutine(SlideOutDevice());
        }

        // Get the next device data
        DeviceData currentDeviceData = devices[currentIndex];

        // Create new device off-screen (to the left)
        currentDeviceGO = Instantiate(devicePrefab, spawnArea);
        currentDeviceGO.GetComponent<Image>().sprite = currentDeviceData.deviceSprite;
        currentDeviceRect = currentDeviceGO.GetComponent<RectTransform>();

        // Set initial position (off-screen to the left)
        Vector2 offScreenPos = new Vector2(-conveyorDistance, 0);
        Vector2 centerPos = Vector2.zero;
        currentDeviceRect.anchoredPosition = offScreenPos;

        // Play conveyor sound
        PlaySound(conveyorSound);

        // Animate sliding in from left to center
        yield return StartCoroutine(SlideInDevice(offScreenPos, centerPos));

        // Play arrival sound and particles
        PlaySound(deviceArriveSound);
        if (spawnParticles != null)
        {
            spawnParticles.transform.position = currentDeviceGO.transform.position;
            spawnParticles.Play();
        }

        // Setup button listeners
        SetupButtonListeners(currentDeviceData);

        isAnimating = false;
        SetButtonsInteractable(true);
    }

    IEnumerator SlideInDevice(Vector2 startPos, Vector2 endPos)
    {
        if (currentDeviceRect == null) yield break;

        float elapsed = 0f;

        // Add some bounce/scale effect
        currentDeviceRect.localScale = Vector3.one * 0.8f;

        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideInDuration;
            float curveT = slideInCurve.Evaluate(t);

            // Position animation
            currentDeviceRect.anchoredPosition = Vector2.Lerp(startPos, endPos, curveT);

            // Scale animation (slight bounce effect)
            float scale = Mathf.Lerp(0.8f, 1f, curveT);
            currentDeviceRect.localScale = Vector3.one * scale;

            // Optional: Add slight rotation for more dynamic feel
            float rotation = Mathf.Sin(t * Mathf.PI) * 5f; // 5 degrees max swing
            currentDeviceRect.rotation = Quaternion.Euler(0, 0, rotation);

            yield return null;
        }

        // Ensure final values
        currentDeviceRect.anchoredPosition = endPos;
        currentDeviceRect.localScale = Vector3.one;
        currentDeviceRect.rotation = Quaternion.identity;
    }

    IEnumerator SlideOutDevice()
    {
        if (currentDeviceRect == null) yield break;

        Vector2 startPos = currentDeviceRect.anchoredPosition;
        Vector2 endPos = new Vector2(conveyorDistance, 0); // Slide to the right

        float elapsed = 0f;

        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideOutDuration;
            float curveT = slideOutCurve.Evaluate(t);

            // Position animation
            currentDeviceRect.anchoredPosition = Vector2.Lerp(startPos, endPos, curveT);

            // Scale down as it leaves
            float scale = Mathf.Lerp(1f, 0.7f, curveT);
            currentDeviceRect.localScale = Vector3.one * scale;

            // Fade out
            Image deviceImage = currentDeviceGO.GetComponent<Image>();
            if (deviceImage != null)
            {
                Color color = deviceImage.color;
                color.a = Mathf.Lerp(1f, 0f, curveT);
                deviceImage.color = color;
            }

            yield return null;
        }

        // Destroy the old device
        if (currentDeviceGO != null)
        {
            Destroy(currentDeviceGO);
            currentDeviceGO = null;
        }
    }

    void SetupButtonListeners(DeviceData deviceData)
    {
        // Clear old listeners
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        // Add new listeners with animation
        yesButton.onClick.AddListener(() =>
        {
            if (!isAnimating)
            {
                StartCoroutine(HandleAnswer(deviceData.isHeatEnergyDevice, true));
            }
        });

        noButton.onClick.AddListener(() =>
        {
            if (!isAnimating)
            {
                StartCoroutine(HandleAnswer(deviceData.isHeatEnergyDevice, false));
            }
        });
    }

    IEnumerator HandleAnswer(bool deviceIsHeat, bool playerAnswer)
    {
        isAnimating = true;
        SetButtonsInteractable(false);

        // Send answer to game controller
        HeatHeroRescue.Answer(deviceIsHeat, playerAnswer);

        // Add small delay for feedback
        yield return new WaitForSeconds(0.3f);

        // Move to next device
        currentIndex++;
        yield return StartCoroutine(SpawnDeviceWithAnimation());
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (yesButton != null) yesButton.interactable = interactable;
        if (noButton != null) noButton.interactable = interactable;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // ✨ Public method to restart the spawning (useful for replaying)
    public void RestartSpawning()
    {
        StopAllCoroutines();

        if (currentDeviceGO != null)
        {
            Destroy(currentDeviceGO);
        }

        currentIndex = 0;
        isAnimating = false;
        StartCoroutine(SpawnDeviceWithAnimation());
    }

    // ✨ Public method to skip current animation (for testing)
    public void SkipAnimation()
    {
        if (isAnimating && currentDeviceRect != null)
        {
            StopAllCoroutines();
            currentDeviceRect.anchoredPosition = Vector2.zero;
            currentDeviceRect.localScale = Vector3.one;
            currentDeviceRect.rotation = Quaternion.identity;
            isAnimating = false;
            SetButtonsInteractable(true);
        }
    }
}
