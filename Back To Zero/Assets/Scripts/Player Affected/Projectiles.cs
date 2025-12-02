using UnityEngine;

public class Projectiles : MonoBehaviour
{
    private int damage;          
    private float speed;
    private float maxDistance;
    private GameObject owner;

    [Header("Explosion (optional)")]
    [SerializeField] private bool explodeOnImpact = false;
    [SerializeField] private float explosionRadius = 0f;
    [SerializeField] private bool includeOwnerInExplosion = false;
    [SerializeField] private LayerMask explosionLayers = ~0;        // who can be damaged in AoE
    [SerializeField] private GameObject explosionVfx = null;
    [SerializeField] private bool autoScaleExplosionVfx = true;
    [SerializeField] private float autoDestroyDelay = 0.5f;
    private Vector3 startPos;
    private Rigidbody2D rb;
    private Collider2D col;
    private bool destroyed;

    // Use a custom travel direction if provided (keeps aim exact while visuals can be rotated)
    private bool useCustomDir = false;
    private Vector2 travelDir;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (rb) rb.gravityScale = 0f;
    }

    public void Initialize(float damage, float speed, float range, GameObject owner)
    {
        Initialize(damage, speed, range, owner, false, 0f, false, ~0, null);
    }

    // New overload: supply travel direction explicitly
    public void Initialize(float damage, float speed, float range, GameObject owner, Vector2 travelDirection)
    {
        Initialize(damage, speed, range, owner);
        useCustomDir = true;
        travelDir = travelDirection.normalized;
        if (rb) rb.linearVelocity = travelDir * speed;
    }

    public void Initialize(
        float damage,
        float speed,
        float range,
        GameObject owner,
        bool explodeOnImpact,
        float explosionRadius = 0f,
        bool includeOwnerInExplosion = false,
        int explosionLayerMask = ~0,
        GameObject explosionVfx = null)
    {
        this.damage = Mathf.RoundToInt(damage); 
        this.speed = speed;
        this.maxDistance = range;
        this.owner = owner;

        this.explodeOnImpact = explodeOnImpact;
        this.explosionRadius = explosionRadius;
        this.includeOwnerInExplosion = includeOwnerInExplosion;
        this.explosionLayers = explosionLayerMask;
        if (explosionVfx != null) this.explosionVfx = explosionVfx;

        startPos = transform.position;

        if (rb && !useCustomDir) rb.linearVelocity = transform.right * speed;

        if (col && owner)
        {
            foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
                if (oc) Physics2D.IgnoreCollision(col, oc, true);
        }
    }

    private void Update()
    {
        if (!rb)
        {
            Vector2 dir = useCustomDir ? travelDir : (Vector2)transform.right;
            transform.position += (Vector3)(dir * (speed * Time.deltaTime));
        }
        if (Vector3.Distance(startPos, transform.position) >= maxDistance)
            DestroySelf();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsOwner(other)) return;
        HandleImpact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsOwner(collision.collider)) return;
        HandleImpact(collision.collider);
    }
    private void HandleImpact(Collider2D hit)
    {
        if (destroyed) return;

        if (explodeOnImpact && explosionRadius > 0f)
        {
            DoExplosion();
        }
        else
        {
            TryDamage(hit.gameObject);
            DestroySelf();
        }
    }

    private bool IsOwner(Collider2D hit)
    {
        if (!owner) return false;
        var hitRb = hit.attachedRigidbody;
        return hitRb && hitRb.gameObject == owner;
    }

    private void TryDamage(GameObject target)
    {
        var health = target.GetComponent<Health>();
        if (health != null && !health.isInvincible)
        {
            health.TakeDamage(damage);
            // Debug.Log($"Projectile dealt {damage} to {target.name}");
        }
    }

    private void DoExplosion()
    {
        if (explosionVfx)
        {
            var vfx = Instantiate(explosionVfx, transform.position, Quaternion.identity);
            if (autoScaleExplosionVfx) ScaleToRadius(vfx.transform, explosionRadius);
            Destroy(vfx, autoDestroyDelay);
        }

        var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, explosionLayers);
        foreach (var h in hits)
        {
            if (!includeOwnerInExplosion && IsOwner(h)) continue;

            var health = h.GetComponent<Health>();
            if (health != null && !health.isInvincible)
            {
                health.TakeDamage(damage);
            }
        }

        DestroySelf();
    }
    private void ScaleToRadius(Transform root, float radius)
    {
        if (radius <= 0f || root == null) return;
        var sr = root.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        float spriteDiameter = sr.sprite.bounds.size.x;          // in world units at scale (1,1,1)
        if (spriteDiameter <= 0f) return;

        float targetDiameter = radius * 2f;
        float scale = targetDiameter / spriteDiameter;
        root.localScale = new Vector3(scale, scale, 1f);
    }
    private void DestroySelf()
    {
        if (destroyed) return;
        destroyed = true;
        Destroy(gameObject);
    }
    private void OnDrawGizmos()
    {
        if (explodeOnImpact && explosionRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}