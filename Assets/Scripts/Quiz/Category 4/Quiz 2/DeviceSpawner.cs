using UnityEngine;
using UnityEngine.UI;

public class DeviceSpawner : MonoBehaviour
{
    public HeatHeroRescue HeatHeroRescue;
    public DeviceData[] devices;
    public Transform spawnArea;
    public GameObject devicePrefab;
    public Button yesButton;
    public Button noButton;

    private int currentIndex = 0;
    private GameObject currentDeviceGO;

    void Start()
    {
        currentIndex = 0; // start at first device
        SpawnDevice();
    }

    void SpawnDevice()
    {
        // If we've shown all devices, end spawning
        if (currentIndex >= devices.Length)
        {
            HeatHeroRescue.EndGame(true);
            return;
        }

        // Remove old device
        if (currentDeviceGO != null)
            Destroy(currentDeviceGO);

        // Get the next device
        DeviceData currentDeviceData = devices[currentIndex];

        // Spawn it
        currentDeviceGO = Instantiate(devicePrefab, spawnArea);
        currentDeviceGO.GetComponent<Image>().sprite = currentDeviceData.deviceSprite;

        // Clear old listeners
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        // Add listeners for YES and NO
        yesButton.onClick.AddListener(() =>
        {
            HeatHeroRescue.Answer(currentDeviceData.isHeatEnergyDevice, true);
            currentIndex++;
            SpawnDevice();
        });

        noButton.onClick.AddListener(() =>
        {
            HeatHeroRescue.Answer(currentDeviceData.isHeatEnergyDevice, false);
            currentIndex++;
            SpawnDevice();
        });
    }
}
