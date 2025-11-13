using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private InputActionReference inventoryAction;
    [SerializeField] private GameObject InventoryMenu;
    [SerializeField] private GameObject InventoryMenuEquipment;
    [SerializeField] private GameObject InventoryMenuAbility;
    [SerializeField] private GameObject TopPanel;
    private ItemSlot[] itemSlot;
    private ItemSlot[] artifactSlot;
    private ItemSlot[] abilitySlot;

    public ItemSO[] itemSOs;
    
    [Header("Slots Setup")]
    [Tooltip("Parent transform that contains all ItemSlot components as children.")]
    [SerializeField] private Transform slotsParent;
    
    [Tooltip("Parent transform for artifact inventory slots.")]
    [SerializeField] private Transform artifactSlotsParent;
    
    [Tooltip("Parent transform for ability inventory slots.")]
    [SerializeField] private Transform abilitySlotsParent;
    
    private bool menuActivated = false;

    void OnEnable()
    {
        if (inventoryAction != null)
        {
            
            inventoryAction.action.Enable();
            inventoryAction.action.performed += OnInventory;
        }
        
        // Subscribe to shop events to close inventory when shop opens
        Merchant.OnShopStateChanged += OnShopStateChanged;
    }

    void OnDisable()
    {
        if (inventoryAction != null)
        {
            
            inventoryAction.action.performed -= OnInventory;
            inventoryAction.action.Disable();
        }
        
        // Unsubscribe from shop events
        Merchant.OnShopStateChanged -= OnShopStateChanged;
    }

    private void OnShopStateChanged(ShopManager shop, bool isOpen)
    {
        // Close inventory when shop opens
        if (isOpen && menuActivated)
        {
            CloseInventory();
        }
    }

    private void Awake()
    {
        // Auto-populate slots from parent container
        if (slotsParent != null)
        {
            itemSlot = slotsParent.GetComponentsInChildren<ItemSlot>(true);
            Debug.Log($"InventoryManager: Found {itemSlot.Length} slots in '{slotsParent.name}'.");
        }
        else if (InventoryMenu != null)
        {
            // Fallback: try to find slots in InventoryMenu
            itemSlot = InventoryMenu.GetComponentsInChildren<ItemSlot>(true);
            if (itemSlot.Length > 0)
            {
                Debug.Log($"InventoryManager: Found {itemSlot.Length} slots in InventoryMenu.");
            }
        }
        
        // Auto-populate artifact slots
        if (artifactSlotsParent != null)
        {
            artifactSlot = artifactSlotsParent.GetComponentsInChildren<ItemSlot>(true);
            Debug.Log($"InventoryManager: Found {artifactSlot.Length} artifact slots in '{artifactSlotsParent.name}'.");
        }
        
        // Auto-populate ability slots
        if (abilitySlotsParent != null)
        {
            abilitySlot = abilitySlotsParent.GetComponentsInChildren<ItemSlot>(true);
            Debug.Log($"InventoryManager: Found {abilitySlot.Length} ability slots in '{abilitySlotsParent.name}'.");
        }
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        // Don't allow opening inventory if shop is open
        if (Merchant.currentShopKeeper != null)
        {
            return;
        }
        
        menuActivated = !menuActivated;
        
        // When closing, close ALL panels
        if (!menuActivated)
        {
            InventoryMenu.SetActive(false);
            if (InventoryMenuEquipment != null) InventoryMenuEquipment.SetActive(false);
            if (InventoryMenuAbility != null) InventoryMenuAbility.SetActive(false);
        }
        else
        {
            // When opening, only open main menu
            InventoryMenu.SetActive(true);
        }
        
        if (TopPanel != null)
        {
            TopPanel.SetActive(menuActivated);
        }
        
        // Pause/unpause time
        Time.timeScale = menuActivated ? 0f : 1f;
    }

    private void CloseInventory()
    {
        menuActivated = false;
        InventoryMenu.SetActive(false);
        InventoryMenuEquipment.SetActive(false);
        InventoryMenuAbility.SetActive(false);
        
        if (TopPanel != null)
        {
            TopPanel.SetActive(false);
        }
        // Don't change timeScale here - let the shop manage it
    }

    public bool UseItem(string itemName)
    {
        // Before using an item, ensure the player actually has at least one in their inventory slots.
        int totalCount = 0;
        if (itemSlot != null)
        {
            for (int s = 0; s < itemSlot.Length; s++)
            {
                if (itemSlot[s] != null && itemSlot[s].itemName == itemName)
                    totalCount += itemSlot[s].quantity;
            }
        }

        if (totalCount <= 0)
        {
            Debug.Log($"InventoryManager: Tried to use '{itemName}' but none are present (totalCount={totalCount}).");
            return false;
        }

        for (int i = 0; i < itemSOs.Length; i++)
        {
            if (itemSOs[i].itemName == itemName)
            {
                bool usable = itemSOs[i].UseItem();
                return usable;
            }
        }
        return false;
    }

    public int AddItem(string itemName, Sprite itemIcon, int quantity, string itemDescription)
    {
        return AddItem(itemName, itemIcon, quantity, itemDescription, 0);
    }
    
    public int AddItem(string itemName, Sprite itemIcon, int quantity, string itemDescription, int isArtifactOverride)
    {
        Debug.Log("item name: " + itemName + " quantity: " + quantity);
        
        // Check if this item is an artifact or ability - use override if provided, otherwise check ItemSO
        bool isArtifact = isArtifactOverride == 1;
        bool isAbility = isArtifactOverride == 2; // 2 = ability override
        
        if (!isArtifact && !isAbility)
        {
            for (int j = 0; j < itemSOs.Length; j++)
            {
                if (itemSOs[j].itemName == itemName)
                {
                    if (itemSOs[j].isArtifact == 1)
                    {
                        isArtifact = true;
                        break;
                    }
                    if (itemSOs[j].isAbility == 1)
                    {
                        isAbility = true;
                        break;
                    }
                }
            }
        }
        
        // Choose the appropriate slot array - abilities have priority over artifacts
        ItemSlot[] targetSlots;
        string slotType;
        int typeOverride; // For recursive calls
        
        if (isAbility)
        {
            targetSlots = abilitySlot;
            slotType = "ability";
            typeOverride = 2;
        }
        else if (isArtifact)
        {
            targetSlots = artifactSlot;
            slotType = "artifact";
            typeOverride = 1;
        }
        else
        {
            targetSlots = itemSlot;
            slotType = "item";
            typeOverride = 0;
        }
        
        if (targetSlots == null || targetSlots.Length == 0)
        {
            Debug.LogError($"InventoryManager: No {slotType} slots found! Make sure slotsParent, artifactSlotsParent, or abilitySlotsParent is assigned.");
            return quantity;
        }
        
        for (int i = 0; i < targetSlots.Length; i++)
        {
           if (targetSlots[i] != null && (targetSlots[i].isFull == false && targetSlots[i].itemName == itemName || targetSlots[i].quantity == 0))
           {
               int leftOverItems = targetSlots[i].AddItem(itemName, itemIcon, quantity, itemDescription);
               Debug.Log($"Added {itemName} to {slotType} slot {i}");
               if (leftOverItems > 0)
                   leftOverItems = AddItem(itemName, itemIcon, leftOverItems, itemDescription, typeOverride);

                return leftOverItems;
           }
        }
        
        Debug.Log($"All {slotType} inventory slots are full!");
        return quantity; // Return remaining items if inventory is full
    }

    public void DeselectAllSlots(){
        if (itemSlot == null) return;
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i] != null)
                itemSlot[i].DeselectVisuals();
        }
    }

    public void DeselectAllArtifactSlots(){
        if (artifactSlot == null) return;
        
        for (int i = 0; i < artifactSlot.Length; i++)
        {
            if (artifactSlot[i] != null)
                artifactSlot[i].DeselectVisuals();
        }
    }

    public void DeselectAllAbilitySlots(){
        if (abilitySlot == null) return;
        
        for (int i = 0; i < abilitySlot.Length; i++)
        {
            if (abilitySlot[i] != null)
                abilitySlot[i].DeselectVisuals();
        }
    }

    // Get total count of a specific item by name across all inventory slots
    public int GetItemCount(string itemName)
    {
        int totalCount = 0;
        if (itemSlot != null)
        {
            for (int i = 0; i < itemSlot.Length; i++)
            {
                if (itemSlot[i] != null && itemSlot[i].itemName == itemName)
                {
                    totalCount += itemSlot[i].quantity;
                }
            }
        }
        return totalCount;
    }

    // Check if there's space available for a specific item (either empty slot or existing stack with room)
    public bool HasInventorySpace(string itemName)
    {
        if (itemSlot == null || itemSlot.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i] != null)
            {
                // Check for empty slot
                if (itemSlot[i].quantity == 0)
                {
                    return true;
                }
                
                // Check for existing stack that's not full
                if (itemSlot[i].itemName == itemName && !itemSlot[i].isFull)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Remove a specific quantity of an item from inventory (returns true if successful)
    public bool RemoveItem(string itemName, int quantityToRemove)
    {
        if (itemSlot == null || quantityToRemove <= 0) return false;

        // First check if we have enough
        int totalAvailable = GetItemCount(itemName);
        if (totalAvailable < quantityToRemove)
        {
            Debug.Log($"InventoryManager: Cannot remove {quantityToRemove} of '{itemName}', only have {totalAvailable}.");
            return false;
        }

        // Remove items from slots
        int remainingToRemove = quantityToRemove;
        for (int i = 0; i < itemSlot.Length && remainingToRemove > 0; i++)
        {
            if (itemSlot[i] != null && itemSlot[i].itemName == itemName)
            {
                int amountInSlot = itemSlot[i].quantity;
                if (amountInSlot <= remainingToRemove)
                {
                    // Empty this slot completely
                    remainingToRemove -= amountInSlot;
                    itemSlot[i].EmptySlot();
                }
                else
                {
                    // Partially reduce this slot
                    itemSlot[i].quantity -= remainingToRemove;
                    itemSlot[i].UpdateQuantityDisplay();
                    remainingToRemove = 0;
                }
            }
        }

        Debug.Log($"InventoryManager: Removed {quantityToRemove} of '{itemName}'.");
        return true;
    }

}
