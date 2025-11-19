using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ActiveSlot represents an equipped/active item slot (e.g. currently wielded or quick-access item).
// It is lighter than ItemSlot: no hover, selection shader, or swap panels.
// If you need those features, consider reusing ItemSlot or refactoring shared logic.
public class ActiveSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
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
    private TMP_Text slotNrText; // Cache for SlotNr text component

    private void Start()
    {
        inventoryManager = GameObject.Find("Player").GetComponent<InventoryManager>();
        
        // Auto-find SlotNr text component (recursively search children)
        Transform slotNrTransform = FindChildRecursive(transform, "SlotNr");
        if (slotNrTransform != null)
        {
            slotNrText = slotNrTransform.GetComponent<TMP_Text>();
            if (slotNrText == null)
            {
                Debug.LogWarning($"ActiveSlot '{gameObject.name}': Found SlotNr GameObject but no TMP_Text component.");
            }
            else
            {
                Debug.Log($"ActiveSlot '{gameObject.name}': Found SlotNr text component. Text value: '{slotNrText.text}'");
            }
        }
        else
        {
            // Alternative: Try searching for TMP_Text components by name
            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in allTexts)
            {
                if (text.gameObject.name == "SlotNr")
                {
                    slotNrText = text;
                    Debug.Log($"ActiveSlot '{gameObject.name}': Found SlotNr via GetComponentsInChildren. Text value: '{slotNrText.text}'");
                    break;
                }
            }
            
            if (slotNrText == null)
            {
                Debug.LogWarning($"ActiveSlot '{gameObject.name}': No SlotNr child found. Keybind detection may not work. Searching hierarchy...");
                // Debug: Print the hierarchy
                PrintHierarchy(transform, 0);
            }
        }
        
        // Auto-find itemInfoPopup if not assigned
        if (itemInfoPopup == null)
        {
            itemInfoPopup = FindObjectOfType<ShopInfo>();
            if (itemInfoPopup == null)
            {
                Debug.LogWarning($"ActiveSlot '{gameObject.name}': No ShopInfo component found in scene! Hover functionality will not work.");
            }
            else
            {
                Debug.Log($"ActiveSlot '{gameObject.name}': Auto-assigned ShopInfo component.");
            }
        }
        
        // Validate that we have an Image component for raycasting
        Image img = GetComponent<Image>();
        if (img == null)
        {
            Debug.LogWarning($"ActiveSlot '{gameObject.name}': No Image component found! Add an Image component for hover detection.");
        }
        else if (!img.raycastTarget)
        {
            Debug.LogWarning($"ActiveSlot '{gameObject.name}': Image raycastTarget is disabled! Enable it for hover detection.");
        }
    }

    /// <summary>
    /// Recursively searches for a child Transform by name
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        // Check direct children first
        Transform found = parent.Find(name);
        if (found != null)
        {
            return found;
        }
        
        // Recursively search in all children
        foreach (Transform child in parent)
        {
            found = FindChildRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Debug helper to print the hierarchy
    /// </summary>
    private void PrintHierarchy(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}- {parent.name} (has TMP_Text: {parent.GetComponent<TMP_Text>() != null})");
        
        foreach (Transform child in parent)
        {
            PrintHierarchy(child, depth + 1);
        }
    }
    
    /// <summary>
    /// Gets the keybind text from SlotNr child component
    /// </summary>
    public string GetSlotNrText()
    {
        if (slotNrText != null)
        {
            string text = slotNrText.text;
            Debug.Log($"ActiveSlot '{gameObject.name}': GetSlotNrText() returning '{text}'");
            return text;
        }
        
        // Fallback: try to find it again recursively
        Debug.Log($"ActiveSlot '{gameObject.name}': slotNrText is null, searching recursively...");
        Transform slotNrTransform = FindChildRecursive(transform, "SlotNr");
        if (slotNrTransform != null)
        {
            slotNrText = slotNrTransform.GetComponent<TMP_Text>();
            if (slotNrText != null)
            {
                string text = slotNrText.text;
                Debug.Log($"ActiveSlot '{gameObject.name}': Found SlotNr on retry, returning '{text}'");
                return text;
            }
        }
        
        Debug.LogWarning($"ActiveSlot '{gameObject.name}': GetSlotNrText() returning empty string - SlotNr not found!");
        return string.Empty;
    }

    /// <summary>
    /// Adds an item to this active slot. Returns leftover quantity if over max.
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
        Debug.Log($"ActiveSlot '{gameObject.name}': Pointer entered. Item: '{itemName}', Popup: {(itemInfoPopup != null ? "Found" : "NULL")}");
        
        if (itemInfoPopup != null && !string.IsNullOrEmpty(itemName))
        {
            itemInfoPopup.ShowItemInfo(itemName, itemDescription);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"ActiveSlot '{gameObject.name}': Pointer exited.");
        
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
            // When clicked, try to receive item from selected ability slot
            if (inventoryManager != null)
            {
                inventoryManager.TransferToActiveSlot(this);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right-click: return item to inventory
            if (inventoryManager != null && !string.IsNullOrEmpty(itemName) && quantity > 0)
            {
                inventoryManager.ReturnActiveSlotToInventory(this);
            }
        }
    }

    




}
