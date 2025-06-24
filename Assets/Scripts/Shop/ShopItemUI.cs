using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    public Image iconImage;
    public Text nameText;
    public Text priceText;
    public Text ownedText;
    public Button buyButton;

    private Item item;
    private ShopManager shopManager;

    private GameObject confirmPanel;
    private GameObject insufficientPanel;

    public AudioClip click;

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

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClick);
    }

    private void RefreshOwnershipUI()
    {
        bool isOwned = PlayerPrefs.GetInt("item_" + item.itemID, 0) == 1;
        ownedText.text = isOwned ? "Owned" : "Not Owned";
        buyButton.interactable = !isOwned;
    }

    private void OnBuyClick()
    {
        if (shopManager.HasEnoughCoins(item.price))
        {
            ShowConfirmPanel();
        }
        else
        {
            insufficientPanel.SetActive(true);
        }

        AudioManager.Instance.PlaySFX(click);
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
                }
            });
        }
    }
}
