using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KnockbackOnImpact : MonoBehaviour
{
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private GameObject owner;
    [SerializeField] private bool destroyOnImpact = true;

    private bool done;

    public void Setup(float knockbackForce, LayerMask layers, GameObject owner, bool destroyOnImpact = true)
    {
        this.knockbackForce = knockbackForce;
        this.hitLayers = layers;
        this.owner = owner;
        this.destroyOnImpact = destroyOnImpact;

        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // use triggers for impact
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Impact(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Impact(collision.rigidbody ? collision.rigidbody.gameObject : collision.gameObject);
    }

    private void Impact(GameObject hit)
    {
        if (done || hit == null) return;
        if (owner != null && hit == owner) return;
        if (((1 << hit.layer) & hitLayers) == 0) return;

        // Apply knockback once
        Vector2 dir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
        var kb = hit.GetComponent<KnockbackReceiver>();
        if (kb != null) kb.ApplyKnockback(dir, knockbackForce);
        else
        {
            var rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null) rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        }

        done = true;
        if (destroyOnImpact) Destroy(gameObject);
    }
}