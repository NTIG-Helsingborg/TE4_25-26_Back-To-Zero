using UnityEngine;

public class Item : MonoBehaviour
{   
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int quantity;
    [TextArea][SerializeField] private string itemDescription;

    private InventoryManager inventoryManager;
    
    void Start()
    {
        inventoryManager = GameObject.Find("Player")?.GetComponent<InventoryManager>();
        Debug.Log("Item Start - InventoryManager found: " + (inventoryManager != null));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("TRIGGER ENTERED! Collided with: " + collision.gameObject.name + " | Tag: " + collision.gameObject.tag);
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player tag matched! Attempting to add item...");
            
            // Try to get InventoryManager if not cached
            if (inventoryManager == null)
            {
                inventoryManager = collision.gameObject.GetComponent<InventoryManager>();
                Debug.Log("Dynamically fetched InventoryManager: " + (inventoryManager != null));
            }
            
            if (inventoryManager != null)
            {
                // Add item to inventory
                int leftOverItems = inventoryManager.AddItem(itemName, itemIcon, quantity, itemDescription);
                if(leftOverItems <= 0)
                    Destroy(gameObject);
                else
                    quantity = leftOverItems;
            }
            else
            {
                Debug.LogError("InventoryManager not found on Player!");
            }
        }
    }
}
