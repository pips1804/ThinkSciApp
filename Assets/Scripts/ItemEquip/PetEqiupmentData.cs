using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PetEquipmentData", menuName = "Inventory/Pet Equipment Data")]
public class PetEquipmentData : ScriptableObject
{
    public List<Item> equippedItems = new List<Item>();

    public void SetEquippedItem(Item item)
    {
        RemoveEquippedItem(item);
        equippedItems.RemoveAll(i => i.type == item.type);
        equippedItems.Add(item);
    }

    public void RemoveEquippedItem(Item item)
    {
        equippedItems.RemoveAll(i => i == item);
    }

    public bool IsEquipped(Item item)
    {
        return equippedItems.Contains(item);
    }
}
