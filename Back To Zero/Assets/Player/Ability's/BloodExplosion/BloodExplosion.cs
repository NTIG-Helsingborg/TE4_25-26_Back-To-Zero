using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Blood Explosion")]

public class BloodExplosion : Ability
{
    [Header("Blood Explosion Settings")]
    public float damage;
    public float radius;
    public float range;
    public float speed;
    public float HpCost;
    public bool IsAbility = true;

    [Header("Impact")]
    public float knockbackForce = 8f;
    public LayerMask damageLayers = ~0;

    [Header("References")]
    public GameObject BloodExplosionPrefab;
    [SerializeField] private string firePointChildName = "SpellTransform";

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) { Debug.LogError("[BloodExplosion] No Player."); return; }

        var firePoint = FindChildByName(player.transform, firePointChildName);
        if (!firePoint) { Debug.LogError("[BloodExplosion] Missing fire point."); return; }
        if (!BloodExplosionPrefab) { Debug.LogError("[BloodExplosion] Missing projectile prefab."); return; }

        var go = Object.Instantiate(BloodExplosionPrefab, firePoint.position, firePoint.rotation);

        // Move the projectile forward
        var proj = go.GetComponent<Projectiles>();
        if (proj != null)
        {
            float totalDamage = damage * PowerBonus.GetDamageMultiplier();
            proj.Initialize(totalDamage, speed, range, player, true, radius, true);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = firePoint.right * speed;
            Object.Destroy(go, Mathf.Max(0.01f, range / Mathf.Max(0.01f, speed)));
        }

        // Add impact knockback (no expanding explosion)
        var impactKB = go.GetComponent<KnockbackOnImpact>() ?? go.AddComponent<KnockbackOnImpact>();
        impactKB.Setup(knockbackForce, damageLayers, player, destroyOnImpact: true);

        var playerHealth = player.GetComponent<Health>();
        if (playerHealth != null && HpCost > 0f) playerHealth.SpendHealth(Mathf.RoundToInt(HpCost));
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}