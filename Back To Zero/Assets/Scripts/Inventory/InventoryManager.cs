using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private InputActionReference inventoryAction;
    [SerializeField] private GameObject InventoryMenu;
    
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

    private void OnInventory(InputAction.CallbackContext context)
    {
        menuActivated = !menuActivated;
        InventoryMenu.SetActive(menuActivated);
        
        // Pause/unpause time
        Time.timeScale = menuActivated ? 0f : 1f;
    }

    public void AddItem(string itemName, Sprite icon, int quantity)
    {
        // Logic to add item to inventory UI
        Debug.Log("itemName =" + itemName + " quantity =" + quantity);
    }

}
