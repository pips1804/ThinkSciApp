using UnityEngine;
using System.Collections.Generic;

public class PetEquipment : MonoBehaviour
{
    public PetEquipmentData equipmentData;
    public Transform hatSlot, shadesSlot, shoesSlot;
    public static PetEquipment Instance;

    private Dictionary<ItemType, GameObject> equippedItems = new Dictionary<ItemType, GameObject>();

    void OnEnable()
    {
        LoadEquippedItems(); // Automatically refresh when pet room panel is shown
    }

    void Awake()
    {
        Instance = this;
    }

    public void LoadEquippedItems()
    {
        ClearSlots();

        foreach (Item item in equipmentData.equippedItems)
        {
            EquipVisual(item);
        }
    }

    private void EquipVisual(Item item)
    {
        Transform slot = GetSlot(item.type);
        if (slot == null) return;

        GameObject instance = Instantiate(item.itemPrefab, slot);
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

        equippedItems[item.type] = instance;
    }

    private void ClearSlots()
    {
        foreach (Transform t in hatSlot) Destroy(t.gameObject);
        foreach (Transform t in shadesSlot) Destroy(t.gameObject);
        foreach (Transform t in shoesSlot) Destroy(t.gameObject);
        equippedItems.Clear();
    }

    private Transform GetSlot(ItemType type)
    {
        return type switch
        {
            ItemType.Hat => hatSlot,
            ItemType.Shades => shadesSlot,
            ItemType.Shoes => shoesSlot,
            _ => null,
        };
    }

    public void EquipItem(Item item)
    {
        UnequipItemType(item.type);

        Transform slot = GetSlot(item.type);
        if (slot == null) return;

        GameObject equipped = Instantiate(item.itemPrefab, slot);
        RectTransform rt = equipped.GetComponent<RectTransform>();

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

        equippedItems[item.type] = equipped;
        equipmentData.SetEquippedItem(item);
    }

    public void UnequipItem(Item item)
    {
        if (equippedItems.ContainsKey(item.type))
        {
            Destroy(equippedItems[item.type]);
            equippedItems.Remove(item.type);
        }

        equipmentData.RemoveEquippedItem(item);
    }

    private void UnequipItemType(ItemType type)
    {
        if (equippedItems.TryGetValue(type, out GameObject go))
        {
            Destroy(go);
            equippedItems.Remove(type);
        }
    }

    public bool IsEquipped(Item item)
    {
        return equipmentData.IsEquipped(item);
    }
}

