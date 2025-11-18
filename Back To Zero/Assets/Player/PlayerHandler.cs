using UnityEngine;
using System.Collections;

public class PlayerHandler : MonoBehaviour
{
    [Header("Respawn Settings")]
    public Vector3 spawnPoint;
    public float respawnDelay = 2f;
    public bool useCurrentPositionAsSpawn = true;
    public bool freezePositionOnDeath = true;
    
    [Header("UI Settings")]
    public GameObject deathTxt;
    public float deathTxtFadeDuration = 0.5f;
    
    [Header("Ultimate Meter")]
    public float ultimateMaxCharge = 100f;
    [SerializeField] private float ultimateCharge = 0f;
    public float chargePerHarvest = 25f;
    public System.Action<float> OnUltimateChanged; // normalized 0..1

    private Health playerHealth;
    private bool isDead = false;
    private RigidbodyConstraints2D originalConstraints;
    private CanvasGroup deathTxtCanvasGroup;
    
    void Start()
    {
        playerHealth = GetComponent<Health>();
        
        if (useCurrentPositionAsSpawn)
        {
            spawnPoint = transform.position;
        }
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            originalConstraints = rb.constraints;
        }
        
        if (deathTxt != null)
        {
            deathTxtCanvasGroup = deathTxt.GetComponent<CanvasGroup>();
            if (deathTxtCanvasGroup == null)
            {
                deathTxtCanvasGroup = deathTxt.AddComponent<CanvasGroup>();
            }
            deathTxtCanvasGroup.alpha = 0f;
            deathTxt.SetActive(false);
        }
        
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHandler: No Health component found on player!");
        }
    }

    void Update()
    {
        if (playerHealth != null && !isDead)
        {
            if (playerHealth.GetCurrentHealth() <= 0)
            {
                Die();
            }
        }
    }
    
    void Die()
    {
        isDead = true;
        Debug.Log("Player died! Respawning in " + respawnDelay + " seconds...");
        
        if (freezePositionOnDeath)
        {
            FreezePosition(true);
        }
        
        if (deathTxt != null)
        {
            deathTxt.SetActive(true);
            StartCoroutine(FadeInDeathText());
        }
        
        DisablePlayerControls();
        StartCoroutine(RespawnCoroutine());
    }
    
    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }
    
    public void Respawn()
    {
        if (freezePositionOnDeath)
        {
            FreezePosition(false);
        }
        
        if (deathTxt != null)
        {
            if (deathTxtCanvasGroup != null)
            {
                deathTxtCanvasGroup.alpha = 0f;
            }
            deathTxt.SetActive(false);
        }
        
        transform.position = spawnPoint;
        
        if (playerHealth != null)
        {
            playerHealth.Heal(playerHealth.GetMaxHealth());
        }
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        EnablePlayerControls();
        isDead = false;
        
        Debug.Log("Player respawned!");
    }
    
    public void SetSpawnPoint(Vector3 newSpawnPoint)
    {
        spawnPoint = newSpawnPoint;
        Debug.Log("Spawn point updated to: " + spawnPoint);
    }
    
    void DisablePlayerControls()
    {
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this && script.enabled)
            {
                string scriptName = script.GetType().Name;
                if (scriptName.Contains("Move") || scriptName.Contains("Ability"))
                {
                    script.enabled = false;
                }
            }
        }
    }
    
    void EnablePlayerControls()
    {
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this)
            {
                string scriptName = script.GetType().Name;
                if (scriptName.Contains("Move") || scriptName.Contains("Ability"))
                {
                    script.enabled = true;
                }
            }
        }
    }
    
    void FreezePosition(bool freeze)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (freeze)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else
            {
                rb.constraints = originalConstraints;
            }
        }
    }
    
    IEnumerator FadeInDeathText()
    {
        if (deathTxtCanvasGroup == null) yield break;
        
        deathTxtCanvasGroup.alpha = 0f;
        float elapsed = 0f;
        
        while (elapsed < deathTxtFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / deathTxtFadeDuration);
            deathTxtCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, normalizedTime);
            yield return null;
        }
        
        deathTxtCanvasGroup.alpha = 1f;
    }

    public bool IsUltimateReady => ultimateCharge >= ultimateMaxCharge - 0.001f;
    public float UltimateNormalized => Mathf.Clamp01(ultimateCharge / Mathf.Max(0.0001f, ultimateMaxCharge));

    public void AddUltimateCharge(float amount)
    {
        if (ultimateMaxCharge <= 0f) return;
        float before = ultimateCharge;
        ultimateCharge = Mathf.Clamp(ultimateCharge + Mathf.Max(0f, amount), 0f, ultimateMaxCharge);
        if (ultimateCharge != before) OnUltimateChanged?.Invoke(UltimateNormalized);
    }

    public void AddHarvestCharge() => AddUltimateCharge(chargePerHarvest);

    public void ConsumeUltimate()
    {
        if (ultimateCharge <= 0f) return;
        ultimateCharge = 0f;
        OnUltimateChanged?.Invoke(UltimateNormalized);
    }
}
