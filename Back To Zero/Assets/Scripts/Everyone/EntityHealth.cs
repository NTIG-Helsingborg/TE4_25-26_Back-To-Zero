using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public bool isInvincible = false;
    public Image healthBar;

    [Header("Experience Reward")]
    [SerializeField] private bool grantsExperienceOnDeath = false;
    [SerializeField] private int experienceReward = 10;

    [Header("Harvest Feedback")]
    [SerializeField] private bool enableHarvestFeedback = true;

    private RectTransform healthBarRect;
    private Vector3 healthBarOriginalLocalPosition;
    private Coroutine harvestShakeCoroutine;
    private bool isHarvestable;

    private void OnEnable()
    {
        CacheHealthBarRect();
        HarvestAbility.HarvestSettingsChanged += OnHarvestSettingsChanged;
        healthBarOriginalLocalPosition = healthBarRect != null ? healthBarRect.localPosition : Vector3.zero;
        EvaluateHarvestability();
    }

    private void OnDisable()
    {
        HarvestAbility.HarvestSettingsChanged -= OnHarvestSettingsChanged;
        StopHarvestShake();
    }

    private void Start()
    {
        // Only set to max health if currentHealth hasn't been set (is 0)
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
        RefreshHealthBarFill();
        EvaluateHarvestability();
    }

    public void SpendHealth(int amount)
    {
        if (amount <= 0) return;

        // ignore isInvincible on purpose
        currentHealth -= amount;

        RefreshHealthBarFill();
        EvaluateHarvestability();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        // Prevent taking damage if already dead
        if (currentHealth == int.MinValue)
        {
            return;
        }

        if (!isInvincible)
        {
            int incoming = damage;

            // If this Health is on the Player, apply defense multiplier
            if (TryGetComponent<PlayerStats>(out var stats))
            {
                incoming = stats.ApplyDefenseMultiplier(damage);
            }

            currentHealth -= incoming;
            RefreshHealthBarFill();
            EvaluateHarvestability();
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        RefreshHealthBarFill();
        EvaluateHarvestability();
    }

    // Unified API combining both branches
    public int GetMaxHealth() => maxHealth;
    public int GetCurrentHealth() => currentHealth;
    public bool IsFullHealth() => currentHealth >= maxHealth;
    public void InstantKill() => Die();
    
    public void SetMaxHealth(int newMaxHealth)
    {
        if (newMaxHealth > 0)
        {
            maxHealth = newMaxHealth;
            // Ensure current health doesn't exceed new max
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            RefreshHealthBarFill();
        }
    }

    private void RefreshHealthBarFill()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = Mathf.Clamp((float)currentHealth / maxHealth, 0f, 1f);
        }
    }

    private void EvaluateHarvestability()
    {
        if (!enableHarvestFeedback || maxHealth <= 0)
        {
            SetHarvestable(false);
            return;
        }

        float threshold = HarvestAbility.CurrentHarvestThresholdFraction;
        if (threshold <= 0f)
        {
            SetHarvestable(false);
            return;
        }

        bool shouldHarvest = (float)currentHealth / maxHealth <= threshold;
        SetHarvestable(shouldHarvest);
    }

    private void SetHarvestable(bool value)
    {
        if (isHarvestable == value)
            return;

        isHarvestable = value;

        if (isHarvestable)
        {
            StartHarvestShake();
        }
        else
        {
            StopHarvestShake();
        }
    }

    private void StartHarvestShake()
    {
        CacheHealthBarRect();
        if (healthBarRect == null)
            return;

        if (harvestShakeCoroutine != null)
        {
            StopCoroutine(harvestShakeCoroutine);
        }

        harvestShakeCoroutine = StartCoroutine(HarvestShakeRoutine());
    }

    private IEnumerator HarvestShakeRoutine()
    {
        if (healthBarRect == null)
            yield break;

        while (isHarvestable)
        {
            float time = Time.unscaledTime;
            float cycleDuration = Mathf.Max(0.05f, HarvestAbility.GlobalHarvestShakeDuration);
            float cyclePhase = Mathf.PingPong(time, cycleDuration) / cycleDuration;
            float damper = Mathf.Lerp(0.6f, 1f, cyclePhase);
            float intensity = HarvestAbility.GlobalHarvestShakeIntensity;
            Vector2 offset = Random.insideUnitCircle * intensity * damper;
            healthBarRect.localPosition = healthBarOriginalLocalPosition + (Vector3)offset;
            yield return null;
        }

        StopHarvestShake();
    }

    private void StopHarvestShake()
    {
        if (harvestShakeCoroutine != null)
        {
            StopCoroutine(harvestShakeCoroutine);
            harvestShakeCoroutine = null;
        }

        if (healthBarRect != null)
        {
            healthBarRect.localPosition = healthBarOriginalLocalPosition;
        }
    }

    private void CacheHealthBarRect()
    {
        if (healthBar != null)
        {
            healthBarRect = healthBar.rectTransform;
        }
    }

    private void OnHarvestSettingsChanged()
    {
        CacheHealthBarRect();
        if (healthBarRect != null)
        {
            healthBarOriginalLocalPosition = healthBarRect.localPosition;
        }
        EvaluateHarvestability();
    }

    private void Die()
    {
        // Prevent multiple death calls
        if (currentHealth <= 0 && currentHealth != int.MinValue)
        {
            currentHealth = int.MinValue; // Mark as dead
        }
        else if (currentHealth == int.MinValue)
        {
            return; // Already dead, prevent double execution
        }

        StopHarvestShake();

        // Handle player death differently
        if (gameObject.CompareTag("Player"))
        {
            Debug.Log("Player died");
            return;
        }

        // Notify WaveManager that an enemy died
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyKilled();
        }

        GrantExperience();
        Debug.Log(gameObject.name + " died");

        // Try to drop loot
        try
        {
            LootBag lootBag = GetComponent<LootBag>();
            if (lootBag != null)
            {
                lootBag.InstantiateLoot(transform.position);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error dropping loot from {gameObject.name}: {e.Message}");
        }

        // Grant experience
        try
        {
            GrantExperience();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error granting experience from {gameObject.name}: {e.Message}");
        }

        // Destroy the object
        Destroy(gameObject);
    }

    private void GrantExperience()
    {
        if (!grantsExperienceOnDeath)
        {
            return;
        }

        if (experienceReward <= 0)
        {
            return;
        }

        // Safety: Check if ExperienceManager instance exists and is valid
        ExperienceManager expManager = ExperienceManager.Instance;
        
        // Fallback: find the Player and get its ExperienceManager if Instance is null
        if (expManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                expManager = player.GetComponent<ExperienceManager>();
            }
        }
        
        if (expManager == null)
        {
            Debug.LogWarning($"No ExperienceManager found when trying to grant {experienceReward} XP from {gameObject.name}");
            return;
        }

        // Additional safety: Verify the instance is still active
        if (expManager.gameObject == null || !expManager.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"ExperienceManager instance is not active when trying to grant experience from {gameObject.name}");
            return;
        }

        expManager.AddExperience(experienceReward);
    }
}

