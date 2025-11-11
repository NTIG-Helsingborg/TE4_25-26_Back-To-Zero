using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    

    [SerializeField] private Shop[] shopSlots;

    [SerializeField] private InventoryManager inventoryManager;


    public void PopulateShopItems(List<ShopItems> shopItems)
    {
        for (int i = 0; i < shopItems.Count && i < shopSlots.Length; i++)
        {
            ShopItems shopitem = shopItems[i];
            shopSlots[i].Initialize(shopitem.itemSO, shopitem.price);
            shopSlots[i].gameObject.SetActive(true);
        }

        for (int j = shopItems.Count; j < shopSlots.Length; j++)
        {
            shopSlots[j].gameObject.SetActive(false);
        }
    }


    public void TryBuyItem(ItemSO itemSO, int price)
    {
        if (inventoryManager == null)
        {
            Debug.LogError("ShopManager: InventoryManager reference is not set!");
            return;
        }

        // Check if there's space in the inventory for the item
        if (!inventoryManager.HasInventorySpace(itemSO.itemName))
        {
            Debug.Log($"ShopManager: Inventory is full! No space for {itemSO.itemName}.");
            return;
        }

        // Check how many coins the player has
        int coinCount = inventoryManager.GetItemCount("Coin");
        
        if (coinCount >= price)
        {
            // Player has enough coins - proceed with purchase
            bool removed = inventoryManager.RemoveItem("Coin", price);
            
            if (removed)
            {
                // Add the purchased item to inventory
                inventoryManager.AddItem(itemSO.itemName, itemSO.itemSprite, 1, itemSO.itemDescription);
                Debug.Log($"ShopManager: Purchased {itemSO.itemName} for {price} coins. Remaining coins: {inventoryManager.GetItemCount("Coin")}");
            }
            else
            {
                Debug.LogError("ShopManager: Failed to remove coins from inventory.");
            }
        }
        else
        {
            // Not enough coins
            Debug.Log($"ShopManager: Not enough coins! Need {price}, but only have {coinCount}.");
        }
    }

}

[System.Serializable]
public class ShopItems{
    public ItemSO itemSO;
    public int price;
}
