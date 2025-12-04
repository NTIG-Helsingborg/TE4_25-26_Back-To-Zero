using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Blood Slash")]
public class BloodSlash : Ability
{
    [Header("Slash Settings")]
    public float damage;
    public float knockbackForce;
    public float lifetime;
    public float HpCost;

    public bool IsAbility = true;
    

    [Header("References")]
    public GameObject slashPrefab;
    [SerializeField] private string firePointChildName = "SpellTransform";
    [SerializeField] private string pivotChildName = "RotationPoint"; // child used as rotation center
    [SerializeField] private LayerMask hitLayers; // set to Enemy layer(s)

    [Header("Visual")]
    [SerializeField] private float visualRotationOffsetDeg = 0f; // adjust to match sprite forward

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            Debug.LogError("[BloodSlash] No Player tagged 'Player' in scene.");
            return;
        }

        var firePoint = FindChildByName(player.transform, firePointChildName);
        var pivot = FindChildByName(player.transform, pivotChildName) ?? player.transform;
        if (!firePoint)
        {
            Debug.LogError($"[BloodSlash] Could not find child '{firePointChildName}' under Player hierarchy.");
            return;
        }
        if (!slashPrefab)
        {
            Debug.LogError("[BloodSlash] No slash prefab assigned.");
            return;
        }

        // Spawn and parent to pivot so it follows movement, but keep world pose
        var go = Object.Instantiate(slashPrefab, firePoint.position, firePoint.rotation);
        // Apply visual offset so sprite faces correctly
        go.transform.rotation = firePoint.rotation * Quaternion.Euler(0f, 0f, visualRotationOffsetDeg);

        var motion = go.GetComponent<SlashingMotion>();
        if (motion)
        {
            float baseAngle = firePoint.eulerAngles.z;
            float reach = 1.0f;
            motion.Initialize(pivot, baseAngle, reach, lifetime);
            
            motion.SnapToStart();
        }

        var hitbox = go.GetComponent<MeleeHitbox>();
        if (hitbox != null)
        {
            float totalDamage = damage * PowerBonus.GetDamageMultiplier();
            hitbox.Initialize(totalDamage, knockbackForce, lifetime, player, hitLayers);
        }
        else
        {
            Debug.LogWarning("[BloodSlash] Prefab missing MeleeHitbox component.");
            Object.Destroy(go, Mathf.Max(0.01f, lifetime));
        }

        var playerHealth = player.GetComponent<Health>();
        if (playerHealth != null && HpCost > 0f)
            playerHealth.SpendHealth(Mathf.RoundToInt(HpCost));
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}