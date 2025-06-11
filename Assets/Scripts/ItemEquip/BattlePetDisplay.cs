using UnityEngine;

public class BattlePetDisplay : MonoBehaviour
{
    public PetEquipmentData equipmentData;
    public Transform hatSlot, shadesSlot, shoesSlot;

    void OnEnable()
    {
        RefreshEquippedItems();
    }

    public void RefreshEquippedItems()
    {
        ClearSlots();

        foreach (Item item in equipmentData.equippedItems)
        {
            EquipVisual(item);
        }
    }

    void EquipVisual(Item item)
    {
        Transform slot = GetSlot(item.type);
        if (slot == null || item.battleItemPrefab == null) return;

        GameObject instance = Instantiate(item.battleItemPrefab, slot);

        RectTransform rt = instance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.localPosition = Vector3.zero;
        }
    }

    void ClearSlots()
    {
        foreach (Transform t in hatSlot) Destroy(t.gameObject);
        foreach (Transform t in shadesSlot) Destroy(t.gameObject);
        foreach (Transform t in shoesSlot) Destroy(t.gameObject);
    }

    Transform GetSlot(ItemType type)
    {
        return type switch
        {
            ItemType.Hat => hatSlot,
            ItemType.Shades => shadesSlot,
            ItemType.Shoes => shoesSlot,
            _ => null,
        };
    }
}
