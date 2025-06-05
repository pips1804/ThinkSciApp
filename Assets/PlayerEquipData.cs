using UnityEngine;

public class PlayerEquipData : MonoBehaviour
{
    public static PlayerEquipData Instance;

    public bool isHatEquipped = false;
    public bool isGlassesEquipped = false;

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
}
