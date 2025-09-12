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

    private int currentUserId = 1;
    private List<ItemData> ownedItems = new List<ItemData>();
    private string currentFilter = "All";

    void Start()
    {
        // Hook up buttons
        allBtn.onClick.AddListener(() => SetFilter("All"));
        foodBtn.onClick.AddListener(() => SetFilter("Food"));
        collectibleBtn.onClick.AddListener(() => SetFilter("Collectible"));

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

        List<ItemData> filtered = new List<ItemData>(ownedItems);

        // Apply filter
        if (currentFilter != "All")
        {
            filtered = ownedItems.FindAll(item => item.Type == currentFilter);
        }

        foreach (ItemData item in filtered)
        {
            GameObject prefabToUse = (item.Type.ToLower() == "food") ? foodItemPrefab : collectibleItemPrefab;
            GameObject go = Instantiate(prefabToUse, ownedItemContainer);

            go.transform.Find("Name").GetComponent<Text>().text = item.Name;
            go.transform.Find("Type").GetComponent<Text>().text = item.Type;

            if (!string.IsNullOrEmpty(item.SpritePath))
            {
                Sprite sprite = Resources.Load<Sprite>("shop-items/" + item.SpritePath);
                if (sprite != null)
                    go.transform.Find("Icon").GetComponent<Image>().sprite = sprite;
            }
        }
    }
}
