using UnityEngine;
using System.Collections.Generic;

public class BossBeam : MonoBehaviour
{
    [Header("Beam Properties")]
    public float beamLength = 20f;
    public float beamWidth = 0.5f;
    
    [Header("Collider Settings")]
    [Tooltip("If true, collider will use different size than visual sprite")]
    public bool useCustomColliderSize = false;
    public float colliderLength = 20f;
    public float colliderWidth = 0.5f;
    
    [Header("Damage Settings")]
    public int damage = 10;
    public float damageInterval = 0.5f; // Tick damage every 0.5 seconds
    
    [Header("Status Effect Settings")]
    public bool enableStatusEffects = false;
    public float maxStatus = 100f;
    public StatusEffectData[] statusEffects;

    [Header("Effect Settings")]
    public float poisonTickDamage = 5f;
    public float poisonTickInterval = 1f;
    public float poisonTickDuration = 5f;
    public float burnIgnitionDamage = 20f;
    public float frostSlowAmount = 0.5f;
    public float frostSlowDuration = 3f;

    [Header("Health Bar Colors")]
    public Color poisonColor = new Color(0.2f, 0.8f, 0.2f);
    public Color frostColor = new Color(0.5f, 0.8f, 1f);
    public Color burnColor = new Color(1f, 0.4f, 0.2f);
    public Color normalHealthColor = Color.red;
    
    [Header("Visual")]
    public bool useCustomSprite = false; // Toggle between sprite and solid color
    public Sprite customBeamSprite; // Assign a sprite in Inspector
    public Color beamColor = Color.red;
    [Tooltip("Enable color pulse effect (disable if using Animator)")]
    public bool usePulseEffect = true;
    public float pulseSpeed = 2f;
    public string sortingLayerName = "Default"; // Add sorting layer control
    public int sortingOrder = -5; // Lower number = render behind
    
    [Header("Animation")]
    [Tooltip("Optional: Assign an Animator for custom beam animations")]
    public Animator beamAnimator;
    
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D beamCollider;
    private Rigidbody2D rb;
    private Dictionary<GameObject, float> lastDamageTime = new Dictionary<GameObject, float>();

    void Start()
    {
        SetupBeam();
    }

