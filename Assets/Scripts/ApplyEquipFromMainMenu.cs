using UnityEngine;

public class ApplyEquipFromMainMenu : MonoBehaviour
{
    public Transform hatSlot;
    public Transform shadesSlot;

    public Vector3 hatScale = new Vector3(0.5f, 0.5f, 0.5f);      // Adjust based on quiz size
    public Vector3 shadesScale = new Vector3(0.8f, 0.8f, 0.8f);  // Adjust as needed

    void Start()
    {
        if (PlayerEquipData.Instance == null) return;

        if (PlayerEquipData.Instance.isHatEquipped && PlayerEquipData.Instance.equippedHatPrefab != null)
        {
            GameObject newHat = Instantiate(PlayerEquipData.Instance.equippedHatPrefab, hatSlot);
            RectTransform rt = newHat.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = hatScale;
        }

        if (PlayerEquipData.Instance.isGlassesEquipped && PlayerEquipData.Instance.equippedShadesPrefab != null)
        {
            GameObject newShades = Instantiate(PlayerEquipData.Instance.equippedShadesPrefab, shadesSlot);
            RectTransform rt = newShades.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = shadesScale;
        }
    }
}

