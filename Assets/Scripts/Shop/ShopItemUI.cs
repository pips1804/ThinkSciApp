using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    public Image iconImage;
    public Text nameText;
    public Text priceText;
    public Text ownedText;
    public Button viewButton;

    private Item item;
    private ShopManager shopManager;

    private GameObject confirmPanel;
    private GameObject insufficientPanel;

    public GameObject itemDetailModal;
    public Text detailDescriptionText;
    public Text detailRarityText;
    public Button detailBuyButton;
    public Button detailCloseButton;

    public void Setup(Item newItem, ShopManager manager, GameObject confirm, GameObject insufficient)
    {
        item = newItem;
        shopManager = manager;
        confirmPanel = confirm;
        insufficientPanel = insufficient;

        iconImage.sprite = item.icon;
        nameText.text = item.itemName;
        priceText.text = item.price + " coins";

        RefreshOwnershipUI();

        viewButton.onClick.RemoveAllListeners();
        viewButton.onClick.AddListener(ShowItemDetailModal);
    }

    private void RefreshOwnershipUI()
    {
        bool isOwned = PlayerPrefs.GetInt("item_" + item.itemID, 0) == 1;
        ownedText.text = isOwned ? "Owned" : "Not Owned";
        viewButton.interactable = true;
    }

    private void ShowItemDetailModal()
    {
        itemDetailModal.SetActive(true);
        detailDescriptionText.text = item.description;
        detailRarityText.text = "" + item.rarity;

        bool isOwned = PlayerPrefs.GetInt("item_" + item.itemID, 0) == 1;

        if (isOwned)
        {
            detailBuyButton.interactable = false;
            detailBuyButton.GetComponentInChildren<Text>().text = "Owned";
        }
        else
        {
            detailBuyButton.interactable = true;
            detailBuyButton.GetComponentInChildren<Text>().text = "Buy";
        }

        detailBuyButton.onClick.RemoveAllListeners();
        detailBuyButton.onClick.AddListener(OnBuyClickFromModal);

        detailCloseButton.onClick.RemoveAllListeners();
        detailCloseButton.onClick.AddListener(() =>
        {
            itemDetailModal.SetActive(false);
        });
    }



    private void OnBuyClickFromModal()
    {
        if (shopManager.HasEnoughCoins(item.price))
        {
            ShowConfirmPanel();
            itemDetailModal.SetActive(false);
        }
        else
        {
            insufficientPanel.SetActive(true);
            itemDetailModal.SetActive(false);
        }
    }

    private void ShowConfirmPanel()
    {
        confirmPanel.SetActive(true);

        Button confirmBtn = confirmPanel.GetComponentInChildren<Button>();
        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(() =>
            {
                if (shopManager.TryPurchase(item, item.price))
                {
                    PlayerPrefs.SetInt("item_" + item.itemID, 1);
                    PlayerPrefs.Save();

                    RefreshOwnershipUI();
                    shopManager.RefreshCoinsUI();
                    confirmPanel.SetActive(false);
                    itemDetailModal.SetActive(false);
                }
            });
        }
    }
}
