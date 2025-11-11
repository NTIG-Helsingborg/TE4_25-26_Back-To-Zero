using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Shop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public ItemSO itemSO;
    public TMP_Text itemNameText;
    public Image itemImage;
    public TMP_Text priceText;

    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopInfo shopInfo;

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

    public void OnPointerEnter(PointerEventData eventData)
    {
         if(itemSO != null){
             shopInfo.ShowItemInfo(itemSO);}
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        shopInfo.HideItemInfo();
    }
    public void OnPointerMove(PointerEventData eventData)
    {
        if(itemSO != null)
        {
            shopInfo.FollowMouse();
        }
    }
}
