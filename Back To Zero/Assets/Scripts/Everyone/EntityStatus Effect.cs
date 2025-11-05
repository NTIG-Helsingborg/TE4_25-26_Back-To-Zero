using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum StatusEffectType
{
    None,
    Poison,
    Frost,
    Burn
}

public class EntityStatusEffect : MonoBehaviour
{
    [Header("UI Components")]
    public Image Poison;
    public Image Frost;
    public Image Burn;
    [Tooltip("Drag the health bar image here for color changes")]
    public Image HealthBar;

    [Header("Position Settings")]
    public float barSpacing = 100f; // Distance between bars
    public float startYPosition = 100f; // Starting Y position for first bar

    [Header("Transition Settings")]
    public float fillSpeed = 5f; // How fast bars fill up
    public float decayDelay = 3f; // Seconds before decay starts
    public float decaySpeed = 2f; // How fast bars decay
    public float smoothPositionSpeed = 8f; // How fast bars move to new positions

    private float poisonValue, frostValue, burnValue;
    private float poisonTarget, frostTarget, burnTarget;

    private float maxStatus = 100f;

    // Track which effects are active and their order
    private List<StatusEffectType> activeEffects = new List<StatusEffectType>();

    // Decay timers
    private float poisonDecayTimer, frostDecayTimer, burnDecayTimer;

    // Effect timers and states
    private float poisonTickTimer;
    private bool poisonEffectActive, burnEffectActive, frostEffectActive;

    // Max effect duration timers
    private float poisonMaxTimer, frostMaxTimer, burnMaxTimer;
    private bool poisonAtMax, frostAtMax, burnAtMax;

    // Burn effect duration timer
    private float burnEffectTimer = 0f;
    private float burnEffectDuration = 2f; // How long burn effect stays active

    // References
    private Health healthComponent;
    private PlayerMove playerMoveComponent; // Player movement component
    private Rigidbody2D rbComponent; // Rigidbody2D component
    // private Image healthBarImage; // Reference to health bar image for color changes - REMOVED

    // Individual effect settings per enemy
    private Dictionary<StatusEffectType, EffectSettings> effectSettings = new Dictionary<StatusEffectType, EffectSettings>();
    private Color normalHealthColor = Color.red;

    public void Initialize(float maxStatus)
    {
        this.maxStatus = Mathf.Max(1f, maxStatus);
    }

    public void SetEffectSettings(Dictionary<StatusEffectType, EffectSettings> settings, Color normalHealthColor)
    {
        this.effectSettings = new Dictionary<StatusEffectType, EffectSettings>(settings);
        this.normalHealthColor = normalHealthColor;
    }

    void Start()
    {
        // Get component references
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No Health component found for status effects!");
        }
        
        // Get movement components
        playerMoveComponent = GetComponent<PlayerMove>();
        rbComponent = GetComponent<Rigidbody2D>();

        // Check if UI components are assigned
        if (Poison == null) Debug.LogWarning($"[{gameObject.name}] Poison UI Image is not assigned!");
        if (Frost == null) Debug.LogWarning($"[{gameObject.name}] Frost UI Image is not assigned!");
        if (Burn == null) Debug.LogWarning($"[{gameObject.name}] Burn UI Image is not assigned!");
        if (HealthBar == null) Debug.LogWarning($"[{gameObject.name}] Health Bar is not assigned!");
        
