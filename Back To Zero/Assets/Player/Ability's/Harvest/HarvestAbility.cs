using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu]
public class HarvestAbility : Ability
{
    public static event System.Action HarvestSettingsChanged;

    public static float GlobalHarvestActivationPercentage { get; private set; } = 20f;
    public static bool IsHarvestFeedbackEnabled { get; private set; } = true;
    public static float GlobalHarvestShakeDuration { get; private set; } = 0.35f;
    public static float GlobalHarvestShakeIntensity { get; private set; } = 18f;
    public static float GlobalHarvestHealthThreshold { get; private set; } = 0.4f;

    public static float CurrentHarvestThresholdFraction => Mathf.Clamp01(GlobalHarvestActivationPercentage / 100f);

    [Header("Harvest Settings")]
    public float harvestRange = 3f;
    public float healPercentage = 0.5f;
    [Range(0f, 100f)]
    public float harvestActivationPercentage = 20f;
    public float harvestDuration = 0.3f;
    public LayerMask harvestLayers = ~0;
    
    [Header("Harvest Feedback")]
    [SerializeField] private bool enableHarvestFeedback = true;
    [Tooltip("How long one shake cycle lasts while an enemy is harvestable.")]
    [SerializeField] [Range(0.05f, 1f)] private float harvestShakeDuration = 0.35f;
    [Tooltip("How far the health bar is displaced per shake when harvestable.")]
    [SerializeField] [Range(0f, 50f)] private float harvestShakeIntensity = 18f;
    [SerializeField] [Range(0.05f, 1f)] private float harvestHealthThreshold = 0.4f;
    
    [Header("Visual Effects")]
    public Color harvestColor = new Color(1f, 0.2f, 0.2f, 0.5f);
    
    private GameObject playerObject;
    private Health playerHealth;
    
    private void OnEnable()
    {
        SyncGlobalSettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncGlobalSettings();
    }
#endif

    public override void Activate()
    {
        Debug.Log("Harvest activated!");
        SyncGlobalSettings();
        
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("Player not found!");
            return;
        }
        
        playerHealth = playerObject.GetComponent<Health>();
        if (playerHealth == null)
        {
            Debug.LogError("Player Health not found!");
            return;
        }
        
        MonoBehaviour monoBehaviour = playerObject.GetComponent<MonoBehaviour>();
        if (monoBehaviour != null)
        {
            monoBehaviour.StartCoroutine(PerformHarvest());
        }
    }
    
    private IEnumerator PerformHarvest()
    {
        if (playerHealth == null) yield break;
        
        Collider2D[] nearbyEntities = Physics2D.OverlapCircleAll(
            playerObject.transform.position, 
            harvestRange, 
            harvestLayers
        );
        
        List<GameObject> harvestedEntities = new List<GameObject>();
        int totalHealAmount = 0;

        float thresholdFraction = CurrentHarvestThresholdFraction;
        
        foreach (Collider2D collider in nearbyEntities)
        {
            if (collider.gameObject == playerObject || collider.transform.IsChildOf(playerObject.transform))
                continue;
            
            Health entityHealth = collider.GetComponent<Health>();
            if (entityHealth != null)
            {
                int maxHealth = entityHealth.GetMaxHealth();
                if (maxHealth <= 0)
                    continue;

                float currentFraction = (float)entityHealth.GetCurrentHealth() / maxHealth;
                if (currentFraction > thresholdFraction)
                    continue;

                int healAmount = Mathf.RoundToInt(maxHealth * healPercentage);
                totalHealAmount += healAmount;
                harvestedEntities.Add(collider.gameObject);
            }
        }
        
        if (harvestedEntities.Count > 0)
        {
            Debug.Log($"Harvesting {harvestedEntities.Count} entities for {totalHealAmount} HP");
            
            FreezeEntities(harvestedEntities, true);
            
            StartHarvestVisuals(harvestedEntities);
            yield return new WaitForSeconds(harvestDuration);
            
            foreach (GameObject entity in harvestedEntities)
            {
                if (entity != null)
                {
                    Health entityHealth = entity.GetComponent<Health>();
                    if (entityHealth != null)
                        entityHealth.InstantKill();
                }
            }
            
            if (playerHealth != null && totalHealAmount > 0)
                playerHealth.Heal(totalHealAmount);
        }
        else
        {
            Debug.Log("No entities in range to harvest");
        }
    }

    private void SyncGlobalSettings()
    {
        GlobalHarvestActivationPercentage = Mathf.Clamp(harvestActivationPercentage, 0f, 100f);
        IsHarvestFeedbackEnabled = enableHarvestFeedback;
        GlobalHarvestShakeDuration = Mathf.Max(0.05f, harvestShakeDuration);
        GlobalHarvestShakeIntensity = Mathf.Max(0f, harvestShakeIntensity);
        GlobalHarvestHealthThreshold = Mathf.Clamp(harvestHealthThreshold, 0.01f, 1f);
        HarvestSettingsChanged?.Invoke();
    }
    
    private void StartHarvestVisuals(List<GameObject> entities)
    {
        MonoBehaviour monoBehaviour = playerObject.GetComponent<MonoBehaviour>();
        if (monoBehaviour == null) return;
        
        foreach (GameObject entity in entities)
        {
            if (entity != null)
            {
                SpriteRenderer spriteRenderer = entity.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    monoBehaviour.StartCoroutine(FlashSprite(spriteRenderer, harvestColor, harvestDuration));
            }
        }
    }
    
    private IEnumerator FlashSprite(SpriteRenderer spriteRenderer, Color flashColor, float duration)
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(originalColor, flashColor, Mathf.PingPong(elapsed / duration * 16, 1));
            yield return null;
        }
        
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
    
    private void FreezeEntities(List<GameObject> entities, bool freeze)
    {
        foreach (GameObject entity in entities)
        {
            if (entity == null) continue;
            
            Rigidbody2D rb = entity.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (freeze)
                    rb.linearVelocity = Vector2.zero;
                rb.constraints = freeze ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.None;
            }
            
            MonoBehaviour[] scripts = entity.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                string scriptName = script.GetType().Name;
                if (scriptName.Contains("Move") || scriptName.Contains("Chase") || scriptName.Contains("AI"))
                    script.enabled = !freeze;
            }
        }
    }

    private void OnSuccessfulHarvest(Health victim)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            var handler = player.GetComponent<PlayerHandler>();
            if (handler) handler.AddHarvestCharge();
        }
        // ...existing heal logic...
    }
}

