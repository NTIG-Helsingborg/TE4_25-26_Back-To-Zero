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
    
    private GameObject playerObject;
    private Health playerHealth;
    
    public override void Activate()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;
        
        playerHealth = playerObject.GetComponent<Health>();
        if (playerHealth == null) return;
        
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