        // Initialize bar positions
        InitializeBarPositions();
    }
    
    private void InitializeBarPositions()
    {
        // Set initial positions for all bars
        if (Poison != null)
        {
            RectTransform poisonRect = Poison.GetComponent<RectTransform>();
            if (poisonRect != null)
            {
                Vector2 currentPos = poisonRect.anchoredPosition;
                poisonRect.anchoredPosition = new Vector2(currentPos.x, 100f);
            }
        }
        
        if (Frost != null)
        {
            RectTransform frostRect = Frost.GetComponent<RectTransform>();
            if (frostRect != null)
            {
                Vector2 currentPos = frostRect.anchoredPosition;
                frostRect.anchoredPosition = new Vector2(currentPos.x, 0f);
            }
        }
        
        if (Burn != null)
        {
            RectTransform burnRect = Burn.GetComponent<RectTransform>();
            if (burnRect != null)
            {
                Vector2 currentPos = burnRect.anchoredPosition;
                burnRect.anchoredPosition = new Vector2(currentPos.x, -100f);
            }
        }
    }

    void Update()
    {
        // Update decay timers
        UpdateDecayTimers();
        
        // Update current values - instant fill, smooth decay
        poisonValue = poisonTarget;
        frostValue = frostTarget;
        burnValue = burnTarget;

        // Check for full effects
        CheckFullEffects();

        UpdateUI();
    }

    private void CheckFullEffects()
    {
        // Update max timers
        if (poisonValue >= maxStatus && !poisonAtMax)
        {
            poisonAtMax = true;
            poisonMaxTimer = 0f;
        }
        else if (poisonValue < maxStatus && poisonAtMax)
        {
            poisonAtMax = false;
            poisonMaxTimer = 0f;
        }

        if (frostValue >= maxStatus && !frostAtMax)
        {
            frostAtMax = true;
            frostMaxTimer = 0f;
        }
        else if (frostValue < maxStatus && frostAtMax)
        {
            frostAtMax = false;
            frostMaxTimer = 0f;
        }

        if (burnValue >= maxStatus && !burnAtMax)
        {
            burnAtMax = true;
            burnMaxTimer = 0f;
        }
        else if (burnValue < maxStatus && burnAtMax)
        {
            burnAtMax = false;
            burnMaxTimer = 0f;
        }

        // Update timers
        if (poisonAtMax) poisonMaxTimer += Time.deltaTime;
        if (frostAtMax) frostMaxTimer += Time.deltaTime;
        if (burnAtMax) burnMaxTimer += Time.deltaTime;

        // Poison effect - ticks damage when full
        if (poisonValue >= maxStatus && !poisonEffectActive)
        {
            poisonEffectActive = true;
            poisonTickTimer = 0f;
        }

        if (poisonEffectActive)
        {
            poisonTickTimer += Time.deltaTime;
            
            // Get poison settings if available
            if (effectSettings.TryGetValue(StatusEffectType.Poison, out EffectSettings poisonSettings))
            {
                if (poisonTickTimer >= poisonSettings.tickInterval)
                {
                    poisonTickTimer = 0f;
                    ApplyPoisonDamage(poisonSettings.tickDamage);
                }
                
                // Check if poison duration has expired
                if (poisonMaxTimer >= poisonSettings.tickDuration)
                {
                    poisonTarget = 0f;
                    poisonValue = 0f; // Instantly reset the bar to 0
                    poisonEffectActive = false;
                    poisonAtMax = false;
                    poisonMaxTimer = 0f;
                }
            }
        }

        // Burn effect - instant damage when reaching full
        if (burnValue >= maxStatus && !burnEffectActive)
        {
            burnEffectActive = true;
            burnEffectTimer = 0f; // Reset burn effect timer
            
            // Apply instant damage and reset bar immediately
            if (effectSettings.TryGetValue(StatusEffectType.Burn, out EffectSettings burnSettings))
            {
                ApplyBurnDamage(burnSettings.ignitionDamage);
            }
            burnTarget = 0f;
            burnValue = 0f;
            burnAtMax = false;
            burnMaxTimer = 0f;
        }

        // Update burn effect timer (for health bar color)
        if (burnEffectActive)
        {
            burnEffectTimer += Time.deltaTime;
            if (burnEffectTimer >= burnEffectDuration)
            {
                burnEffectActive = false;
            }
        }

        // Frost effect - slows movement when full
        if (frostValue >= maxStatus && !frostEffectActive)
        {
            frostEffectActive = true;
            if (effectSettings.TryGetValue(StatusEffectType.Frost, out EffectSettings frostSettings))
            {
                ApplyFrostSlow(frostSettings.slowAmount);
            }
        }

        if (frostEffectActive)
        {
            if (effectSettings.TryGetValue(StatusEffectType.Frost, out EffectSettings frostSettings))
            {
                if (frostMaxTimer >= frostSettings.slowDuration)
                {
                    frostTarget = 0f;
                    frostValue = 0f; // Instantly reset the bar to 0
                    frostEffectActive = false;
                    frostAtMax = false;
                    frostMaxTimer = 0f;
                    RemoveFrostSlow();
                }
            }
        }

        UpdateHealthBarColor();
    }

    private void ApplyPoisonDamage(float damage)
    {
        if (healthComponent != null)
        {
            healthComponent.TakeDamage((int)damage);
        }
    }

    private void ApplyBurnDamage(float damage)
    {
        if (healthComponent != null)
        {
            healthComponent.TakeDamage((int)damage);
        }
    }

    private void ApplyFrostSlow(float slowAmount)
    {
        // Apply movement slow
        if (playerMoveComponent != null)
        {
            playerMoveComponent.MoveSpeed *= slowAmount;
        }
        else if (rbComponent != null)
        {
            rbComponent.linearDamping *= (1f / slowAmount);
        }
    }

    private void RemoveFrostSlow()
    {
        // Remove movement slow
        if (playerMoveComponent != null)
        {
            if (effectSettings.TryGetValue(StatusEffectType.Frost, out EffectSettings frostSettings))
            {
                playerMoveComponent.MoveSpeed /= frostSettings.slowAmount;
            }
        }
        else if (rbComponent != null)
        {
            if (effectSettings.TryGetValue(StatusEffectType.Frost, out EffectSettings frostSettings))
            {
                rbComponent.linearDamping /= (1f / frostSettings.slowAmount);
            }
        }
    }

    private void UpdateDecayTimers()
    {
        // Update timers
        if (poisonValue > 0f && !poisonEffectActive) poisonDecayTimer += Time.deltaTime;
        if (frostValue > 0f && !frostEffectActive) frostDecayTimer += Time.deltaTime;
        if (burnValue > 0f) burnDecayTimer += Time.deltaTime;

        // Apply decay after delay
        if (poisonDecayTimer >= decayDelay && poisonTarget > 0f && !poisonEffectActive)
        {
            poisonTarget = Mathf.Max(0f, poisonTarget - decaySpeed * Time.deltaTime);
        }
        
        if (frostDecayTimer >= decayDelay && frostTarget > 0f && !frostEffectActive)
        {
            frostTarget = Mathf.Max(0f, frostTarget - decaySpeed * Time.deltaTime);
        }
        
        if (burnDecayTimer >= decayDelay && burnTarget > 0f)
        {
            burnTarget = Mathf.Max(0f, burnTarget - decaySpeed * Time.deltaTime);
        }
    }

    private void UpdateUI()
    {
        UpdateBar(Poison, poisonValue, "Poison");
        UpdateBar(Frost, frostValue, "Frost");
        UpdateBar(Burn, burnValue, "Burn");
        
        // Update positions based on active effects
        UpdatePositions();
    }

    private void UpdateBar(Image bar, float value, string effectName)
    {
        if (bar == null) 
        {
            Debug.LogWarning($"[{gameObject.name}] {effectName} UI Image is not assigned!");
            return;
        }

        bool shouldBeVisible = value > 0f;
        bar.fillAmount = value / maxStatus;
        
        if (bar.gameObject.activeSelf != shouldBeVisible)
        {
            bar.gameObject.SetActive(shouldBeVisible);
        }
    }

    private void UpdatePositions()
    {
        activeEffects.Clear();
        
        if (poisonValue > 0f) activeEffects.Add(StatusEffectType.Poison);
        if (frostValue > 0f) activeEffects.Add(StatusEffectType.Frost);
        if (burnValue > 0f) activeEffects.Add(StatusEffectType.Burn);

        // Position each active effect with fixed positions (100, 0, -100)
        for (int i = 0; i < activeEffects.Count; i++)
        {
            Image effectBar = GetBarForEffect(activeEffects[i]);
            
            if (effectBar != null)
            {
                RectTransform rectTransform = effectBar.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float targetYPos;
                    switch (i)
                    {
                        case 0: targetYPos = 100f; break;  // First active bar
                        case 1: targetYPos = 0f; break;    // Second active bar
                        case 2: targetYPos = -100f; break; // Third active bar
                        default: targetYPos = 100f; break;
                    }
                    
                    Vector2 currentPos = rectTransform.anchoredPosition;
                    Vector2 targetPos = new Vector2(currentPos.x, targetYPos);
                    
                    // Smoothly move to target position
                    Vector2 newPos = Vector2.Lerp(currentPos, targetPos, smoothPositionSpeed * Time.deltaTime);
                    rectTransform.anchoredPosition = newPos;
                }
            }
        }
    }

    private Image GetBarForEffect(StatusEffectType effectType)
    {
        switch (effectType)
        {
            case StatusEffectType.Poison: return Poison;
            case StatusEffectType.Frost: return Frost;
            case StatusEffectType.Burn: return Burn;
            default: return null;
        }
    }

    public void ApplyEffect(StatusEffectType effectType, float amount)
    {
        switch (effectType)
        {
            case StatusEffectType.Poison:
                poisonTarget = Mathf.Clamp(poisonTarget + amount, 0f, maxStatus);
                poisonDecayTimer = 0f; // Reset decay timer
                if (Poison == null) Debug.LogWarning($"[{gameObject.name}] Poison UI Image is not assigned!");
                break;

            case StatusEffectType.Frost:
                frostTarget = Mathf.Clamp(frostTarget + amount, 0f, maxStatus);
                frostDecayTimer = 0f; // Reset decay timer
                if (Frost == null) Debug.LogWarning($"[{gameObject.name}] Frost UI Image is not assigned!");
                break;

            case StatusEffectType.Burn:
                burnTarget = Mathf.Clamp(burnTarget + amount, 0f, maxStatus);
                burnDecayTimer = 0f; // Reset decay timer
                if (Burn == null) Debug.LogWarning($"[{gameObject.name}] Burn UI Image is not assigned!");
                break;

            case StatusEffectType.None:
            default:
                break;
        }
    }

    private void UpdateHealthBarColor()
    {
        if (HealthBar == null) 
        {
            Debug.LogWarning($"[{gameObject.name}] Health bar image is null! Cannot change color.");
            return;
        }

        if (burnEffectActive && effectSettings.TryGetValue(StatusEffectType.Burn, out EffectSettings burnSettings))
        {
            HealthBar.color = burnSettings.color;
        }
        else if (frostEffectActive && effectSettings.TryGetValue(StatusEffectType.Frost, out EffectSettings frostSettings))
        {
            HealthBar.color = frostSettings.color;
        }
        else if (poisonEffectActive && effectSettings.TryGetValue(StatusEffectType.Poison, out EffectSettings poisonSettings))
        {
            HealthBar.color = poisonSettings.color;
        }
        else
        {
            HealthBar.color = normalHealthColor;
        }
    }
}
