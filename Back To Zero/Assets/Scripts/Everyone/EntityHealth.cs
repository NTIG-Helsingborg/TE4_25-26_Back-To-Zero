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

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer flashSpriteRenderer; // assign child SpriteRenderer in Inspector
    private Coroutine flashCoroutine;

    [Header("Animation")]
    [SerializeField] private Animator animator;                 // assign if Animator is on a child
    private string deathBoolParam = "Dead";    // Animator bool that triggers death anim
    private string deathStateTag = "Death";    // Tag your death state with this
    [SerializeField] private float destroyAfterDeathDelay = 0.1f;
    private string hurtBoolParam = "Hurt"; // Animator bool for hurt
    [SerializeField] private float hurtResetDelay = 0.2f;

    private RectTransform healthBarRect;
    private Vector3 healthBarOriginalLocalPosition;
    private Coroutine harvestShakeCoroutine;
    private bool isHarvestable;

    private void OnEnable()
    {
        CacheHealthBarRect();
        if (!animator) animator = GetComponentInChildren<Animator>(); // find child animator too
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

        // Hurt feedback
        PlayHurt();

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

            // Hurt feedback
            PlayHurt();

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

    private IEnumerator HandlePotDestruction()
    {
        // Wait for animation to complete (adjust time if needed)
        yield return new WaitForSeconds(0.9f);
        
        // Disable animator to prevent reverting to original sprite
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        // Wait a tiny bit more before destroying
        yield return new WaitForSeconds(0.1f);
        
        Destroy(gameObject);
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
        // Trigger death animation if animator exists
        if (animator != null && HasAnimatorParam(animator, deathBoolParam))
        {
            animator.SetBool(deathBoolParam, true);
        }

        LootBag lootBag = GetComponent<LootBag>();
        if (lootBag != null) lootBag.InstantiateLoot(transform.position);

        StopHarvestShake();
        Debug.Log(gameObject.name + " died");

        if (gameObject.CompareTag("Player"))
            return;

        // Don't destroy boss immediately - let BossAttack script handle it
        if (gameObject.CompareTag("Boss"))
        {
            Debug.Log("Boss died - letting BossAttack script handle destruction");
            return;
        }

        // Don't destroy pots immediately - let animation play first
        // Check by name or layer instead of tag to avoid tag not defined error
        // Special pot handling (kept as-is)
        if (gameObject.name.ToLower().Contains("pot"))
        {
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            StartCoroutine(HandlePotDestruction());
            return;
        }

        // Notify wave manager immediately (optional)
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyKilled();

        GrantExperience();

        // Disable collider and movement so the corpse doesn't interfere
        var col2d = GetComponent<Collider2D>();
        if (col2d) col2d.enabled = false;
        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d) { rb2d.linearVelocity = Vector2.zero; rb2d.isKinematic = true; }

        // Wait for death animation to finish, then destroy
        StartCoroutine(WaitAndDestroyAfterDeath());
    }

    private IEnumerator WaitAndDestroyAfterDeath()
    {
        if (animator != null)
        {
            // allow transition to start
            yield return null;

            float timeout = 5f; // safety
            while (timeout > 0f)
            {
                var s = animator.GetCurrentAnimatorStateInfo(0);
                // Wait until the state tagged "Death" finishes
                if (s.IsTag(deathStateTag) && s.normalizedTime >= 0.99f && !animator.IsInTransition(0))
                    break;

                timeout -= Time.deltaTime;
                yield return null;
            }

            if (destroyAfterDeathDelay > 0f)
                yield return new WaitForSeconds(destroyAfterDeathDelay);
        }

        Destroy(gameObject);
    }

    private static bool HasAnimatorParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters)
            if (p.name == name) return true;
        return false;
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

    public void Flash(Color flashColor, float duration)
    {
        if (flashSpriteRenderer == null || duration <= 0f) return;
        if (flashCoroutine != null) { StopCoroutine(flashCoroutine); flashCoroutine = null; }
        flashCoroutine = StartCoroutine(FlashRoutine(flashSpriteRenderer, flashColor, duration));
    }

    private IEnumerator FlashRoutine(SpriteRenderer sr, Color flashColor, float duration)
    {
        if (sr == null) yield break;
        Color originalColor = sr.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sr.color = Color.Lerp(originalColor, flashColor, Mathf.PingPong(elapsed / duration * 16f, 1f));
            yield return null;
        }

        sr.color = originalColor;
    }

    private void PlayHurt()
    {
        if (animator == null) return;
        if (HasAnimatorParam(animator, hurtBoolParam))
        {
            animator.SetBool(hurtBoolParam, true);
            StopCoroutine(nameof(ResetHurtBool));
            StartCoroutine(ResetHurtBool());
        }
        // Optional: quick flash on hit
        // Flash(Color.red, 0.1f);
    }

    private IEnumerator ResetHurtBool()
    {
        yield return new WaitForSeconds(hurtResetDelay);
        animator.SetBool(hurtBoolParam, false);
    }
}