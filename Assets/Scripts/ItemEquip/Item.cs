using UnityEngine;

public enum ItemType { Hat, Shades, Shoes }

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemID;
    public int price;
    public string itemName;
    public Sprite icon;
    public ItemType type;

    public string description;
    public string rarity;

    [Header("Regular Use")]
    public GameObject itemPrefab;              
    public GameObject leftShoePrefab;           
    public GameObject rightShoePrefab;          

    [Header("Battle Use")]
    public GameObject battleItemPrefab;         
    public GameObject battleLeftShoePrefab;     
    public GameObject battleRightShoePrefab;    
}

