using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Blood Shield")]
public class BloodShield : Ability
{
    [Header("Shield Settings")]
    [SerializeField] private int healthCost;
    [SerializeField] private int shieldAmount;
    [SerializeField] private float shieldDuration;

    public bool IsAbility = true;

    private Health playerHealth;

    public override bool CanActivate()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }

        if (playerHealth == null)
            return false;

        // Can only cast if player has enough health (more than the cost)
        return playerHealth.GetCurrentHealth() > healthCost;
    }

    public override void Activate()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }

        if (playerHealth != null)
        {
            // Spend health
            playerHealth.SpendHealth(healthCost);

            // Grant shield
            playerHealth.AddShield(shieldAmount, shieldDuration);
        }
    }
}