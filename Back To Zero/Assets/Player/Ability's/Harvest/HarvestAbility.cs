using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu]
public class HarvestAbility : Ability
{
    [Header("Harvest Settings")]
    public float harvestRange = 3f;
    public float healPercentage = 0.5f;
    public float harvestDuration = 0.3f;
    public LayerMask harvestLayers = ~0;
    
    [Header("Visual Effects")]
    public Color harvestColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    [Header("Harvest Feedback")]
    [SerializeField] private bool enableHarvestFeedback = true;
    [SerializeField] [Range(0.05f, 1f)] private float harvestShakeDuration = 0.12f;
    [SerializeField] [Range(0f, 50f)] private float harvestShakeIntensity = 6f;
    [SerializeField] [Range(0.05f, 1f)] private float harvestHealthThreshold = 0.4f;

    public static event Action HarvestSettingsChanged;
    public static bool IsHarvestFeedbackEnabled { get; private set; } = true;
    public static float GlobalHarvestShakeDuration { get; private set; } = 0.12f;
    public static float GlobalHarvestShakeIntensity { get; private set; } = 6f;
    public static float GlobalHarvestHealthThreshold { get; private set; } = 0.4f;
    
    private GameObject playerObject;
    private Health playerHealth;
    
    private void OnEnable()
    {
        ApplyHarvestFeedbackSettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyHarvestFeedbackSettings();
    }
#endif

    public override void Activate()
    {
        Debug.Log("Harvest activated!");
        ApplyHarvestFeedbackSettings();
        
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
        
        foreach (Collider2D collider in nearbyEntities)
        {
            if (collider.gameObject == playerObject || collider.transform.IsChildOf(playerObject.transform))
                continue;
            
            Health entityHealth = collider.GetComponent<Health>();
            if (entityHealth != null)
            {
                int healAmount = Mathf.RoundToInt(entityHealth.GetMaxHealth() * healPercentage);
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

    private void ApplyHarvestFeedbackSettings()
    {
        IsHarvestFeedbackEnabled = enableHarvestFeedback;
        GlobalHarvestShakeDuration = Mathf.Max(0.05f, harvestShakeDuration);
        GlobalHarvestShakeIntensity = Mathf.Max(0f, harvestShakeIntensity);
        GlobalHarvestHealthThreshold = Mathf.Clamp(harvestHealthThreshold, 0.01f, 1f);
        HarvestSettingsChanged?.Invoke();
    }
}

