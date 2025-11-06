using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private InputActionReference inventoryAction;
    [SerializeField] private GameObject InventoryMenu;
    private ItemSlot[] itemSlot;
    
    [Header("Slots Setup")]
    [Tooltip("Parent transform that contains all ItemSlot components as children.")]
    [SerializeField] private Transform slotsParent;
    
    private bool menuActivated = false;

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
    }

    private void OnInventory(InputAction.CallbackContext context)
    {
        menuActivated = !menuActivated;
        InventoryMenu.SetActive(menuActivated);
        
        // Pause/unpause time
        Time.timeScale = menuActivated ? 0f : 1f;
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

}
