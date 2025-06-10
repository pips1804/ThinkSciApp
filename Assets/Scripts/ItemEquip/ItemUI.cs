using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image iconImage;
    private Item item;
    private InventoryUI inventoryUI;

    public void Setup(Item newItem, InventoryUI ui)
    {
        item = newItem;
        inventoryUI = ui;
        iconImage.sprite = item.icon;
    }

    public void OnClick()
    {
        inventoryUI.ShowEquipPanel(item);
    }
}
