using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Blood Knife")]
public class BloodKnife : Ability
{
    [Header("Blood Knife Settings")]
    public float damage;
    public float range; 
    public float speed;
    public float HpCost;
    public bool IsAbility = true;

    [Header("References")]
    public GameObject BloodKnifePrefab;

    [SerializeField] private string firePointChildName = "SpellTransform";
    [SerializeField, Tooltip("Clockwise is negative. E.g. -10 rotates a bit clockwise.")]
    private float spawnAngleOffsetDeg = -10f;

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[BloodKnife] No Player tagged 'Player' in scene.");
            return;
        }

        var firePoint = FindChildByName(player.transform, firePointChildName);
        if (firePoint == null)
        {
            Debug.LogError($"[BloodKnife] Could not find child '{firePointChildName}' under Player hierarchy.");
            return;
        }

        if (BloodKnifePrefab == null)
        {
            Debug.LogError("[BloodKnife] No projectile prefab assigned.");
            return;
        }

        // Spawn with a small visual rotation offset (clockwise is negative)
        Quaternion spawnRot = firePoint.rotation * Quaternion.Euler(0f, 0f, spawnAngleOffsetDeg);
        var go = Object.Instantiate(BloodKnifePrefab, firePoint.position, spawnRot);

        var proj = go.GetComponent<Projectiles>();
        if (proj != null)
        {
            float totalDamage = damage * PowerBonus.GetDamageMultiplier();
            // Move exactly along firePoint.right; visual rotation stays offset
            proj.Initialize(totalDamage, speed, range, player, firePoint.right);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = firePoint.right * speed;
            Object.Destroy(go, Mathf.Max(0.01f, range / Mathf.Max(0.01f, speed)));
        }

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