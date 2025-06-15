using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public Transform gridParent;
    public GameObject itemUIPrefab;

    [Header("Equip Panel UI")]
    public GameObject equipPanel;
    public Image equipIcon;
    public Text equipItemName;
    public Button equipButton;
    public Text equipButtonText;

    private Item selectedItem;

    void Start()
    {
        ShowAll(); // Default
        equipPanel.SetActive(false); // Hide panel at start
    }

    public void PopulateInventory(ItemType? filter = null)
    {
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Item item in Inventory.Instance.collectedItems)
        {
            if (filter == null || item.type == filter)
            {
                GameObject go = Instantiate(itemUIPrefab, gridParent);
                ItemUI itemUI = go.GetComponent<ItemUI>();
                itemUI.Setup(item, this);
            }
        }
    }

    public void ShowEquipPanel(Item item)
    {
        selectedItem = item;

        equipPanel.SetActive(true);
        equipIcon.sprite = item.icon;
        equipItemName.text = item.itemName;

        bool isEquipped = PetEquipment.Instance.IsEquipped(item);
        equipButtonText.text = isEquipped ? "Unequip" : "Equip";

        equipButton.onClick.RemoveAllListeners();
        if (isEquipped)
        {
            equipButton.onClick.AddListener(() => {
                PetEquipment.Instance.UnequipItem(item);
                equipPanel.SetActive(false);
                PopulateInventory(); // Refresh UI
            });
        }
        else
        {
            equipButton.onClick.AddListener(() => {
                PetEquipment.Instance.EquipItem(item);
                equipPanel.SetActive(false);
                PopulateInventory();
            });
        }
    }

    public void CloseEquipPanel()
    {
        equipPanel.SetActive(false);
    }

    private bool IsItemOwned(Item item)
    {
        return PlayerPrefs.GetInt("ItemOwned_" + item.itemName, 0) == 1;
    }

    public void ShowAll() => PopulateInventory(null);
    public void ShowHats() => PopulateInventory(ItemType.Hat);
    public void ShowShades() => PopulateInventory(ItemType.Shades);
    public void ShowShoes() => PopulateInventory(ItemType.Shoes);
}
