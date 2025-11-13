using UnityEngine;

public class Projectiles : MonoBehaviour
{
    private int damage;          
    private float speed;
    private float maxDistance;
    private GameObject owner;

    private Vector3 startPos;
    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (rb) rb.gravityScale = 0f;
    }

    public void Initialize(float damage, float speed, float range, GameObject owner)
    {
        this.damage = Mathf.RoundToInt(damage); 
        this.speed = speed;
        this.maxDistance = range;
        this.owner = owner;

        startPos = transform.position;

        if (rb) rb.linearVelocity = transform.right * speed;

        if (col && owner)
        {
            foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
                if (oc) Physics2D.IgnoreCollision(col, oc, true);
        }
    }

    private void Update()
    {
        if (!rb) transform.position += transform.right * (speed * Time.deltaTime);
        if (Vector3.Distance(startPos, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsOwner(other)) return;
        TryDamage(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsOwner(collision.collider)) return;
        TryDamage(collision.collider.gameObject);
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
            int finalDamage = damage;
            if (owner != null)
            {
                var ps = owner.GetComponent<PlayerStats>();
                if (ps != null)
                    finalDamage = ps.ApplyDamageMultiplier(damage);
            }

            health.TakeDamage(finalDamage);
            // Debug.Log($"Projectile dealt {finalDamage} (base {damage}) to {target.name}");
        }
        Destroy(gameObject);
    }
}