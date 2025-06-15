using UnityEngine;
using UnityEngine.UI;

public class ShopBuyPanel : MonoBehaviour
{
    public Image iconImage;
    public Text nameText;
    public Text costText;
    public Button buyButton;
    public GameObject panel;

    private Item currentItem;
    private ShopManager shopManager;

    public void SetShopManager(ShopManager manager)
    {
        shopManager = manager;
    }

    public void Show(Item item)
    {
        currentItem = item;
        iconImage.sprite = item.icon;
        nameText.text = item.itemName;

        int cost = GetItemCost(item);
        costText.text = "Cost: " + cost.ToString() + " coins";

        panel.SetActive(true);

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => BuyItem(item, cost));
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private void BuyItem(Item item, int cost)
    {
        if (shopManager != null && shopManager.TryPurchase(item, cost))
        {
            Debug.Log("Item purchased!");
            InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
                inventoryUI.PopulateInventory();
        }
        else
        {
            Debug.Log("Not enough coins!");
        }

        Hide();
    }

    private int GetItemCost(Item item)
    {
        return item.type switch
        {
            ItemType.Hat => 100,
            ItemType.Shades => 150,
            ItemType.Shoes => 200,
            _ => 100
        };
    }
}
