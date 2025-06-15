using UnityEngine;

public class MainMenuPetDisplay : MonoBehaviour
{
    public PetEquipmentData equipmentData;

    public Transform hatSlot, shadesSlot, shoesSlotLeft, shoesSlotRight;

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
        if (item.type == ItemType.Shoes)
        {
            if (item.leftShoePrefab && shoesSlotLeft)
            {
                GameObject left = Instantiate(item.leftShoePrefab, shoesSlotLeft);
                SetupRectTransform(left);
            }
            if (item.rightShoePrefab && shoesSlotRight)
            {
                GameObject right = Instantiate(item.rightShoePrefab, shoesSlotRight);
                SetupRectTransform(right);
            }
        }
        else
        {
            Transform slot = GetSlot(item.type);
            if (slot == null || item.itemPrefab == null) return;

            GameObject instance = Instantiate(item.itemPrefab, slot);
            SetupRectTransform(instance);
        }
    }

    void ClearSlots()
    {
        foreach (Transform t in hatSlot) Destroy(t.gameObject);
        foreach (Transform t in shadesSlot) Destroy(t.gameObject);
        foreach (Transform t in shoesSlotLeft) Destroy(t.gameObject);
        foreach (Transform t in shoesSlotRight) Destroy(t.gameObject);
    }

    Transform GetSlot(ItemType type)
    {
        return type switch
        {
            ItemType.Hat => hatSlot,
            ItemType.Shades => shadesSlot,
            _ => null,
        };
    }

    void SetupRectTransform(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
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
}
