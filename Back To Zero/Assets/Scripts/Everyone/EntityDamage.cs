using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StatusEffectData
{
    public StatusEffectType effectType;

    [Range(0f, 100f)]
    [Tooltip("How much the status bar fills per hit")]
    public float amountPerHit = 25f;
}

[System.Serializable]
public class EffectSettings
{
    public float tickDamage = 5f;
    public float tickInterval = 1f;
    public float tickDuration = 5f;
    public float ignitionDamage = 20f;
    public float slowAmount = 0.5f;
    public float slowDuration = 3f; 
    public Color color = Color.white;
}

public class EntityDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 10;

    [Header("Status Effect Settings")]
    public bool enableStatusEffects = false;

    [Tooltip("Max value each status effect can reach")]
    public float maxStatus = 100f;

    public StatusEffectData[] statusEffects;

    [Header("Effect Settings")]
    [Tooltip("Damage per tick when poison is full (only used if this enemy applies poison)")]
    public float poisonTickDamage = 5f;
    [Tooltip("How often poison ticks (only used if this enemy applies poison)")]
    public float poisonTickInterval = 1f;
    [Tooltip("How long poison effect lasts (only used if this enemy applies poison)")]
    public float poisonTickDuration = 5f;
    [Tooltip("Damage when burn reaches full (only used if this enemy applies burn)")]
    public float burnIgnitionDamage = 20f;
    [Tooltip("Movement speed multiplier when frost is full (only used if this enemy applies frost)")]
    public float frostSlowAmount = 0.5f;
    [Tooltip("How long frost slow effect lasts (only used if this enemy applies frost)")]
    public float frostSlowDuration = 3f;

    [Header("Health Bar Colors")]
    [Tooltip("Health bar color when poison is active (only used if this enemy applies poison)")]
    public Color poisonColor = new Color(0.2f, 0.8f, 0.2f);
    [Tooltip("Health bar color when frost is active (only used if this enemy applies frost)")]
    public Color frostColor = new Color(0.5f, 0.8f, 1f);
    [Tooltip("Health bar color when burn is active (only used if this enemy applies burn)")]
    public Color burnColor = new Color(1f, 0.4f, 0.2f);
    [Tooltip("Default health bar color")]
    public Color normalHealthColor = Color.red;

    private void OnCollisionEnter2D(Collision2D other)
    {
        Health targetHealth = other.gameObject.GetComponent<Health>();

        if (targetHealth != null && !targetHealth.isInvincible)
        {
            targetHealth.TakeDamage(damage);
            Debug.Log($"[{gameObject.name}] Dealt {damage} damage to {other.gameObject.name}");

            if (enableStatusEffects)
            {
                Debug.Log($"[{gameObject.name}] Status effects enabled, checking for EntityStatusEffect on {other.gameObject.name}");
                
                EntityStatusEffect targetStatus = other.gameObject.GetComponent<EntityStatusEffect>();
                if (targetStatus != null)
                {
                    Debug.Log($"[{gameObject.name}] Found EntityStatusEffect on {other.gameObject.name}, initializing with maxStatus: {maxStatus}");
                    targetStatus.Initialize(maxStatus);

                    // Pass effect settings to the status effect component
                    // Only pass settings for effects this enemy actually applies
                    var effectSettings = new Dictionary<StatusEffectType, EffectSettings>();
                    
                    foreach (var effect in statusEffects)
                    {
                        if (effect.effectType != StatusEffectType.None)
                        {
                            var settings = new EffectSettings();
                            
                            switch (effect.effectType)
                            {
                                case StatusEffectType.Poison:
                                    settings.tickDamage = poisonTickDamage;
                                    settings.tickInterval = poisonTickInterval;
                                    settings.tickDuration = poisonTickDuration;
                                    settings.color = poisonColor;
                                    break;
                                case StatusEffectType.Burn:
                                    settings.ignitionDamage = burnIgnitionDamage;
                                    settings.color = burnColor;
                                    break;
                                case StatusEffectType.Frost:
                                    settings.slowAmount = frostSlowAmount;
                                    settings.slowDuration = frostSlowDuration;
                                    settings.color = frostColor;
                                    break;
                            }
                            
                            effectSettings[effect.effectType] = settings;
                        }
                    }
                    
                    targetStatus.SetEffectSettings(effectSettings, normalHealthColor);

                    if (statusEffects != null && statusEffects.Length > 0)
                    {
                        Debug.Log($"[{gameObject.name}] Applying {statusEffects.Length} status effects");
                        foreach (var effect in statusEffects)
                        {
                            if (effect.effectType != StatusEffectType.None)
                            {
                                Debug.Log($"[{gameObject.name}] Applying {effect.effectType} with amount: {effect.amountPerHit}");
                                targetStatus.ApplyEffect(effect.effectType, effect.amountPerHit);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[{gameObject.name}] No status effects configured in statusEffects array");
                    }
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] No EntityStatusEffect component found on {other.gameObject.name}");
                }
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Status effects disabled");
            }
        }
        else
        {
            if (targetHealth == null)
            {
                Debug.Log($"[{gameObject.name}] No Health component found on {other.gameObject.name}");
            }
            else if (targetHealth.isInvincible)
            {
                Debug.Log($"[{gameObject.name}] {other.gameObject.name} is invincible, no damage dealt");
            }
        }
    }
}
