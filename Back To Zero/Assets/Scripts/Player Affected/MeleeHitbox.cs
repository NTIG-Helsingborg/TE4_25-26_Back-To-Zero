using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeleeHitbox : MonoBehaviour
{
    private int damage;
    private float knockback;
    private float ttl;
    private GameObject owner;
    private LayerMask hitLayers;

    private Collider2D col;
    private Rigidbody2D rb;
    private readonly HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    public void Initialize(float damage, float knockbackForce, float lifetime, GameObject owner, LayerMask layers)
    {
        this.damage = Mathf.RoundToInt(damage);
        this.knockback = knockbackForce;
        this.ttl = Mathf.Max(0.01f, lifetime);
        this.owner = owner;
        this.hitLayers = layers;

        if (!col) col = GetComponent<Collider2D>();
        EnsureColliderConfigured(col);
        col.enabled = true;

        // Immediate hit on spawn/enable
        InitialOverlapHit();

        // Auto-disable after window
        CancelInvoke(nameof(EndWindow));
        Invoke(nameof(EndWindow), this.ttl);
    }

    // Call this from an Animation Event to open a short hit window
    public void ActivateWindow(int dmg, float kbForce, GameObject owner, LayerMask layers, float duration)
    {
        damage = dmg;
        knockback = kbForce;
        this.owner = owner;
        hitLayers = layers;
        ttl = Mathf.Max(0.01f, duration);
        alreadyHit.Clear();

        if (!col) col = GetComponent<Collider2D>();
        EnsureColliderConfigured(col);
        col.enabled = true;

        InitialOverlapHit();

        CancelInvoke(nameof(EndWindow));
        Invoke(nameof(EndWindow), ttl);
    }

    private void EndWindow()
    {
        if (col) col.enabled = false;
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        EnsureColliderConfigured(col);

        // Ensure at least one RB2D exists for trigger callbacks
        rb = GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.simulated = true;
        }
    }

    private static void EnsureColliderConfigured(Collider2D c)
    {
        if (!c) return;
        c.isTrigger = true;

        var edge = c as EdgeCollider2D;
        if (edge != null && edge.pointCount < 2)
        {
            Debug.LogWarning("[MeleeHitbox] EdgeCollider2D has fewer than 2 points.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void InitialOverlapHit()
    {
        if (!col) return;

        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = hitLayers,
            useTriggers = true
        };

        var results = new List<Collider2D>(16);
        col.Overlap(filter, results); // correct API

        foreach (var c in results) TryHit(c);
    }

    private void TryHit(Collider2D other)
    {
        if (!other) return;
        var go = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

        if (go == owner) return;
        if (((1 << go.layer) & hitLayers) == 0) return;
        if (!alreadyHit.Add(go)) return;

        var health = go.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        var kb = other.GetComponent<KnockbackReceiver>();
        if (kb != null)
        {
            Vector2 origin = owner != null ? (Vector2)owner.transform.position : (Vector2)transform.position;
            Vector2 dir = ((Vector2)other.transform.position - origin).normalized;
            kb.ApplyKnockback(dir, knockback, 0.15f);
        }
    }
}