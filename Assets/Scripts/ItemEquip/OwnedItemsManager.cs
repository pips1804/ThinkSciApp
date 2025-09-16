using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OwnedItemsManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform ownedItemContainer;

    [Header("Prefabs")]
    public GameObject foodItemPrefab;
    public GameObject collectibleItemPrefab;

    [Header("Database")]
    public DatabaseManager Database;

    [Header("Filter Buttons")]
    public Button allBtn;
    public Button foodBtn;
    public Button collectibleBtn;

    [Header("Item Modal")]
    public GameObject itemModalPanel;
    public Text modalItemName;
    public Text modalItemDescription;
    public Button modalCloseButton;

    [Header("Use Food Confirmation")]
    public GameObject useFoodConfirmPanel;
    public Text confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private int currentUserId = 1;
    private List<ItemData> ownedItems = new List<ItemData>();
    private string currentFilter = "All";

    void Start()
    {
        // Hook up buttons
        allBtn.onClick.AddListener(() => SetFilter("All"));
        foodBtn.onClick.AddListener(() => SetFilter("Food"));
        collectibleBtn.onClick.AddListener(() => SetFilter("Collectible"));

        useFoodConfirmPanel.SetActive(false);
        confirmNoButton.onClick.AddListener(() => useFoodConfirmPanel.SetActive(false));

        LoadOwnedItems();
    }

    void OnEnable()
    {
        LoadOwnedItems();
    }

    public void SetFilter(string filter)
    {
        currentFilter = filter;
        LoadOwnedItems();
    }

    public void LoadOwnedItems()
    {
        // Clear existing items
        foreach (Transform child in ownedItemContainer)
            Destroy(child.gameObject);

        // Load owned items from DB
        ownedItems = Database.GetUserItems(currentUserId);

        DisplayItems();
    }
    private void DisplayItems()
    {
        // Clear container first
        foreach (Transform child in ownedItemContainer)
            Destroy(child.gameObject);

        // Apply filter
        List<ItemData> filtered;
        if (currentFilter.ToLower() == "all")
        {
            filtered = new List<ItemData>(ownedItems);
        }
        else
        {
            filtered = ownedItems.FindAll(item => item.Type.ToLower() == currentFilter.ToLower());
        }

        foreach (ItemData item in filtered)
        {
            // 👇 Skip items with zero quantity
            if (item.Quantity <= 0)
                continue;

            GameObject prefabToUse = (item.Type.ToLower() == "food") ? foodItemPrefab : collectibleItemPrefab;
            GameObject go = Instantiate(prefabToUse, ownedItemContainer);

            // Set UI fields
            go.transform.Find("Name").GetComponent<Text>().text = item.Name;
            go.transform.Find("Type").GetComponent<Text>().text = item.Type;

            // Show quantity
            Text qtyText = go.transform.Find("Quantity")?.GetComponent<Text>();
            if (qtyText != null)
            {
                if (item.Type.ToLower() == "food" || item.Quantity > 1)
                    qtyText.text = "x" + item.Quantity;
                else
                    qtyText.text = "";
            }

            if (item.Type.ToLower() == "food")
            {
                Button useButton = go.transform.Find("UseButton")?.GetComponent<Button>();
                if (useButton != null)
                {
                    ItemData capturedItem = item;
                    useButton.onClick.RemoveAllListeners();
                    useButton.onClick.AddListener(() =>
                    {
                        ShowUseFoodConfirm(capturedItem);
                    });
                }
            }

            // Load sprite
            if (!string.IsNullOrEmpty(item.SpritePath))
            {
                Sprite sprite = Resources.Load<Sprite>("shop-items/" + item.SpritePath);
                if (sprite != null)
                    go.transform.Find("Icon").GetComponent<Image>().sprite = sprite;
            }

            // Add click listener for modal
            Button itemButton = go.GetComponent<Button>();
            if (itemButton != null)
            {
                ItemData capturedItem = item;
                itemButton.onClick.AddListener(() =>
                {
                    ShowItemModal(capturedItem);
                });
            }
        }
    }
    private void ShowItemModal(ItemData item)
    {
        modalItemName.text = item.Name;
        modalItemDescription.text = item.Description;

        if (item.Type.ToLower() == "food")
            modalItemDescription.text += $"\nRestores {item.EnergyValue} Energy";

        itemModalPanel.SetActive(true);
    }
    private void ShowUseFoodConfirm(ItemData item)
    {
        confirmText.text = $"Use {item.Name} to gain {item.EnergyValue} Energy?";
        useFoodConfirmPanel.SetActive(true);

        // Remove old listeners
        confirmYesButton.onClick.RemoveAllListeners();

        confirmYesButton.onClick.AddListener(() =>
        {
            useFoodConfirmPanel.SetActive(false);
            UseFoodItem(item);
        });
    }
    private void UseFoodItem(ItemData item)
    {
        if (item.Quantity <= 0)
        {
            // Optional: Show error modal or message
            ShowErrorPanel("You have no more of this item!");
            return;
        }

        // 1. Increase energy based on food
        Database.AddEnergy(currentUserId, item.EnergyValue);

        // 2. Reduce quantity
        Database.ReduceItemQuantity(currentUserId, item.ItemId, 1);

        // 3. Reload inventory
        LoadOwnedItems();
    }
    public void ShowErrorPanel(string message)
    {
        // Simple error modal implementation
        itemModalPanel.SetActive(true);
        modalItemName.text = "Error";
        modalItemDescription.text = message;
    }
    public void CloseItemModal()
    {
        itemModalPanel.SetActive(false);
    }
}
