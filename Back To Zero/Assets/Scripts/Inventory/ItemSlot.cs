using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    //Item Data//
    public string itemName;
    public int quantity;
    public Sprite itemSprite;
    public bool isFull;
    public string itemDescription;

    [SerializeField]
    private int maxNumberOfItems;

    
    [SerializeField] private GameObject[] swapPanels; 
    


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

    [SerializeField] 
    private SlotType slotType = SlotType.Regular; // Type of slot (regular, artifact, or ability)

    private InventoryManager inventoryManager;
    
    [SerializeField] private ShopInfo itemInfoPopup; // Reference to the hover popup

    private void Start()
    {
        inventoryManager = GameObject.Find("Player").GetComponent<InventoryManager>();
        
        // Auto-find itemInfoPopup if not assigned
        if (itemInfoPopup == null)
        {
            itemInfoPopup = FindFirstObjectByType<ShopInfo>();
            if (itemInfoPopup == null)
            {
                Debug.LogWarning($"ItemSlot '{gameObject.name}': No ShopInfo component found in scene! Hover functionality will not work.");
            }
            else
            {
                Debug.Log($"ItemSlot '{gameObject.name}': Auto-assigned ShopInfo component.");
            }
        }
        
        // Validate that we have an Image component for raycasting
        Image img = GetComponent<Image>();
        if (img == null)
        {
            Debug.LogWarning($"ItemSlot '{gameObject.name}': No Image component found! Add an Image component for hover detection.");
        }
        else if (!img.raycastTarget)
        {
            Debug.LogWarning($"ItemSlot '{gameObject.name}': Image raycastTarget is disabled! Enable it for hover detection.");
        }
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
            // If this is an ability slot, the item stays selected for transfer to active slot
            if (slotType == SlotType.Ability && !string.IsNullOrEmpty(itemName))
            {
                // Keep it selected, do nothing - user needs to click an active slot
                Debug.Log($"Ability slot '{itemName}' selected. Click an active slot to equip it.");
            }
            else
            {
                bool usable = inventoryManager.UseItem(itemName);
                if(usable){
                    this.quantity -= 1;
                    quantityText.text = this.quantity.ToString();
                    if(this.quantity <= 0)
                         EmptySlot();
                }
            }
            
            
        }
        else
        {
            // Deselect all slots of the same type
            switch (slotType)
            {
                case SlotType.Regular:
                    inventoryManager.DeselectAllSlots();
                    break;
                case SlotType.Artifact:
                    inventoryManager.DeselectAllArtifactSlots();
                    break;
                case SlotType.Ability:
                    inventoryManager.DeselectAllAbilitySlots();
                    break;
            }
            
            if (selectedShader != null || (swapPanels != null && swapPanels.Length > 0))
            {
                if (selectedShader != null) selectedShader.SetActive(true);
                ActivateSwapPanels(true);
            }
            thisItemSelected = true;
            
            // If this is an ability slot, register it as selected in the manager
            if (slotType == SlotType.Ability && !string.IsNullOrEmpty(itemName))
            {
                inventoryManager.SetSelectedAbilitySlot(this);
            }
            // If this is an artifact slot, register it as selected in the manager
            else if (slotType == SlotType.Artifact && !string.IsNullOrEmpty(itemName))
            {
                inventoryManager.SetSelectedArtifactSlot(this);
            }
            
            if (ItemDescriptionNameText != null)
            {
                ItemDescriptionNameText.text = itemName;
            }
            if (ItemDescriptionText != null)
            {
                ItemDescriptionText.text = itemDescription;
            }
            if (itemDescriptionImage != null)
            {
                itemDescriptionImage.sprite = itemSprite;
            }
           
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
    if (selectedShader != null) selectedShader.SetActive(false);
    ActivateSwapPanels(false);
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"ItemSlot '{gameObject.name}': Pointer entered. Item: '{itemName}', Popup: {(itemInfoPopup != null ? "Found" : "NULL")}");
        
        // Only show popup if slot has an item (not empty)
        if (itemInfoPopup != null && !string.IsNullOrEmpty(itemName) && quantity > 0)
        {
            itemInfoPopup.ShowItemInfo(itemName, itemDescription);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"ItemSlot '{gameObject.name}': Pointer exited.");
        
        if (itemInfoPopup != null)
        {
            itemInfoPopup.HideItemInfo();
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        // Only update popup position if slot has an item
        if (itemInfoPopup != null && !string.IsNullOrEmpty(itemName) && quantity > 0)
        {
            itemInfoPopup.FollowMouse();
        }
    }

    // Helper to activate/deactivate all configured swap panels.
    private void ActivateSwapPanels(bool active)
    {
        if (swapPanels == null) return;
        for (int i = 0; i < swapPanels.Length; i++)
        {
            if (swapPanels[i] != null)
                swapPanels[i].SetActive(active);
        }
    }

    // Public deselect method used by external managers to clear selection visuals.
    public void DeselectVisuals()
    {
        thisItemSelected = false;
        if (selectedShader != null) selectedShader.SetActive(false);
        ActivateSwapPanels(false);
    }

}

public enum SlotType
{
    Regular,
    Artifact,
    Ability
}
