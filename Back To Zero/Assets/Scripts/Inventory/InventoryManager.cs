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
    
    [Header("Active Slots")]
    [Tooltip("The active slots where equipped abilities are placed.")]
    [SerializeField] private ActiveSlot[] activeSlots;
    
    [Tooltip("The active equipment slots where equipped artifacts/equipment are placed.")]
    [SerializeField] private ActiveEquipmentSlot[] activeEquipmentSlots;
    
    private ItemSlot selectedAbilitySlot = null; // Track which ability slot is selected
    private ItemSlot selectedArtifactSlot = null; // Track which artifact slot is selected
    
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
                       if (targetSlots[i] != null && !targetSlots[i].isFull && 
               (targetSlots[i].quantity == 0 || string.IsNullOrEmpty(targetSlots[i].itemName) || targetSlots[i].itemName == itemName))
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

    // Set which ability slot is currently selected
    public void SetSelectedAbilitySlot(ItemSlot slot)
    {
        selectedAbilitySlot = slot;
        Debug.Log($"InventoryManager: Selected ability slot with item '{slot?.itemName}'.");
    }

    // Get the currently selected ability slot
    public ItemSlot GetSelectedAbilitySlot()
    {
        return selectedAbilitySlot;
    }

    // Set which artifact slot is currently selected
    public void SetSelectedArtifactSlot(ItemSlot slot)
    {
        selectedArtifactSlot = slot;
        Debug.Log($"InventoryManager: Selected artifact slot with item '{slot?.itemName}'.");
    }

    // Get the currently selected artifact slot
    public ItemSlot GetSelectedArtifactSlot()
    {
        return selectedArtifactSlot;
    }

    // Transfer an item from the selected ability slot to a specific active slot
    public void TransferToActiveSlot(ActiveSlot targetSlot)
    {
        if (targetSlot == null)
        {
            Debug.LogWarning("InventoryManager: Target active slot is null!");
            return;
        }

        if (selectedAbilitySlot == null || string.IsNullOrEmpty(selectedAbilitySlot.itemName))
        {
            Debug.Log("InventoryManager: No ability slot selected or it's empty.");
            return;
        }

        // Return the currently equipped item to inventory first (if any)
        if (!string.IsNullOrEmpty(targetSlot.itemName) && targetSlot.quantity > 0)
        {
            int returnLeftover = AddItem(targetSlot.itemName, targetSlot.itemSprite, targetSlot.quantity, targetSlot.itemDescription, 2);
            if (returnLeftover > 0)
            {
                Debug.LogWarning($"InventoryManager: Could not return all of '{targetSlot.itemName}' to inventory. Cannot equip new item.");
                return; // Don't equip new item if we couldn't return the old one
            }
            Debug.Log($"InventoryManager: Returned '{targetSlot.itemName}' from active slot to inventory.");
        }

        // Clear the target active slot
        targetSlot.EmptySlot();

        // Transfer the item to the active slot (transfer 1 item)
        int leftover = targetSlot.AddItem(selectedAbilitySlot.itemName, selectedAbilitySlot.itemSprite, 1, selectedAbilitySlot.itemDescription);

        if (leftover == 0)
        {
            // Successfully transferred, reduce quantity in source slot
            selectedAbilitySlot.quantity -= 1;
            if (selectedAbilitySlot.quantity <= 0)
            {
                selectedAbilitySlot.EmptySlot();
            }
            else
            {
                selectedAbilitySlot.UpdateQuantityDisplay();
            }
            Debug.Log($"InventoryManager: Transferred '{selectedAbilitySlot.itemName}' to active slot.");
            
            // Deselect the ability slot after successful transfer
            selectedAbilitySlot.DeselectVisuals();
            selectedAbilitySlot = null;
        }
    }

    // Return an item from an active slot back to the ability inventory
    public void ReturnActiveSlotToInventory(ActiveSlot activeSlot)
    {
        if (activeSlot == null || string.IsNullOrEmpty(activeSlot.itemName) || activeSlot.quantity <= 0)
        {
            Debug.Log("InventoryManager: Active slot is empty, nothing to return.");
            return;
        }

        // Add the item back to ability inventory
        int leftover = AddItem(activeSlot.itemName, activeSlot.itemSprite, activeSlot.quantity, activeSlot.itemDescription, 2);
        
        if (leftover == 0)
        {
            // Successfully returned to inventory, empty the active slot
            Debug.Log($"InventoryManager: Returned '{activeSlot.itemName}' from active slot to inventory.");
            activeSlot.EmptySlot();
        }
        else
        {
            Debug.LogWarning($"InventoryManager: Could not return all of '{activeSlot.itemName}' to inventory. {leftover} items remain in active slot.");
        }
    }

    // Transfer an item from the selected artifact slot to a specific active equipment slot
    public void TransferToActiveEquipmentSlot(ActiveEquipmentSlot targetSlot)
    {
        if (targetSlot == null)
        {
            Debug.LogWarning("InventoryManager: Target active equipment slot is null!");
            return;
        }

        if (selectedArtifactSlot == null || string.IsNullOrEmpty(selectedArtifactSlot.itemName))
        {
            Debug.Log("InventoryManager: No artifact slot selected or it's empty.");
            return;
        }

        // Return the currently equipped item to inventory first (if any)
        if (!string.IsNullOrEmpty(targetSlot.itemName) && targetSlot.quantity > 0)
        {
            int returnLeftover = AddItem(targetSlot.itemName, targetSlot.itemSprite, targetSlot.quantity, targetSlot.itemDescription, 1);
            if (returnLeftover > 0)
            {
                Debug.LogWarning($"InventoryManager: Could not return all of '{targetSlot.itemName}' to inventory. Cannot equip new item.");
                return; // Don't equip new item if we couldn't return the old one
            }
            Debug.Log($"InventoryManager: Returned '{targetSlot.itemName}' from active equipment slot to inventory.");
        }

        // Clear the target active equipment slot
        targetSlot.EmptySlot();

        // Transfer the item to the active equipment slot (transfer 1 item)
        int leftover = targetSlot.AddItem(selectedArtifactSlot.itemName, selectedArtifactSlot.itemSprite, 1, selectedArtifactSlot.itemDescription);

        if (leftover == 0)
        {
            // Successfully transferred, reduce quantity in source slot
            selectedArtifactSlot.quantity -= 1;
            if (selectedArtifactSlot.quantity <= 0)
            {
                selectedArtifactSlot.EmptySlot();
            }
            else
            {
                selectedArtifactSlot.UpdateQuantityDisplay();
            }
            Debug.Log($"InventoryManager: Transferred '{selectedArtifactSlot.itemName}' to active equipment slot.");
            
            // Deselect the artifact slot after successful transfer
            selectedArtifactSlot.DeselectVisuals();
            selectedArtifactSlot = null;
        }
    }

    // Return an item from an active equipment slot back to the artifact inventory
    public void ReturnActiveEquipmentSlotToInventory(ActiveEquipmentSlot activeSlot)
    {
        if (activeSlot == null || string.IsNullOrEmpty(activeSlot.itemName) || activeSlot.quantity <= 0)
        {
            Debug.Log("InventoryManager: Active equipment slot is empty, nothing to return.");
            return;
        }

        // Add the item back to artifact inventory
        int leftover = AddItem(activeSlot.itemName, activeSlot.itemSprite, activeSlot.quantity, activeSlot.itemDescription, 1);
        
        if (leftover == 0)
        {
            // Successfully returned to inventory, empty the active equipment slot
            Debug.Log($"InventoryManager: Returned '{activeSlot.itemName}' from active equipment slot to inventory.");
            activeSlot.EmptySlot();
        }
        else
        {
            Debug.LogWarning($"InventoryManager: Could not return all of '{activeSlot.itemName}' to inventory. {leftover} items remain in active equipment slot.");
        }
    }

}
