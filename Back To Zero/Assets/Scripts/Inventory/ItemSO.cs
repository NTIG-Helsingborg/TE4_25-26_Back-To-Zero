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
    
    [Header("Item Type")]
    [Tooltip("Consumable items are used once. Artifacts provide permanent buffs.")]
    public ItemType itemType = ItemType.Consumable;

    public bool UseItem()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("ItemSO.UseItem: Could not find Player object!");
            return false;
        }

        // Handle consumable items
        if (itemType == ItemType.Consumable)
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
        
        // Handle artifacts (permanent stat buffs)
        if (itemType == ItemType.Artifact)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("ItemSO.UseItem: Player needs PlayerStats component for artifacts!");
                return false;
            }

            try
            {
                switch (statToChange)
                {
                    case StatToChange.Power:
                        playerStats.AddStatBonus(StatType.DamageMultiplier, amountToChangeStat * 0.01f);
                        Debug.Log($"Power increased! +{amountToChangeStat}% damage");
                        return true;
                        
                    case StatToChange.Agility:
                        playerStats.AddStatBonus(StatType.MoveSpeed, amountToChangeStat * 0.1f);
                        Debug.Log($"Agility increased! +{amountToChangeStat * 0.1f} move speed");
                        return true;
                        
                    case StatToChange.Intelligence:
                        playerStats.AddStatBonus(StatType.AttackSpeed, amountToChangeStat * 0.01f);
                        Debug.Log($"Intelligence increased! +{amountToChangeStat}% attack speed");
                        return true;
                        
                    case StatToChange.Health:
                        playerStats.AddStatBonus(StatType.MaxHealth, amountToChangeStat);
                        Debug.Log($"Max health increased! +{amountToChangeStat} HP");
                        return true;
                        
                    default:
                        Debug.LogWarning($"Artifact stat type {statToChange} not implemented");
                        return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error applying artifact: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        
        return false;
    }

    public enum StatToChange
    {
        None,
        Health,
        Power,
        Agility,
        Intelligence,
        Coin
    }
    
    public enum ItemType
    {
        Consumable,
        Artifact
    }
}
