using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    //Item Data//
    public string itemName;
    public int quantity;
    public Sprite itemSprite;
    public bool isFull;
    public string itemDescription;

    [SerializeField]
    private int maxNumberOfItems;


    //Item Slot//
    [SerializeField] 
    private TMP_Text quantityText;

    [SerializeField]
    private Image itemImage;

    //Item Description Slot//
    public Image itemDescriptionImage;
    public TMP_Text ItemDescriptionNameText;
    public TMP_Text ItemDescriptionText;

    public GameObject selectedShader;
    public bool thisItemSelected;

    private InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = GameObject.Find("Player").GetComponent<InventoryManager>();
    }


    public int AddItem(string itemName, Sprite itemIcon, int quantity, string itemDescription){
        //Check to see if we can stack items//
        if(isFull)
           return quantity;

        this.itemName = itemName;
        this.itemSprite = itemIcon;
        this.itemDescription = itemDescription;

        this.quantity += quantity;
        if(this.quantity >= maxNumberOfItems){
            quantityText.text = maxNumberOfItems.ToString();
            quantityText.enabled = true;
            itemImage.sprite = itemIcon;
            itemImage.enabled = true;
            isFull = true;
        
            int extraItems = this.quantity - maxNumberOfItems;
            this.quantity = maxNumberOfItems;
            return extraItems;
        }

        quantityText.text = this.quantity.ToString();
        quantityText.enabled = true;
        itemImage.sprite = itemIcon;
        itemImage.enabled = true;

        return 0;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
           OnLeftClick();
        }
         if(eventData.button == PointerEventData.InputButton.Right)
        {
           OnRightClick();
        }
    }

    public void OnLeftClick()
    {
        if (thisItemSelected)
        {
            bool usable = inventoryManager.UseItem(itemName);
            if(usable){
                this.quantity -= 1;
                quantityText.text = this.quantity.ToString();
                if(this.quantity <= 0)
                     EmptySlot();
            }
        }
        else
        {
            inventoryManager.DeselectAllSlots();
            selectedShader.SetActive(true);
            thisItemSelected = true;
            ItemDescriptionNameText.text = itemName;
            ItemDescriptionText.text = itemDescription;
            itemDescriptionImage.sprite = itemSprite;
        }
    }

    public void EmptySlot(){
        // Clear visuals
        quantityText.enabled = false;
        itemImage.enabled = false;
        ItemDescriptionNameText.text = "";
        ItemDescriptionText.text = "";
        itemDescriptionImage.sprite = null;

        // Clear internal data to avoid stale values (prevents selecting/using an empty slot)
        itemName = string.Empty;
        itemSprite = null;
        itemDescription = string.Empty;
        quantity = 0;
        isFull = false;
        thisItemSelected = false;
        selectedShader.SetActive(false);
    }

    // Update the quantity display text
    public void UpdateQuantityDisplay()
    {
        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
            quantityText.enabled = quantity > 0;
        }
    }


    public void OnRightClick()
    {
        // Implement right-click functionality here
    }

}
