using System.Collections.Generic;
using UnityEngine;

public class ExpandingExplosion : MonoBehaviour
{
    private int damage;
    private float maxRadius;
    private float duration;
    private float knockbackForce;
    private GameObject owner;
    private LayerMask layers;
    private bool includeOwner;

    // Auto-harvest on kill
    private bool autoHarvestOnKill;
    private float healOnKillPercent;
    private Health healer; // player's Health
    private PlayerHandler playerHandler;

    private float t;
    private readonly HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    private GameObject vfxInstance;
    private SpriteRenderer vfxSprite;

    // Backward-compatible overload
    public void Initialize(
        int damage,
        float maxRadius,
        float duration,
        float knockbackForce,
        GameObject owner,
        LayerMask layers,
        GameObject vfxPrefab = null)
    {
        Initialize(damage, maxRadius, duration, knockbackForce, owner, layers, false, vfxPrefab, false, 0f, null, playerHandler);
    }

    // Full overload
    public void Initialize(
        int damage,
        float maxRadius,
        float duration,
        float knockbackForce,
        GameObject owner,
        LayerMask layers,
        bool includeOwner,
        GameObject vfxPrefab,
        bool autoHarvestOnKill,
        float healOnKillPercent,
        Health healer,
        PlayerHandler playerHandler)
    {
        this.damage = Mathf.Max(0, damage);
        this.maxRadius = Mathf.Max(0.01f, maxRadius);
        this.duration = Mathf.Max(0.05f, duration);
        this.knockbackForce = knockbackForce;
        this.owner = owner;
        this.layers = layers;
        this.includeOwner = includeOwner;

        this.autoHarvestOnKill = autoHarvestOnKill;
        this.healOnKillPercent = Mathf.Clamp01(healOnKillPercent);
        this.healer = healer;
        this.playerHandler = playerHandler;

        if (vfxPrefab != null)
        {
            vfxInstance = Instantiate(vfxPrefab, transform.position, Quaternion.identity, transform);
            PrepareVfxInstance(vfxInstance);
            vfxSprite = vfxInstance.GetComponentInChildren<SpriteRenderer>();
            UpdateVfxScale(0f);
        }
    }

    private void Update()
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / duration);
        float eased = 1f - (1f - p) * (1f - p);
        float currentRadius = maxRadius * eased;

        DoOverlap(currentRadius);
        UpdateVfxScale(currentRadius);

        if (t >= duration) Destroy(gameObject);
    }

    private void DoOverlap(float radius)
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, radius, layers);
        foreach (var h in hits)
        {
            if (!h) continue;
            var go = h.attachedRigidbody ? h.attachedRigidbody.gameObject : h.gameObject;

            if (!includeOwner && owner != null && go == owner) continue;
            if (!alreadyHit.Add(go)) continue;

            var health = go.GetComponent<Health>();
            if (health != null)
            {
                // Predict lethality before damage, then apply damage
                int maxH = health.GetMaxHealth();
                int curH = health.GetCurrentHealth();
                bool lethal = curH <= damage;

                health.TakeDamage(damage);

                if (autoHarvestOnKill && lethal && healer != null && healOnKillPercent > 0f)
                {
                    int healAmount = Mathf.RoundToInt(maxH * healOnKillPercent);
                    if (healAmount > 0) healer.Heal(healAmount);
                    if (playerHandler != null)
                        playerHandler.AddHarvestCharge();
                }
            }

            // Knockback
            Vector2 dir = ((Vector2)go.transform.position - (Vector2)transform.position).normalized;
            var kb = go.GetComponent<KnockbackReceiver>();
            if (kb != null) kb.ApplyKnockback(dir, knockbackForce);
            else
            {
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    private void PrepareVfxInstance(GameObject vfx)
    {
        foreach (var c in vfx.GetComponentsInChildren<Collider2D>(true)) Destroy(c);
        foreach (var rb in vfx.GetComponentsInChildren<Rigidbody2D>(true)) Destroy(rb);
        var proj = vfx.GetComponentInChildren<Projectiles>(true);
        if (proj) Destroy(proj);
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true)) ps.Play(true);
    }

    private void UpdateVfxScale(float radius)
    {
        if (!vfxInstance) return;
        if (vfxSprite != null && vfxSprite.sprite != null)
        {
            float spriteDiameter = vfxSprite.sprite.bounds.size.x;
            float targetDiameter = radius * 2f;
            float scale = spriteDiameter > 0f ? targetDiameter / spriteDiameter : 1f;
            vfxInstance.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            float d = radius * 2f;
            vfxInstance.transform.localScale = new Vector3(d, d, 1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        float currentRadius = (t <= 0f || duration <= 0f) ? 0f : maxRadius * Mathf.Clamp01(t / duration);
        Gizmos.DrawWireSphere(transform.position, currentRadius);
    }
}