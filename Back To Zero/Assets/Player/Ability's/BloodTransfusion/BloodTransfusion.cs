using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Blood Transfusion")]
public class BloodTransfusion : Ability
{
    [Header("Transfusion settings")]
    public float damage;
    public float radius;
    public float duration;
    public float knockbackForce;
    public bool IsAbility = true;

    [Header("Targeting")]
    public LayerMask damageLayers = ~0;     // set to Enemy layer(s) in Inspector 

    [Header("VFX")]
    public GameObject explosionVfxPrefab;   // assign a circle sprite prefab (optional)

    [Header("Auto-Harvest on Kill")]
    [Range(0f, 1f)] public float harvestHealPercentage = 0.5f; // heal % of victim max HP

     public override bool CanActivate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return false;
        var handler = player.GetComponent<PlayerHandler>();
        return handler != null && handler.IsUltimateReady;
    }

    public override void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            Debug.LogError("[BloodTransfusion] No Player tagged 'Player' in scene.");
            return;
        }

        var playerHealth = player.GetComponent<Health>();
        if (playerHealth == null)
        {
            Debug.LogError("[BloodTransfusion] Player Health not found.");
            return;
        }
        var handler = player.GetComponent<PlayerHandler>();
        if (handler == null)
        {
            Debug.LogError("[BloodTransfusion] PlayerHandler not found on Player.");
            return;
        }
        if (!handler.IsUltimateReady)
        {
            Debug.LogWarning("[BloodTransfusion] Ultimate not ready.");
            return;
        }


        // Set HP to 1
        int cur = playerHealth.GetCurrentHealth();
        if (cur > 1) playerHealth.SpendHealth(cur - 1);
        else if (cur < 1) playerHealth.Heal(1 - cur);

        // Consume meter
        handler.ConsumeUltimate();

        // Spawn explosion
        var go = new GameObject("BloodTransfusionExplosion");
        go.transform.position = player.transform.position;
        var pulse = go.AddComponent<ExpandingExplosion>();
        pulse.Initialize(
            Mathf.RoundToInt(damage),
            radius,
            duration,
            knockbackForce,
            player,
            damageLayers,
            false,                 // do not include owner
            explosionVfxPrefab,
            true,                    // auto-harvest on kill
            harvestHealPercentage,
            playerHealth,
            handler            // heal goes to player
        );
    }
}

