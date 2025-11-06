using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class ItemSlot : MonoBehaviour
{
    //Item Data//
    public string itemName;
    public int quantity;
    public Sprite itemSprite;
    public bool isFull;


    //Item Slot//
    [SerializeField] 
    private TMP_Text quantityText;

    [SerializeField]
    private Image itemImage;


    public void AddItem(string itemName, Sprite itemIcon, int quantity){
        this.itemName = itemName;
        this.itemSprite = itemIcon;
        this.quantity = quantity;
        isFull = true;

        quantityText.text = quantity.ToString();
        quantityText.enabled = true;
        itemImage.enabled = true;
        itemImage.sprite = itemIcon;
        
    }
}
