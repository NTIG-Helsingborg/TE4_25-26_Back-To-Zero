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
    [SerializeField] private LayerMask hitLayers;

    [SerializeField, Tooltip("Not applied at runtime when rotating prefab directly.")]
    private float spawnAngleOffsetDeg = -10f;

    [SerializeField, Tooltip("Sprite-only visual rotation (clockwise negative).")]
    private float visualRotationOffsetDeg = -10f;

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        var firePoint = FindChildByName(player.transform, firePointChildName);
        if (!firePoint || !slashPrefab) return;

        var go = Object.Instantiate(slashPrefab, firePoint.position, firePoint.rotation);

        // Visual rotation only
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.transform.localRotation = Quaternion.Euler(0f, 0f, visualRotationOffsetDeg);

        // Pass runtime values to the event driver
        var ev = go.GetComponent<SlashAnimationEvents>();
        if (ev)
        {
            float totalDamage = damage * PowerBonus.GetDamageMultiplier();
            ev.Setup(player, totalDamage, knockbackForce, hitLayers);
        }

        // Keep motion logic as-is
        var motion = go.GetComponent<SlashingMotion>();
        if (motion) motion.Initialize(player.transform, firePoint.eulerAngles.z, 1f, lifetime);

        // Auto-destroy after lifetime (fallback if SlashingMotion doesn’t do it)
        if (lifetime > 0f)
            Object.Destroy(go, lifetime);

        var playerHealth = player.GetComponent<Health>();
        if (playerHealth && HpCost > 0f)
            playerHealth.SpendHealth(Mathf.RoundToInt(HpCost));
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}