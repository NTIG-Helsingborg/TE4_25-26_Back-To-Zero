using UnityEngine;

public class Item : MonoBehaviour
{   
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int quantity;

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
                inventoryManager.AddItem(itemName, itemIcon, quantity);
                // Destroy the item in the world
                Destroy(gameObject);
            }
            
        }
        


}}
