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
        if (!isInvincible)
        {
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

    // Unified API combining both branches
    public int GetMaxHealth() => maxHealth;
    public int GetCurrentHealth() => currentHealth;
    public bool IsFullHealth() => currentHealth >= maxHealth;
    public void InstantKill() => Die();

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

