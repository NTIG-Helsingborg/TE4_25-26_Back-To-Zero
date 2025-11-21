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

        // Spawn unparented so scale isn't inherited from SpellTransform
        var go = Object.Instantiate(spearPrefab, firePoint.position, firePoint.rotation, player.transform);

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