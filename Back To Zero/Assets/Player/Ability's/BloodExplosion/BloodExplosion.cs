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

    [Header("References")]
    public GameObject BloodExplosionPrefab;
    [SerializeField] private string firePointChildName = "SpellTransform";

     public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[BloodExplosion] No Player tagged 'Player' in scene.");
            return;
        }

        var firePoint = FindChildByName(player.transform, firePointChildName);
        if (firePoint == null)
        {
            Debug.LogError($"[BloodExplosion] Could not find child '{firePointChildName}' under Player hierarchy.");
            return;
        }

        if (BloodExplosionPrefab == null)
        {
            Debug.LogError("[BloodExplosion] No projectile prefab assigned.");
            return;
        }

         // var ph = player.GetComponent<Health>();

        var go = Object.Instantiate(BloodExplosionPrefab, firePoint.position, firePoint.rotation);

        var proj = go.GetComponent<Projectiles>();
        if (proj != null)
        {
            proj.Initialize(damage, speed, range, player, true, radius, true);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = firePoint.right * speed; // fix: use velocity
            Object.Destroy(go, Mathf.Max(0.01f, range / Mathf.Max(0.01f, speed)));
        }

        // Charge HP cost after successful spawn (ignores invincibility)
        var playerHealth = player.GetComponent<Health>();
        if (playerHealth != null && HpCost > 0f)
        {
            playerHealth.SpendHealth(Mathf.RoundToInt(HpCost));
        }

    }
     private static Transform FindChildByName(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}