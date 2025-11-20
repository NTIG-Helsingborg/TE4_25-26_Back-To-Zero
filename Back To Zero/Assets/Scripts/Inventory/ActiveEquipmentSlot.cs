using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ActiveEquipmentSlot represents an equipped artifact/equipment slot.
public class ActiveEquipmentSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    // Data
    public string itemName;
    public string itemDescription;
    public Sprite itemSprite;
    public bool isFull;
    public int quantity; // current stack count

    [SerializeField] private int maxNumberOfItems = 1; // Active slot typically single item

    // UI
    [Header("UI References")]
    [SerializeField] private TMP_Text quantityText; // optional
    [SerializeField] private Image itemImage;       // icon
    [SerializeField] private ShopInfo itemInfoPopup; // Reference to the hover popup

    private InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = GameObject.Find("Player").GetComponent<InventoryManager>();
        
        // Auto-find itemInfoPopup if not assigned
        if (itemInfoPopup == null)
        {
            itemInfoPopup = FindFirstObjectByType<ShopInfo>();
            if (itemInfoPopup == null)
            {
                Debug.LogWarning($"ActiveEquipmentSlot '{gameObject.name}': No ShopInfo component found in scene! Hover functionality will not work.");
            }
            else
            {
                Debug.Log($"ActiveEquipmentSlot '{gameObject.name}': Auto-assigned ShopInfo component.");
            }
        }
        
        // Validate that we have an Image component for raycasting
        Image img = GetComponent<Image>();
        if (img == null)
        {
            Debug.LogWarning($"ActiveEquipmentSlot '{gameObject.name}': No Image component found! Add an Image component for hover detection.");
        }
        else if (!img.raycastTarget)
        {
            Debug.LogWarning($"ActiveEquipmentSlot '{gameObject.name}': Image raycastTarget is disabled! Enable it for hover detection.");
        }
    }

    /// <summary>
    /// Adds an item to this active equipment slot. Returns leftover quantity if over max.
    /// </summary>
    public int AddItem(string itemName, Sprite itemIcon, int quantity, string itemDescription)
    {
        if (isFull)
            return quantity; // nothing added

        this.itemName = itemName;
        this.itemSprite = itemIcon;
        this.itemDescription = itemDescription;

        this.quantity += quantity;

        if (this.quantity >= maxNumberOfItems)
        {
            int extraItems = this.quantity - maxNumberOfItems;
            this.quantity = maxNumberOfItems;
            isFull = true;
            UpdateVisuals(itemIcon);
            return extraItems;
        }

        UpdateVisuals(itemIcon);
        return 0;
    }

    private void UpdateVisuals(Sprite icon)
    {
        if (itemImage != null)
        {
            itemImage.sprite = icon;
            itemImage.enabled = true;
        }
        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
            // Optional: show only if >1
            quantityText.enabled = quantity > 1;
        }
    }

    /// <summary>
    /// Clears all data & visuals.
    /// </summary>
    public void EmptySlot()
    {
        itemName = string.Empty;
        itemDescription = string.Empty;
        itemSprite = null;
        quantity = 0;
        isFull = false;

        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.enabled = false;
        }
        if (quantityText != null)
        {
            quantityText.text = string.Empty;
            quantityText.enabled = false;
        }
        
        // Hide the popup if it's currently showing
        if (itemInfoPopup != null)
        {
            itemInfoPopup.HideItemInfo();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"ActiveEquipmentSlot '{gameObject.name}': Pointer entered. Item: '{itemName}', Popup: {(itemInfoPopup != null ? "Found" : "NULL")}");
        
        if (itemInfoPopup != null && !string.IsNullOrEmpty(itemName))
        {
            itemInfoPopup.ShowItemInfo(itemName, itemDescription);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"ActiveEquipmentSlot '{gameObject.name}': Pointer exited.");
        
        if (itemInfoPopup != null)
        {
            itemInfoPopup.HideItemInfo();
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (itemInfoPopup != null && !string.IsNullOrEmpty(itemName))
        {
            itemInfoPopup.FollowMouse();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // When clicked, try to receive item from selected artifact slot
            if (inventoryManager != null)
            {
                inventoryManager.TransferToActiveEquipmentSlot(this);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right-click: return item to artifact inventory
            if (inventoryManager != null && !string.IsNullOrEmpty(itemName) && quantity > 0)
            {
                inventoryManager.ReturnActiveEquipmentSlotToInventory(this);
            }
        }
    }

    
}
