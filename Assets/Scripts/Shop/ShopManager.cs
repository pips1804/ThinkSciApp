using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public GameObject shopItemPrefab;
    public Transform shopContentParent;
    public List<Item> allShopItems;
    public Text coinsText;

    public Text textAll;
    public Text textHats;
    public Text textShades;
    public Text textShoes;

    public Color activeTextColor = Color.green;
    public Color inactiveTextColor = Color.black;

    private int coins;
    private DatabaseManager db;

    public MainSceneUI mainmenu;

    public AudioClip purchase;
    public AudioClip click;

    public GameObject confirmPanel;
    public GameObject insufficientPanel;

    void Start()
    {
        db = FindObjectOfType<DatabaseManager>();
        if (db == null)
        {
            Debug.LogError("DatabaseManager not found in scene.");
            return;
        }

        coins = db.LoadPlayerStats();
        coinsText.text = "" +coins;
        textAll.color = activeTextColor;
        PopulateShop();
    }

    public void PopulateShop(ItemType? filterType = null)
    {
        foreach (Transform child in shopContentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Item item in allShopItems)
        {
            if (filterType == null || item.type == filterType)
            {
                GameObject go = Instantiate(shopItemPrefab, shopContentParent);
                ShopItemUI shopItemUI = go.GetComponent<ShopItemUI>();
                shopItemUI.Setup(item, this, confirmPanel, insufficientPanel);
            }
        }
    }


    public bool TryPurchase(Item item, int cost)
    {
        if (coins >= cost && !IsItemOwned(item))
        {
            coins -= cost;
            coinsText.text = coins.ToString();

            db.SavePlayerStats(coins);

            AudioManager.Instance.PlaySFX(purchase);

            mainmenu.UpdateUI();

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

    private void SetActiveButton(Text activeText)
    {
        textAll.color = inactiveTextColor;
        textHats.color = inactiveTextColor;
        textShades.color = inactiveTextColor;
        textShoes.color = inactiveTextColor;

        activeText.color = activeTextColor;
    }


    public void OnClickAll()
    {
        PopulateShop();
        SetActiveButton(textAll);
        AudioManager.Instance.PlaySFX(click);
    }

    public void OnClickHats()
    {
        PopulateShop(ItemType.Hat);
        SetActiveButton(textHats);
        AudioManager.Instance.PlaySFX(click);
    }

    public void OnClickShades()
    {
        PopulateShop(ItemType.Shades);
        SetActiveButton(textShades);
        AudioManager.Instance.PlaySFX(click);
    }

    public void OnClickShoes()
    {
        PopulateShop(ItemType.Shoes);
        SetActiveButton(textShoes);
        AudioManager.Instance.PlaySFX(click);
    }

    public bool HasEnoughCoins(int cost)
    {
        return coins >= cost;
    }

    public void RefreshCoinsUI()
    {
        coinsText.text = coins.ToString();
    }

}
