using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Blood Bind")]
public class BloodBind : Ability
{
    [Header("Scan")]
    public float bindRadius = 6f;
    public int maxTargets = 3;
    public LayerMask enemyLayers;

    [Header("Bind Effect")]
    public float bindDuration = 2f;

    [Header("Tendril Visual")]
    public GameObject BloodBindPrefab;              // LineRenderer + TendrilBindController
    [SerializeField] private string firePointChildName = "SpellTransform";
    public float extendTime = 0.2f;
    public float holdTime = 0.0f;
    public float retractTime = 0.25f;

    [Header("HP Cost")]
    public float HpCost;

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        var firePoint = FindChildByName(player.transform, firePointChildName) ?? player.transform;
        if (!BloodBindPrefab) return;

        // Optional health cost
        if (HpCost > 0f)
        {
            var h = player.GetComponent<Health>();
            if (h) h.SpendHealth(Mathf.RoundToInt(HpCost));
        }

        // Find enemies
        var hits = Physics2D.OverlapCircleAll(player.transform.position, bindRadius, enemyLayers);
        if (hits.Length == 0) return;

        int spawned = 0;
        foreach (var col in hits)
        {
            if (spawned >= maxTargets) break;
            if (col.transform == player.transform) continue;

            var go = Object.Instantiate(BloodBindPrefab);
            var ctrl = go.GetComponent<BindController>();
            if (!ctrl)
            {
                Object.Destroy(go);
                continue;
            }

            ctrl.Initialize(
                origin: firePoint,
                target: col.transform,
                extendTime: Mathf.Max(0.01f, extendTime),
                holdTime: Mathf.Max(0f, holdTime),
                retractTime: Mathf.Max(0.01f, retractTime),
                bindDuration: bindDuration
            );

            spawned++;
        }
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}