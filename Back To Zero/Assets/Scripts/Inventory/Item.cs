using UnityEngine;

public class Item : MonoBehaviour
{   
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int quantity;
    [TextArea][SerializeField] private string itemDescription;
    [SerializeField] private int isArtifact; // 0 = no, 1 = yes
    [SerializeField] private int isAbility; // 0 = no, 1 = yes

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
                // Determine the override value: 0 = regular item, 1 = artifact, 2 = ability
                // Abilities have priority over artifacts (though they shouldn't both be set)
                int overrideValue = 0;
                if (isAbility == 1)
                {
                    overrideValue = 2; // 2 = ability override
                }
                else if (isArtifact == 1)
                {
                    overrideValue = 1; // 1 = artifact override
                }
                
                // Add item to inventory with appropriate flag
                int leftOverItems = inventoryManager.AddItem(itemName, itemIcon, quantity, itemDescription, overrideValue);
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
