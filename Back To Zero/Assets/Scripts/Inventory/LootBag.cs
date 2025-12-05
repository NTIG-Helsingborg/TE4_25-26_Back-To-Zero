
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LootBag : MonoBehaviour
{
    public GameObject droppedItemPrefab;
    public List<ItemSO> lootlist = new List<ItemSO>();

    [Tooltip("Number of items to drop when loot bag is opened")]
    public int numberOfItemsToDrop = 3;
    
    [Header("Animator Controllers")]
    [Tooltip("Animator controller for coins")]
    public RuntimeAnimatorController coinAnimatorController;
    
    [Tooltip("Animator controller for health potions")]
    public RuntimeAnimatorController hpPotAnimatorController;
    
    [Tooltip("Animator controller for gems")]
    public RuntimeAnimatorController gemAnimatorController;
    
    [Tooltip("Animator controller for PowerStone")]
    public RuntimeAnimatorController powerStoneAnimatorController;
    
    [Tooltip("Animator controller for Ring of Health")]
    public RuntimeAnimatorController ringOfHealthAnimatorController;
    
    [Tooltip("Animator controller for EmberForgedGauntlet")]
    public RuntimeAnimatorController emberForgedGauntletAnimatorController;
    
    [Tooltip("Animator controller for BloodSkull")]
    public RuntimeAnimatorController bloodSkullAnimatorController;

    List<ItemSO> GetDroppedItems()
    {
        List<ItemSO> droppedItems = new List<ItemSO>();
        
        for (int i = 0; i < numberOfItemsToDrop; i++)
        {
            int randomNumber = Random.Range(1, 101);
            ItemSO selectedItem = null;
            
            // Sort by drop chance ascending so rarest items are checked first
            List<ItemSO> sortedLoot = new List<ItemSO>(lootlist);
            sortedLoot.Sort((a, b) => a.dropChance.CompareTo(b.dropChance));
            
            foreach (ItemSO item in sortedLoot)
            {
                if (randomNumber <= item.dropChance)
                {
                    selectedItem = item;
                    break; // Found an item for this drop
                }
            }
            
            if (selectedItem != null)
            {
                droppedItems.Add(selectedItem);
            }
        }
        
        return droppedItems;
    }

    public void InstantiateLoot(Vector3 spawnPosition)
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogError("LootBag: droppedItemPrefab is not assigned!");
            return;
        }

        if (lootlist == null || lootlist.Count == 0)
        {
            Debug.LogWarning("LootBag: lootlist is empty, no items to drop.");
            return;
        }

        List<ItemSO> droppedItems = GetDroppedItems();
        Debug.Log($"LootBag: Attempting to drop {droppedItems.Count} items from {numberOfItemsToDrop} rolls.");
        
        foreach (ItemSO item in droppedItems)
        {
            if (item != null)
            {
                GameObject droppedItem = Instantiate(droppedItemPrefab, spawnPosition, Quaternion.identity);
                
                // Set the sprite
                SpriteRenderer spriteRenderer = droppedItem.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && item.itemSprite != null)
                {
                    spriteRenderer.sprite = item.itemSprite;
                }

                // Add animator for coins and health potions
                Animator animator = droppedItem.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = droppedItem.AddComponent<Animator>();
                }
                
                // Assign the appropriate animator controller based on item type or name
                if (item.statToChange == ItemSO.StatToChange.Coin && coinAnimatorController != null)
                {
                    animator.runtimeAnimatorController = coinAnimatorController;
                }
                else if (item.statToChange == ItemSO.StatToChange.Health && hpPotAnimatorController != null)
                {
                    animator.runtimeAnimatorController = hpPotAnimatorController;
                }
                else if (item.statToChange == ItemSO.StatToChange.Gem && gemAnimatorController != null)
                {
                    animator.runtimeAnimatorController = gemAnimatorController;
                }
                else if (item.itemName.Contains("PowerStone") && powerStoneAnimatorController != null)
                {
                    animator.runtimeAnimatorController = powerStoneAnimatorController;
                }
                else if (item.itemName.Contains("Ring of Health") && ringOfHealthAnimatorController != null)
                {
                    animator.runtimeAnimatorController = ringOfHealthAnimatorController;
                }
                else if (item.itemName.Contains("EmberForgedGauntlet") && emberForgedGauntletAnimatorController != null)
                {
                    animator.runtimeAnimatorController = emberForgedGauntletAnimatorController;
                }
                else if (item.itemName.Contains("BloodSkull") && bloodSkullAnimatorController != null)
                {
                    animator.runtimeAnimatorController = bloodSkullAnimatorController;
                }

                // Configure the Item component with data from ItemSO
                Item itemComponent = droppedItem.GetComponent<Item>();
                if (itemComponent != null)
                {
                    // Use reflection to set private fields
                    var itemType = typeof(Item);
                    itemType.GetField("itemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(itemComponent, item.itemName);
                    itemType.GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(itemComponent, item.itemSprite);
                    itemType.GetField("quantity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(itemComponent, 1);
                    itemType.GetField("itemDescription", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(itemComponent, item.itemDescription);
                }
                else
                {
                    Debug.LogWarning("LootBag: droppedItemPrefab doesn't have an Item component!");
                }

                // Add physics
                Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float dropForce = 2f; // Reduced from 300
                    Vector2 dropDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                    rb.AddForce(dropDirection * dropForce, ForceMode2D.Impulse);
                    rb.linearDamping = 2f; // Add drag to make items settle quickly
                }
                
                Debug.Log($"Dropped item: {item.itemName}");
            }
        }
    }
}
