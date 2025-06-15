using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    public Image iconImage;

    private Item item;
    private ShopBuyPanel buyPanel;

    public void Setup(Item newItem, ShopBuyPanel panel)
    {
        item = newItem;
        buyPanel = panel;

        iconImage.sprite = item.icon;

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        buyPanel.Show(item);
    }
}
