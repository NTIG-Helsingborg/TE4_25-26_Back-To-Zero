using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu]
public class HarvestAbility : Ability
{
    public static event System.Action HarvestSettingsChanged;

    public static float GlobalHarvestActivationPercentage { get; private set; } = 20f;
    public static float GlobalHarvestShakeDuration { get; private set; } = 0.35f;
    public static float GlobalHarvestShakeIntensity { get; private set; } = 18f;

    public static float CurrentHarvestThresholdFraction => Mathf.Clamp01(GlobalHarvestActivationPercentage / 100f);

    [Header("Harvest Settings")]
    public float harvestRange = 3f;
    public float healPercentage = 0.5f;
    [Range(0f, 100f)]
    public float harvestActivationPercentage = 20f;
    public float harvestDuration = 0.3f;
    public LayerMask harvestLayers = ~0;
    
    [Header("Harvest Feedback")]
    [Tooltip("How long one shake cycle lasts while an enemy is harvestable.")]
    public float harvestShakeDuration = 0.35f;
    [Tooltip("How far the health bar is displaced per shake when harvestable.")]
    public float harvestShakeIntensity = 18f;
    
    [Header("Visual Effects")]
    public Color harvestColor = new Color(1f, 0.2f, 0.2f, 0.5f);
    
    private GameObject playerObject;
    private Health playerHealth;
    
    public override void Activate()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;
        
        playerHealth = playerObject.GetComponent<Health>();
        if (playerHealth == null) return;
        
        SyncGlobalSettings();
        
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
    }

    private void SyncGlobalSettings()
    {
        GlobalHarvestActivationPercentage = Mathf.Clamp(harvestActivationPercentage, 0f, 100f);
        GlobalHarvestShakeDuration = Mathf.Max(0.05f, harvestShakeDuration);
        GlobalHarvestShakeIntensity = Mathf.Max(0f, harvestShakeIntensity);
        HarvestSettingsChanged?.Invoke();
    }

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
            spriteRenderer.color = Color.Lerp(originalColor, flashColor, Mathf.PingPong(elapsed / duration * 4, 1));
            yield return null;
        }
        
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}

