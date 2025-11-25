using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public bool isInvincible = false;
    public Image healthBar;

    [Header("Shield")]
    [SerializeField] private Image shieldBar;
    [SerializeField] private int currentShield = 0;
    [SerializeField] private float shieldTimer = 0f;

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
        UpdateShieldBar();
        EvaluateHarvestability();
    }

    private void Update()
    {
        // Handle shield decay
        if (currentShield > 0 && shieldTimer > 0f)
        {
            shieldTimer -= Time.deltaTime;
            
            if (shieldTimer <= 0f)
            {
                RemoveShield(currentShield);
            }
        }
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
        if (!isInvincible)
        {
            // Shield absorbs damage first
            if (currentShield > 0)
            {
                damage = AbsorbDamage(damage);
            }
            
            currentHealth -= damage;
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

    // Shield Methods
    public void AddShield(int amount, float duration)
    {
        currentShield += amount;
        shieldTimer = Mathf.Max(shieldTimer, duration);
        UpdateShieldBar();
    }

    private int AbsorbDamage(int damage)
    {
        if (currentShield <= 0)
            return damage;

        if (damage <= currentShield)
        {
            // Shield absorbs all damage
            RemoveShield(damage);
            return 0;
        }
        else
        {
            // Shield absorbs part, overflow goes to health
            int overflow = damage - currentShield;
            RemoveShield(currentShield);
            return overflow;
        }
    }

    private void RemoveShield(int amount)
    {
        currentShield -= amount;
        if (currentShield < 0)
            currentShield = 0;
            
        if (currentShield == 0)
            shieldTimer = 0f;
            
        UpdateShieldBar();
    }

    private void UpdateShieldBar()
    {
        if (shieldBar != null && maxHealth > 0)
        {
            // Display shield as a fraction of max health
            float shieldFraction = (float)currentShield / maxHealth;
            shieldBar.fillAmount = Mathf.Clamp(shieldFraction, 0f, 1f);
            shieldBar.enabled = currentShield > 0;
        }
    }

    public int GetCurrentShield() => currentShield;

    // Unified API combining both branches
    public int GetMaxHealth() => maxHealth;
    public int GetCurrentHealth() => currentHealth;
    public bool IsFullHealth() => currentHealth >= maxHealth;
    public void InstantKill() => Die();

    public void IncreaseMaxHealth(int amount)
    {
        // If currentHealth hasn't been initialized yet (is 0), initialize it to maxHealth first
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
        
        maxHealth += amount;
        if (maxHealth < 1)
        {
            maxHealth = 1; // Ensure maxHealth never goes below 1
        }
        
        // Also increase current health by the same amount when maxHealth increases
        if (amount > 0)
        {
            currentHealth += amount;
        }
        
        // Clamp current health to not exceed new max
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        // Ensure current health doesn't go below 1
        if (currentHealth < 1)
        {
            currentHealth = 1;
        }
        
        RefreshHealthBarFill();
        UpdateShieldBar();
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
        LootBag lootBag = GetComponent<LootBag>();
        if (lootBag != null)
        {
            lootBag.InstantiateLoot(transform.position);
        }
        else
        {
            Debug.Log(gameObject.name + " died but has no LootBag component.");
        }

        StopHarvestShake();
        Debug.Log(gameObject.name + " died");

        if (gameObject.CompareTag("Player"))
        {
            return;
        }

        // Notify WaveManager that an enemy died
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyKilled();
        }

        GrantExperience();

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
        if (ExperienceManager.Instance == null)
        {
            Debug.LogWarning($"No ExperienceManager instance found when trying to grant experience from {gameObject.name}");
            return;
        }

        // Additional safety: Verify the instance is still active
        if (ExperienceManager.Instance.gameObject == null || !ExperienceManager.Instance.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"ExperienceManager instance is not active when trying to grant experience from {gameObject.name}");
            return;
        }

        ExperienceManager.Instance.AddExperience(experienceReward);
    }
}