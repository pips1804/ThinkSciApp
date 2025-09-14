using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ShopManager : MonoBehaviour
{
    public Transform itemContainer;
    public GameObject itemPrefab;
    public DatabaseManager Database;
    private int currentUserId = 1;

    [Header("Player Info")]
    public Text coinsText;

    [Header("Buy Panel")]
    public GameObject buyPanel;
    public Text panelItemName;
    public Text panelItemDescription;
    public Button panelBuyButton;
    public Button panelCloseButton;

    [Header("Confirm Panel")]
    public GameObject confirmPanel;
    public Button confirmBuyButton;
    public Button confirmCloseButton;

    [Header("Error Panel")]
    public GameObject errorPanel;
    public Text errorText;               // <- add this in inspector
    public Button errorCloseButton;

    private int selectedItemId;
    private string selectedItemName;

    public Button allBtn;
    public Button foodBtn;
    public Button collectibleBtn;

    public LessonLocker lessonHandler;

    private string currentFilter = "All";

    void Start()
    {
        buyPanel.SetActive(false);
        confirmPanel.SetActive(false);
        errorPanel.SetActive(false);

        panelCloseButton.onClick.AddListener(() => buyPanel.SetActive(false));
        confirmCloseButton.onClick.AddListener(() => confirmPanel.SetActive(false));
        errorCloseButton.onClick.AddListener(() => errorPanel.SetActive(false));

        allBtn.onClick.AddListener(() => SetFilter("All"));
        foodBtn.onClick.AddListener(() => SetFilter("Food"));
        collectibleBtn.onClick.AddListener(() => SetFilter("Collectible"));

        LoadShopItems();
        UpdateCoinsDisplay();
    }

    void UpdateCoinsDisplay()
    {
        int coins = Database.LoadPlayerStats();
        coinsText.text = coins.ToString();
    }

    public void SetFilter(string filter)
    {
        currentFilter = filter;
        LoadShopItems();
    }

    void LoadShopItems()
    {
        // Clear old items
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // Get all items from database
        List<ItemData> items = Database.GetAllItems();

        // Apply filter
        if (currentFilter != "All")
        {
            items = items.FindAll(item => item.Type == currentFilter);
        }

        foreach (ItemData item in items)
        {
            GameObject go = Instantiate(itemPrefab, itemContainer);

            // Fill UI
            go.transform.Find("Name").GetComponent<Text>().text = item.Name;
            go.transform.Find("Type").GetComponent<Text>().text = item.Type;
            go.transform.Find("Price").GetComponent<Text>().text = item.Price.ToString();

            // Load sprite (from Resources/shop-items folder)
            if (!string.IsNullOrEmpty(item.SpritePath))
            {
                Sprite sprite = Resources.Load<Sprite>("shop-items/" + item.SpritePath);
                if (sprite != null)
                {
                    go.transform.Find("Icon").GetComponent<Image>().sprite = sprite;
                }
                else
                {
                    Debug.LogWarning($"Sprite not found at path: shop-items/{item.SpritePath}");
                }
            }

            // Setup Buy button
            Button buyBtn = go.transform.Find("BuyButton").GetComponent<Button>();
            int itemIdCopy = item.ItemId; // local copy for closure
            buyBtn.onClick.AddListener(() =>
            {
                ShowBuyPanel(item); // your existing ShowBuyPanel
            });
        }
    }

    void ShowBuyPanel(ItemData item)
    {
        selectedItemId = item.ItemId;
        selectedItemName = item.Name;

        panelItemName.text = item.Name;
        panelItemDescription.text = item.Description;

        buyPanel.SetActive(true);
        panelBuyButton.onClick.RemoveAllListeners();

        // ✅ Only block purchase if it's a collectible
        int ownedQty = Database.CheckIfOwned(currentUserId, selectedItemId);

        if (item.Type == "Collectible" && ownedQty > 0)
        {
            panelBuyButton.interactable = false;
            panelBuyButton.GetComponentInChildren<Text>().text = "Owned";
        }
        else
        {
            panelBuyButton.interactable = true;
            panelBuyButton.GetComponentInChildren<Text>().text = "Buy";

            panelBuyButton.onClick.AddListener(() =>
            {
                buyPanel.SetActive(false);
                ShowConfirmPanel(item);
            });
        }
    }

    void ShowConfirmPanel(ItemData item)
    {
        confirmPanel.SetActive(true);

        confirmBuyButton.onClick.RemoveAllListeners();
        confirmBuyButton.onClick.AddListener(() =>
        {
            int ownedQty = Database.CheckIfOwned(currentUserId, selectedItemId);

            if (item.Type == "Collectible" && ownedQty > 0)
            {
                panelBuyButton.interactable = false;
                panelBuyButton.GetComponentInChildren<Text>().text = "Owned";
            }
            else
            {
                panelBuyButton.interactable = true;
                panelBuyButton.GetComponentInChildren<Text>().text = "Buy";

                if (item.Type == "Food" && ownedQty > 0)
                {
                    panelItemDescription.text += $"\nYou already have {ownedQty} in your inventory.";
                }
            }

            bool success = Database.PurchaseItem(currentUserId, selectedItemId);
            confirmPanel.SetActive(false);

            if (success)
            {
                Debug.Log($"Successfully purchased {selectedItemName}");
                LoadShopItems();
                UpdateCoinsDisplay();
                lessonHandler.RefreshLessonLocks();
            }
            else
            {
                ShowErrorPanel("Not enough coins!");
            }
        });
    }

    void ShowErrorPanel(string message)
    {
        errorText.text = message;
        errorPanel.SetActive(true);
    }
}
