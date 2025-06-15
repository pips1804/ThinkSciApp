using UnityEngine;
using System.Collections.Generic;

public class PetEquipment : MonoBehaviour
{
    public PetEquipmentData equipmentData;
    public Transform hatSlot, shadesSlot, shoesSlotLeft, shoesSlotRight;
    public static PetEquipment Instance;

    private Dictionary<ItemType, List<GameObject>> equippedItems = new Dictionary<ItemType, List<GameObject>>();

    void OnEnable()
    {
        LoadEquippedItems();
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
        if (item.type == ItemType.Shoes)
        {
            List<GameObject> shoes = new List<GameObject>();

            if (item.leftShoePrefab && shoesSlotLeft)
            {
                GameObject left = Instantiate(item.leftShoePrefab, shoesSlotLeft);
                SetupRectTransform(left);
                shoes.Add(left);
            }

            if (item.rightShoePrefab && shoesSlotRight)
            {
                GameObject right = Instantiate(item.rightShoePrefab, shoesSlotRight);
                SetupRectTransform(right);
                shoes.Add(right);
            }

            equippedItems[item.type] = shoes;
        }
        else
        {
            Transform slot = GetSlot(item.type);
            if (slot == null) return;

            GameObject instance = Instantiate(item.itemPrefab, slot);
            SetupRectTransform(instance);

            equippedItems[item.type] = new List<GameObject> { instance };
        }
    }

    private void ClearSlots()
    {
        foreach (Transform t in hatSlot) Destroy(t.gameObject);
        foreach (Transform t in shadesSlot) Destroy(t.gameObject);
        foreach (Transform t in shoesSlotLeft) Destroy(t.gameObject);
        foreach (Transform t in shoesSlotRight) Destroy(t.gameObject);
        equippedItems.Clear();
    }

    private Transform GetSlot(ItemType type)
    {
        return type switch
        {
            ItemType.Hat => hatSlot,
            ItemType.Shades => shadesSlot,
            _ => null,
        };
    }

    public void EquipItem(Item item)
    {
        UnequipItemType(item.type);
        EquipVisual(item);
        equipmentData.SetEquippedItem(item);
    }

    public void UnequipItem(Item item)
    {
        UnequipItemType(item.type);
        equipmentData.RemoveEquippedItem(item);
    }

    private void UnequipItemType(ItemType type)
    {
        if (equippedItems.TryGetValue(type, out List<GameObject> objs))
        {
            foreach (var go in objs)
                Destroy(go);

            equippedItems.Remove(type);
        }
    }

    private void SetupRectTransform(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
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

    public bool IsEquipped(Item item)
    {
        return equipmentData.IsEquipped(item);
    }
}
