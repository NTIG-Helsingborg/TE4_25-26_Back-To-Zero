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
    private readonly HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    public void Initialize(float damage, float knockbackForce, float lifetime, GameObject owner, LayerMask layers)
    {
        this.damage = Mathf.RoundToInt(damage);
        this.knockback = knockbackForce;
        this.ttl = Mathf.Max(0.01f, lifetime);
        this.owner = owner;
        this.hitLayers = layers;

        if (!col) col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        // Despawn after lifetime (now that ttl is set)
        Destroy(gameObject, this.ttl);

        // Immediate hit on spawn (now that hitLayers is set)
        InitialOverlapHit();
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void InitialOverlapHit()
    {
        if (!col) return;
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = hitLayers, useTriggers = true };
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
            Vector2 dir = (other.transform.position - (owner != null ? owner.transform.position : transform.position)).normalized;
            kb.ApplyKnockback(dir, knockback, 0.15f);
        }
    }
}