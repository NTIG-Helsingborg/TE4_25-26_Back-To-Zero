using UnityEngine;

/// <summary>
/// ScriptableObject representing a level-up reward/boon (like Hades)
/// Can modify player stats with buffs and/or debuffs
/// </summary>
[CreateAssetMenu(fileName = "New Reward", menuName = "Rewards/Reward Boon")]
public class RewardSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Name of the boon/reward")]
    public string rewardName = "New Reward";
    
    [TextArea(3, 5)]
    [Tooltip("Description of what this reward does")]
    public string description = "A powerful boon that grants...";

    [Header("Rarity")]
    [Tooltip("Rarity level determines power and visual styling")]
    public RewardRarity rarity = RewardRarity.Common;

    [Header("Stat Modifications")]
    [Tooltip("Positive values increase, negative values decrease")]
    
    [Header("Damage & Combat")]
    public float damageMultiplierChange = 0f;        // e.g., 0.15 = +15% damage
    public float attackSpeedChange = 0f;             // e.g., 0.1 = +10% attack speed
    public float critChanceChange = 0f;              // e.g., 0.1 = +10% crit chance
    
    [Header("Survivability")]
    public int maxHealthChange = 0;                  // e.g., 25 = +25 max health
    public float defenseMultiplierChange = 0f;        // e.g., 0.1 = +10% defense
    public float lifestealChange = 0f;               // e.g., 0.05 = 5% lifesteal
    
    [Header("Mobility")]
    public float moveSpeedChange = 0f;               // e.g., 0.15 = +15% move speed
    
    [Header("Ability Modifications")]
    public float abilityCooldownReduction = 0f;      // e.g., 0.2 = 20% cooldown reduction
    
    [Header("Special Effects")]
    [Tooltip("Chance to apply status effects (0-1)")]
    public float statusEffectChanceChange = 0f;
    
    /// <summary>
    /// Get a formatted description of all stat changes
    /// </summary>
    public string GetStatChangesDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        if (damageMultiplierChange != 0)
        {
            string sign = damageMultiplierChange > 0 ? "+" : "";
            sb.AppendLine($"{sign}{damageMultiplierChange * 100:F0}% Damage");
        }
        
        if (attackSpeedChange != 0)
        {
            string sign = attackSpeedChange > 0 ? "+" : "";
            sb.AppendLine($"{sign}{attackSpeedChange * 100:F0}% Attack Speed");
        }
        
        if (critChanceChange != 0)
        {
            string sign = critChanceChange > 0 ? "+" : "";
            sb.AppendLine($"{sign}{critChanceChange * 100:F0}% Crit Chance");
        }
        
        if (maxHealthChange != 0)
        {
            string sign = maxHealthChange > 0 ? "+" : "";
            sb.AppendLine($"{sign}{maxHealthChange} Max Health");
        }
        
        if (defenseMultiplierChange != 0)
        {
            string sign = defenseMultiplierChange > 0 ? "+" : "";
            sb.AppendLine($"{sign}{defenseMultiplierChange * 100:F0}% Defense");
        }
        
        if (lifestealChange != 0)
        {
            string sign = lifestealChange > 0 ? "+" : "";
            sb.AppendLine($"{sign}{lifestealChange * 100:F0}% Lifesteal");
        }
        
        if (moveSpeedChange != 0)
        {
            string sign = moveSpeedChange > 0 ? "+" : "";
            sb.AppendLine($"{sign}{moveSpeedChange * 100:F0}% Move Speed");
        }
        
        if (abilityCooldownReduction != 0)
        {
            string sign = abilityCooldownReduction > 0 ? "+" : "";
            sb.AppendLine($"{sign}{abilityCooldownReduction * 100:F0}% Cooldown Reduction");
        }
        
        if (statusEffectChanceChange != 0)
        {
            string sign = statusEffectChanceChange > 0 ? "+" : "";
            sb.AppendLine($"{sign}{statusEffectChanceChange * 100:F0}% Status Effect Chance");
        }
        
        return sb.ToString().TrimEnd();
    }
    
    /// <summary>
    /// Check if this reward has any debuffs (negative values)
    /// </summary>
    public bool HasDebuffs()
    {
        return damageMultiplierChange < 0 ||
               attackSpeedChange < 0 ||
               critChanceChange < 0 ||
               maxHealthChange < 0 ||
               defenseMultiplierChange < 0 ||
               lifestealChange < 0 ||
               moveSpeedChange < 0 ||
               abilityCooldownReduction < 0 ||
               statusEffectChanceChange < 0;
    }
}

