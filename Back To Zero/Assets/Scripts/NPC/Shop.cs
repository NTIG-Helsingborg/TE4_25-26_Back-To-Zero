using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    public ItemSO itemSO;
    public TMP_Text itemNameText;
    public Image itemImage;
    public TMP_Text priceText;

    [SerializeField] private ShopManager shopManager;

    private int price;



    public void Initialize(ItemSO newItemSO, int price)
    {
        itemSO = newItemSO;
        itemNameText.text = itemSO.itemName;
        itemImage.sprite = itemSO.itemSprite;
        this.price = price;
        priceText.text = price.ToString() + " G";

    }

    public void OnBuyButtonClicked()
    {
       shopManager.TryBuyItem(itemSO, price);
    }
}
