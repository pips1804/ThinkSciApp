using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image iconImage;
    public GameObject lockOverlay;

    private Item item;
    private InventoryUI inventoryUI;
    private bool isUnlocked;

    public void Setup(Item newItem, InventoryUI ui)
    {
        item = newItem;
        inventoryUI = ui;
        iconImage.sprite = item.icon;

        isUnlocked = PlayerPrefs.GetInt("item_" + item.itemID, 0) == 1;

        if (lockOverlay != null)
            lockOverlay.SetActive(!isUnlocked);

        Button button = GetComponent<Button>();
        if (button != null)
            button.interactable = isUnlocked;
    }


    public void OnClick()
    {
        if (isUnlocked)
        {
            inventoryUI.ShowEquipPanel(item);
        }
    }
}
