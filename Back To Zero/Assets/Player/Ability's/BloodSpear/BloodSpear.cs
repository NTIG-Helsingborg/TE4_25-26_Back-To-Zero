using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Blood Spear")]
public class BloodSpear : Ability
{
    [Header("Spear Settings")]
    public float damage;
    public float knockbackForce;
    public float lifetime;
    public float HpCost;
    public bool IsAbility = true;
    
    [Header("References")]
    public GameObject spearPrefab;
    [SerializeField] private string firePointChildName = "SpellTransform";
    [SerializeField] private LayerMask hitLayers; // set to Enemy layer(s)

    // Add a small visual rotation tweak (clockwise is negative)
    [SerializeField, Tooltip("Clockwise is negative. E.g. -10 rotates a bit clockwise.")]
    private float spawnAngleOffsetDeg = -10f;

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            Debug.LogError("[BloodSpear] No Player tagged 'Player' in scene.");
            return;
        }

        var firePoint = FindChildByName(player.transform, firePointChildName);
        if (!firePoint)
        {
            Debug.LogError($"[BloodSpear] Could not find child '{firePointChildName}' under Player hierarchy.");
            return;
        }

        if (!spearPrefab)
        {
            Debug.LogError("[BloodSpear] No spear prefab assigned.");
            return;
        }

        // Apply a small Z rotation offset for better visuals
        Quaternion spawnRot = firePoint.rotation * Quaternion.Euler(0f, 0f, spawnAngleOffsetDeg);
        var go = Object.Instantiate(spearPrefab, firePoint.position, spawnRot, player.transform);

        var hitbox = go.GetComponent<MeleeHitbox>();
        if (hitbox != null)
        {
            float totalDamage = damage * PowerBonus.GetDamageMultiplier();
            hitbox.Initialize(totalDamage, knockbackForce, lifetime, player, hitLayers);
        }
        else
        {
            Debug.LogWarning("[BloodSpear] Prefab missing MeleeHitbox component.");
            Object.Destroy(go, Mathf.Max(0.01f, lifetime));
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