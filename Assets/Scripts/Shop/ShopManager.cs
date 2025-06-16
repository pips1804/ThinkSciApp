using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public GameObject shopItemPrefab;
    public Transform shopContentParent;
    public List<Item> allShopItems;
    public Text coinsText;
    public ShopBuyPanel buyPanel;

    private int coins;
    private DatabaseManager db;

    void Start()
    {
        buyPanel.SetShopManager(this);

        db = FindObjectOfType<DatabaseManager>();
        if (db == null)
        {
            Debug.LogError("DatabaseManager not found in scene.");
            return;
        }

        coins = db.LoadPlayerStats();
        coinsText.text = "Coins: " + coins;
        PopulateShop();
    }

    public void PopulateShop()
    {
        foreach (Item item in allShopItems)
        {
            GameObject go = Instantiate(shopItemPrefab, shopContentParent);
            ShopItemUI shopItemUI = go.GetComponent<ShopItemUI>();
            shopItemUI.Setup(item, buyPanel);
        }
    }

    public bool TryPurchase(Item item, int cost)
    {
        if (coins >= cost && !IsItemOwned(item))
        {
            coins -= cost;
            coinsText.text = "Coins: " + coins;

            db.SavePlayerStats(coins);

            PlayerPrefs.SetInt("item_" + item.itemID, 1);
            PlayerPrefs.Save();

            return true;
        }
        return false;
    }

    public bool IsItemOwned(Item item)
    {
        return PlayerPrefs.GetInt("item_" + item.itemID, 0) == 1;
    }
}
