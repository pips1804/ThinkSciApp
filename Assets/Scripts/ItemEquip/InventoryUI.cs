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

    [Header("Filter Button Texts")]
    public Text textAll;
    public Text textHats;
    public Text textShades;
    public Text textShoes;

    public Color activeTextColor = Color.yellow;
    public Color inactiveTextColor = Color.white;

    private Item selectedItem;

    void Start()
    {
        SetActiveFilterText(textAll);
        ShowAll();
        equipPanel.SetActive(false);
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

    private void SetActiveFilterText(Text activeText)
    {
        Text[] allTexts = { textAll, textHats, textShades, textShoes };

        foreach (Text t in allTexts)
        {
            t.color = inactiveTextColor;
        }

        activeText.color = activeTextColor;
    }

    public void ShowAll()
    {
        PopulateInventory(null);
        SetActiveFilterText(textAll);
    }

    public void ShowHats()
    {
        PopulateInventory(ItemType.Hat);
        SetActiveFilterText(textHats);
    }

    public void ShowShades()
    {
        PopulateInventory(ItemType.Shades);
        SetActiveFilterText(textShades);
    }

    public void ShowShoes()
    {
        PopulateInventory(ItemType.Shoes);
        SetActiveFilterText(textShoes);
    }
}
