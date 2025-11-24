using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Blood Whip")]
public class BloodWhip : Ability
{
    [Header("Whip Settings")]
    public float damage;
    public float knockbackForce;
    public float maxLength = 5f;
    public float thickness = 0.15f;
    public float HpCost;
    public bool IsAbility = true;

    [Tooltip("How many points make up the whip curve (>= 2).")]
    public int segmentCount = 16;

    [Header("Timing")]
    public float extendTime = 0.12f;
    public float holdTime = 0.05f;
    public float retractTime = 0.15f;

    [Header("References")]
    public GameObject whipPrefab;
    [SerializeField] private string firePointChildName = "SpellTransform";
    [SerializeField] private LayerMask hitLayers = ~0; // default: everything

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            Debug.LogError("[BloodWhip] No Player tagged 'Player' in scene.");
            return;
        }

        if (!whipPrefab)
        {
            Debug.LogError("[BloodWhip] No whip prefab assigned.");
            return;
        }

        var firePoint = FindChildByName(player.transform, firePointChildName);
        if (!firePoint)
        {
            Debug.LogError($"[BloodWhip] Could not find child '{firePointChildName}' under Player hierarchy.");
            return;
        }

        // Determine aimPoint (mouse world position if available, else forward)
        Vector3 aimPoint = firePoint.position + firePoint.right * maxLength;
        if (Camera.main)
        {
            var mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = firePoint.position.z;
            if ((mouseWorld - firePoint.position).sqrMagnitude > 0.04f)
                aimPoint = mouseWorld;
        }

        var go = Object.Instantiate(whipPrefab);
        var ctrl = go.GetComponent<WhipController>();
        if (!ctrl)
        {
            Debug.LogError("[BloodWhip] Prefab missing WhipController component.");
            Object.Destroy(go);
            return;
        }

        ctrl.Initialize(
            owner: player.transform,
            origin: firePoint,
            damage: damage,
            knockback: knockbackForce,
            maxLength: maxLength,
            thickness: thickness,
            segments: Mathf.Max(2, segmentCount),
            extendTime: Mathf.Max(0.01f, extendTime),
            holdTime: Mathf.Max(0f, holdTime),
            retractTime: Mathf.Max(0.01f, retractTime),
            hitLayers: hitLayers,
            aimPoint: aimPoint
        );

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