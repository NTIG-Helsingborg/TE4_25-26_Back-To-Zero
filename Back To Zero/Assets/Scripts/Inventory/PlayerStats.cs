using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField] private float baseDamageMultiplier = 1f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseDefenseMultiplier = 1f;

    // Current stat modifiers
    private float moveSpeedBonus = 0f;
    private int maxHealthBonus = 0;
    private float damageMultiplierBonus = 0f;
    private float attackSpeedBonus = 0f;
    private float defenseMultiplierBonus = 0f;

    // Events to notify other systems when stats change
    public event Action OnStatsChanged;

    private void Start()
    {
        ApplyStatsToPlayer();
    }

    // Getters for calculated stats
    public float GetMoveSpeed() => baseMoveSpeed + moveSpeedBonus;
    public int GetMaxHealth() => baseMaxHealth + maxHealthBonus;
    public float GetDamageMultiplier() => baseDamageMultiplier + damageMultiplierBonus;
    public float GetAttackSpeed() => baseAttackSpeed + attackSpeedBonus;
    public float GetDefenseMultiplier() => baseDefenseMultiplier + defenseMultiplierBonus;

    /// <summary>
    /// Add a stat bonus from an artifact
    /// </summary>
    public void AddStatBonus(StatType statType, float amount)
    {
        switch (statType)
        {
            case StatType.MoveSpeed:
                moveSpeedBonus += amount;
                break;
            case StatType.MaxHealth:
                maxHealthBonus += Mathf.RoundToInt(amount);
                break;
            case StatType.DamageMultiplier:
                damageMultiplierBonus += amount;
                break;
            case StatType.AttackSpeed:
                attackSpeedBonus += amount;
                break;
            case StatType.DefenseMultiplier:
                defenseMultiplierBonus += amount;
                break;
        }

        ApplyStatsToPlayer();
        OnStatsChanged?.Invoke();
        Debug.Log($"Added {amount} to {statType}. Current value: {GetStatValue(statType)}");
    }

    /// <summary>
    /// Remove a stat bonus (useful if artifacts can be removed)
    /// </summary>
    public void RemoveStatBonus(StatType statType, float amount)
    {
        switch (statType)
        {
            case StatType.MoveSpeed:
                moveSpeedBonus -= amount;
                break;
            case StatType.MaxHealth:
                maxHealthBonus -= Mathf.RoundToInt(amount);
                break;
            case StatType.DamageMultiplier:
                damageMultiplierBonus -= amount;
                break;
            case StatType.AttackSpeed:
                attackSpeedBonus -= amount;
                break;
            case StatType.DefenseMultiplier:
                defenseMultiplierBonus -= amount;
                break;
        }

        ApplyStatsToPlayer();
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Apply current stats to player components
    /// </summary>
    private void ApplyStatsToPlayer()
    {
        try
        {
            // Apply move speed to PlayerMove component
            PlayerMove playerMove = GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerMove.MoveSpeed = GetMoveSpeed();
            }

            // Apply max health to Health component
            Health health = GetComponent<Health>();
            if (health != null)
            {
                int oldMaxHealth = health.GetMaxHealth();
                int newMaxHealth = GetMaxHealth();
                
                // Only update if max health changed
                if (newMaxHealth != oldMaxHealth)
                {
                    health.SetMaxHealth(newMaxHealth);
                    
                    // If max health increased, heal the difference
                    if (newMaxHealth > oldMaxHealth)
                    {
                        int healthDifference = newMaxHealth - oldMaxHealth;
                        health.Heal(healthDifference);
                    }
                }
            }

            // Damage multiplier will be applied when dealing damage (see EntityDamage)
            // Attack speed will be applied to attack abilities
            // Defense multiplier will be applied when taking damage
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PlayerStats.ApplyStatsToPlayer error: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Get current value of a specific stat
    /// </summary>
    private float GetStatValue(StatType statType)
    {
        switch (statType)
        {
            case StatType.MoveSpeed: return GetMoveSpeed();
            case StatType.MaxHealth: return GetMaxHealth();
            case StatType.DamageMultiplier: return GetDamageMultiplier();
            case StatType.AttackSpeed: return GetAttackSpeed();
            case StatType.DefenseMultiplier: return GetDefenseMultiplier();
            default: return 0f;
        }
    }

    /// <summary>
    /// Calculate final damage dealt by player (called by attack scripts)
    /// </summary>
    public int ApplyDamageMultiplier(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * GetDamageMultiplier());
    }

    /// <summary>
    /// Calculate final damage taken by player (called by Health component)
    /// </summary>
    public int ApplyDefenseMultiplier(int incomingDamage)
    {
        float defenseReduction = 1f - (GetDefenseMultiplier() - 1f);
        return Mathf.RoundToInt(incomingDamage * Mathf.Max(0.1f, defenseReduction));
    }

    /// <summary>
    /// Rebuild all bonuses from the current artifacts in inventory
    /// </summary>
    public void RecalculateFromArtifacts(IEnumerable<ItemSO> items)
    {
        // Reset bonuses
        moveSpeedBonus = 0f;
        maxHealthBonus = 0;
        damageMultiplierBonus = 0f;
        attackSpeedBonus = 0f;
        defenseMultiplierBonus = 0f;

        if (items != null)
        {
            foreach (var item in items)
            {
                if (item == null || item.isArtifact != 1) continue;

                switch (item.statToChange)
                {
                    case ItemSO.StatToChange.Power:
                        // +1% damage per point
                        damageMultiplierBonus += item.amountToChangeStat * 0.01f;
                        break;
                    case ItemSO.StatToChange.Agility:
                        // example: +0.1 move speed per point
                        moveSpeedBonus += item.amountToChangeStat * 0.1f;
                        break;
                    case ItemSO.StatToChange.Health:
                        maxHealthBonus += item.amountToChangeStat;
                        break;
                    // Intelligence and Coin removed
                }
            }
        }

        ApplyStatsToPlayer();
        OnStatsChanged?.Invoke();
    }
}

/// <summary>
/// Types of stats that can be modified by artifacts
/// </summary>
public enum StatType
{
    MoveSpeed,
    MaxHealth,
    DamageMultiplier,
    AttackSpeed,
    DefenseMultiplier
}
