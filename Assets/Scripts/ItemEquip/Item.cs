using UnityEngine;

public enum ItemType { Hat, Shades, Shoes }

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType type;
    public GameObject itemPrefab;

}