    void SetupBeam()
    {
        // Setup Rigidbody2D (needed for collision detection)
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        
        // Setup Sprite Renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Use custom sprite or create a simple beam sprite
        if (beamAnimator != null && beamAnimator.runtimeAnimatorController != null)
        {
            // If using animator, let the animation control the sprite
            // But set a fallback sprite if provided
            if (useCustomSprite && customBeamSprite != null)
            {
                spriteRenderer.sprite = customBeamSprite; // Fallback sprite
                Debug.Log($"Beam: Set fallback sprite '{customBeamSprite.name}' for animator");
            }
            else
            {
                Debug.LogWarning("Beam: Animator is assigned but no custom sprite provided as fallback!");
            }
            spriteRenderer.color = beamColor;
            // Scale to desired size
            transform.localScale = new Vector3(beamLength, beamWidth, 1);
        }
        else if (useCustomSprite && customBeamSprite != null)
        {
            // Use static custom sprite if there's no animator
            spriteRenderer.sprite = customBeamSprite;
            Debug.Log($"Beam: Set static sprite '{customBeamSprite.name}'");
            spriteRenderer.color = beamColor;
            // Scale to desired size
            transform.localScale = new Vector3(beamLength, beamWidth, 1);
        }
        else
        {
            CreateBeamSprite();
            Debug.Log("Beam: Created procedural sprite");
            spriteRenderer.color = beamColor;
            // Scale to desired size
            transform.localScale = new Vector3(beamLength, beamWidth, 1);
        }
        
        spriteRenderer.sortingLayerName = sortingLayerName; // Set sorting layer
        spriteRenderer.sortingOrder = sortingOrder; // Set sorting order
        
        // Setup Animator if not assigned
        if (beamAnimator == null)
        {
            beamAnimator = GetComponent<Animator>();
        }
        
        // Enable animator if present
        if (beamAnimator != null)
        {
            beamAnimator.enabled = true;
            Debug.Log($"Beam animator enabled. Controller: {beamAnimator.runtimeAnimatorController?.name}");
        }
        
        // Setup Collider
        beamCollider = GetComponent<BoxCollider2D>();
        if (beamCollider == null)
        {
            beamCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        beamCollider.isTrigger = true; // Changed to true so player passes through
        
        // Use custom collider size or match visual size
        if (useCustomColliderSize)
        {
            beamCollider.size = new Vector2(colliderLength, colliderWidth);
            beamCollider.offset = new Vector2(colliderLength / 2f, 0);
        }
        else
        {
            beamCollider.size = new Vector2(beamLength, beamWidth);
            beamCollider.offset = new Vector2(beamLength / 2f, 0);
        }
    }

    void CreateBeamSprite()
    {
        // Create a simple rectangular texture for the beam
        Texture2D texture = new Texture2D(2, 2);
        Color[] pixels = new Color[4];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0, 0.5f), 1f);
        spriteRenderer.sprite = sprite;
    }

    void Update()
    {
        // Only use pulse effect if enabled and no animator is controlling the beam
        if (usePulseEffect && (beamAnimator == null || !beamAnimator.enabled))
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 0.3f) + 0.7f;
            spriteRenderer.color = beamColor * pulse;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Ignore boss and other enemies
        if (other.CompareTag("Boss") || other.CompareTag("Enemy"))
        {
            return;
        }

        // Only damage player
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Health targetHealth = other.GetComponent<Health>();

        if (targetHealth != null && !targetHealth.isInvincible)
        {
            // Check if enough time has passed for tick damage
            if (!lastDamageTime.ContainsKey(other.gameObject))
            {
                lastDamageTime[other.gameObject] = 0f;
            }

            if (Time.time - lastDamageTime[other.gameObject] >= damageInterval)
            {
                targetHealth.TakeDamage(damage);
                lastDamageTime[other.gameObject] = Time.time;
                Debug.Log($"[{gameObject.name}] Beam tick damage: {damage} to {other.gameObject.name}");

                if (enableStatusEffects)
                {
                    ApplyStatusEffectsToTarget(other.gameObject);
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Clean up when player leaves the beam
        if (lastDamageTime.ContainsKey(other.gameObject))
        {
            lastDamageTime.Remove(other.gameObject);
        }
    }

    private void ApplyStatusEffectsToTarget(GameObject target)
    {
        EntityStatusEffect targetStatus = target.GetComponent<EntityStatusEffect>();
        if (targetStatus != null)
        {
            targetStatus.Initialize(maxStatus);

            var effectSettings = new Dictionary<StatusEffectType, EffectSettings>();
            
            foreach (var effect in statusEffects)
            {
                if (effect.effectType != StatusEffectType.None)
                {
                    var settings = new EffectSettings();
                    
                    switch (effect.effectType)
                    {
                        case StatusEffectType.Poison:
                            settings.tickDamage = poisonTickDamage;
                            settings.tickInterval = poisonTickInterval;
                            settings.tickDuration = poisonTickDuration;
                            settings.color = poisonColor;
                            break;
                        case StatusEffectType.Burn:
                            settings.ignitionDamage = burnIgnitionDamage;
                            settings.color = burnColor;
                            break;
                        case StatusEffectType.Frost:
                            settings.slowAmount = frostSlowAmount;
                            settings.slowDuration = frostSlowDuration;
                            settings.color = frostColor;
                            break;
                    }
                    
                    effectSettings[effect.effectType] = settings;
                }
            }
            
            targetStatus.SetEffectSettings(effectSettings, normalHealthColor);

            if (statusEffects != null && statusEffects.Length > 0)
            {
                foreach (var effect in statusEffects)
                {
                    if (effect.effectType != StatusEffectType.None)
                    {
                        targetStatus.ApplyEffect(effect.effectType, effect.amountPerHit);
                    }
                }
            }
        }
    }
}
