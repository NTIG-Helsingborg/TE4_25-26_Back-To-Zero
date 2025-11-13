using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private InputActionReference inventoryAction;
    [SerializeField] private GameObject InventoryMenu;
    private ItemSlot[] itemSlot;

    public ItemSO[] itemSOs;
    
    [Header("Slots Setup")]
    [Tooltip("Parent transform that contains all ItemSlot components as children.")]
    [SerializeField] private Transform slotsParent;
    
    private bool menuActivated = false;

    private PlayerStats playerStats;

    void OnEnable()
    {
        if (inventoryAction != null)
        {
            
            inventoryAction.action.Enable();
            inventoryAction.action.performed += OnInventory;
        }
    }

    void OnDisable()
    {
        if (inventoryAction != null)
        {
            
            inventoryAction.action.performed -= OnInventory;
            inventoryAction.action.Disable();
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

        playerStats = GetComponent<PlayerStats>();
        Debug.Log("InventoryManager: PlayerStats found: " + (playerStats != null));
    }

    private void Start()
    {
        // Apply passives if inventory pre-populated
        RecalculatePlayerStatsFromInventory();
    }

    private void OnInventory(InputAction.CallbackContext context)
    {
        menuActivated = !menuActivated;
        InventoryMenu.SetActive(menuActivated);
        
        // Pause/unpause time
        Time.timeScale = menuActivated ? 0f : 1f;
    }

    public bool UseItem(string itemName)
    {
        for(int i = 0; i < itemSOs.Length; i++)
        {
            if(itemSOs[i].itemName == itemName)
            {
                bool usable = itemSOs[i].UseItem();
                
                // Inventory may have changed (consumables). Recalc passives anyway.
                RecalculatePlayerStatsFromInventory();

                return usable;
            }
        }
        return false;
    }

    public int AddItem(string itemName, Sprite itemIcon, int quantity, string itemDescription)
    {
        Debug.Log("item name: " + itemName + " quantity: " + quantity);
        
        if (itemSlot == null || itemSlot.Length == 0)
        {
            Debug.LogError("InventoryManager: No item slots found! Make sure slotsParent or InventoryMenu has ItemSlot components.");
            return quantity;
        }
        
        for (int i = 0; i < itemSlot.Length; i++)
        {
           if (itemSlot[i] != null && (itemSlot[i].isFull == false && itemSlot[i].itemName == itemName || itemSlot[i].quantity == 0))
           {
               int leftOverItems = itemSlot[i].AddItem(itemName, itemIcon, quantity, itemDescription);
               Debug.Log($"Added {itemName} to slot {i}");
               if (leftOverItems > 0)
                   leftOverItems = AddItem(itemName, itemIcon, leftOverItems, itemDescription);

               // Recalc passives after inventory actually changed
               RecalculatePlayerStatsFromInventory();

               return leftOverItems;
           }
        }
        
        Debug.Log("All inventory slots are full!");
        return quantity; // Return remaining items if inventory is full
    }

    public void DeselectAllSlots(){

        for (int i = 0; i < itemSlot.Length; i++)
        {
            itemSlot[i].selectedShader.SetActive(false);
            itemSlot[i].thisItemSelected = false;
        }
    }

    // Build a list of artifacts currently in slots and push to PlayerStats
    private void RecalculatePlayerStatsFromInventory()
    {
        if (playerStats == null)
            return;

        var artifacts = new List<ItemSO>();
        if (itemSlot != null && itemSOs != null)
        {
            for (int i = 0; i < itemSlot.Length; i++)
            {
                var slot = itemSlot[i];
                if (slot == null || slot.quantity <= 0 || string.IsNullOrEmpty(slot.itemName))
                    continue;

                // Find matching ItemSO by name
                ItemSO def = null;
                for (int j = 0; j < itemSOs.Length; j++)
                {
                    if (itemSOs[j] != null && itemSOs[j].itemName == slot.itemName)
                    {
                        def = itemSOs[j];
                        break;
                    }
                }

                if (def == null)
                    continue;

                if (def.isArtifact == 1)
                {
                    // If artifacts can stack, count quantity; otherwise, one is enough
                    int count = Mathf.Max(1, slot.quantity);
                    for (int c = 0; c < count; c++)
                        artifacts.Add(def);
                }
            }
        }

        playerStats.RecalculateFromArtifacts(artifacts);
    }
}
