using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public Transform itemContainer;
    public GameObject itemPrefab;
    public DatabaseManager Database;   // assign in Inspector
    private int currentUserId = 1;     // replace with actual user system

    void Start()
    {
        LoadShopItems();
    }

    void LoadShopItems()
    {
        // Clear old items
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // Get items from DatabaseManager
        List<ItemData> items = Database.GetAllItems();

        foreach (ItemData item in items)
        {
            GameObject go = Instantiate(itemPrefab, itemContainer);

            // Fill UI
            go.transform.Find("Name").GetComponent<Text>().text = item.Name;
            go.transform.Find("Type").GetComponent<Text>().text = item.Type;
            go.transform.Find("Price").GetComponent<Text>().text = item.Price.ToString();

            // Load sprite (from Resources/Items folder)
            if (!string.IsNullOrEmpty(item.SpritePath))
            {
                Sprite sprite = Resources.Load<Sprite>("shop-items/" + item.SpritePath);
                if (sprite != null)
                {
                    go.transform.Find("Icon").GetComponent<Image>().sprite = sprite;
                }
                else
                {
                    Debug.LogWarning($"Sprite not found at path: Items/{item.SpritePath}");
                }
            }

            // Setup Buy button
            Button buyBtn = go.transform.Find("BuyButton").GetComponent<Button>();
            int itemIdCopy = item.ItemId; // local copy for closure
            buyBtn.onClick.AddListener(() =>
            {
                bool success = Database.PurchaseItem(currentUserId, itemIdCopy);
                if (success)
                {
                    Debug.Log($"Successfully purchased {item.Name}");
                }
                else
                {
                    Debug.Log("Purchase failed (maybe not enough coins?)");
                }
            });
        }
    }
}
