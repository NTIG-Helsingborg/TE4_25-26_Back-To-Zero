using UnityEngine;

[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite;
    public string itemDescription;
    public StatToChange statToChange = StatToChange.None;
    public int amountToChangeStat;
    public int dropChance;  
    public int isArtifact; // 0 = no, 1 = yes
    public int dropChance;
    
    [Header("Item Type")]
    [Tooltip("Consumable items are used once. Artifacts provide permanent buffs.")]
    public int isArtifact = 0; // 0 = Consumable, 1 = Artifact
    public bool UseItem()
    {
        if (statToChange == StatToChange.Health)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return false;
            }

            Health health = player.GetComponent<Health>();
            if (health == null || health.IsFullHealth())
            {
                return false;
            }

            Healing healing = player.GetComponent<Healing>();
            if (healing != null)
            {
                return healing.TryStartHeal(this);
            }

            // Fallback if no Healing component is available.
            health.Heal(amountToChangeStat);
            return true;
        }

        Debug.Log($"ItemSO.UseItem called: {itemName}, isArtifact={isArtifact}, stat={statToChange}, amount={amountToChangeStat}");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Fallback to name lookup if tag isn’t set
            player = GameObject.Find("Player");
        }

        if (player == null)
        {
            Debug.LogError("ItemSO.UseItem: Could not find Player object!");
            return false;
        }

        // Handle consumable items
        if (isArtifact == 0)
        {
            if(statToChange == StatToChange.Health)
            {
                Health health = player.GetComponent<Health>();
                if (health == null)
                {
                    Debug.LogError("ItemSO.UseItem: Player has no Health component!");
                    return false;
                }
                
                if (health.IsFullHealth())
                {
                    return false;
                }
                
                health.Heal(amountToChangeStat);
                return true;
            }
            return false;
        }
        
        // Handle artifacts (permanent stat buffs if 'used' explicitly)
        if (isArtifact == 1)
        {
            // Artifacts are passive. Stats are applied by InventoryManager scanning slots
            Debug.Log($"ItemSO.UseItem: '{itemName}' is an artifact and applies passively while in inventory. No direct use.");
            return false; // Return false so UI doesn’t consume/remove it
        }
        
        return false;
    }

    public enum StatToChange
    {
        None,
        Health,
        Power,
        Agility
    }
    
    public enum ItemType
    {
        Consumable,
        Artifact
    }
}
