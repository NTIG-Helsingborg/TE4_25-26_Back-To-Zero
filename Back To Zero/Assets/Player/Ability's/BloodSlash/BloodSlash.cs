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
    [SerializeField] private LayerMask hitLayers; // set to Enemy layer(s)

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            Debug.LogError("[BloodSlash] No Player tagged 'Player' in scene.");
            return;
        }

        var firePoint = FindChildByName(player.transform, firePointChildName);
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

        // Spawn unparented so scale isn't inherited from SpellTransform
        var go = Object.Instantiate(slashPrefab, firePoint.position, firePoint.rotation);

        var motion = go.GetComponent<SlashingMotion>();
        if (motion)
        {
            // Use firePoint aim as base angle, set desired reach (radius) and lifetime
            float baseAngle = firePoint.eulerAngles.z;
            float reach = 1.0f; // tune this to match your sprite/collider size
            motion.Initialize(player.transform, baseAngle, reach, lifetime);
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