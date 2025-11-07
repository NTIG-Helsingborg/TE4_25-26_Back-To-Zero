using UnityEngine;

public class Item : MonoBehaviour
{   
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int quantity;
    [TextArea][SerializeField] private string itemDescription;

    private InventoryManager inventoryManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventoryManager = GameObject.Find("Player").GetComponent<InventoryManager>();
        Debug.Log("Item Start - InventoryManager found: " + (inventoryManager != null));
    }

   private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("TRIGGER ENTERED! Collided with: " + collision.gameObject.name + " | Tag: " + collision.gameObject.tag);
        
        if (collision.gameObject.tag=="Player")
        {
            Debug.Log("Player tag matched! Attempting to add item...");
            
            if (inventoryManager != null)
            {
                // Add item to inventory
                int leftOverItems = inventoryManager.AddItem(itemName, itemIcon, quantity, itemDescription);
                if(leftOverItems <= 0)
                    Destroy(gameObject);
                else
                    quantity = leftOverItems;
            }
            
        }
        


}}
