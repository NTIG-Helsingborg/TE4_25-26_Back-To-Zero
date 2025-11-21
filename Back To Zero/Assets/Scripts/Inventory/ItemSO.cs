using UnityEngine;

[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite;
    public string itemDescription;
    public StatToChange statToChange = StatToChange.None;
    public int amountToChangeStat;
    public Rarity itemRarity = Rarity.none;
    public int dropChance;  
    public int isArtifact; // 0 = no, 1 = yes
    public int isAbility; // 0 = no, 1 = yes
    

    public bool UseItem()
    {
        if (statToChange == StatToChange.Health)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return false;
            }

            Health health = player.GetComponent<Health>();
            if (health == null || health.IsFullHealth())
            {
                return false;
            }

            Healing healing = player.GetComponent<Healing>();
            if (healing != null)
            {
                return healing.TryStartHeal(this);
            }

            // Fallback if no Healing component is available.
            health.Heal(amountToChangeStat);
            return true;
        }

        return false;
    }
    public bool ArtifactItem()
    {
        if (statToChange == StatToChange.HealthIncrease)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return false;
            }

            Health health = player.GetComponent<Health>();
            if (health == null)
            {
                return false;
            }

            health.IncreaseMaxHealth(amountToChangeStat);
            return true;
        }
        
        if (statToChange == StatToChange.Power)
        {
            // Increase power bonus for blood abilities
            PowerBonus.AddPowerBonus(amountToChangeStat);
            Debug.Log($"ArtifactItem: Increased Power by {amountToChangeStat}%. Total Power: {PowerBonus.GetTotalPowerPercentage()}%");
            return true;
        }
        
        return false;
    }

    public enum StatToChange
    {
        None,
        Health,
        Power,
        Agility,
        Intelligence,
        Coin,
        HealthIncrease
    };
    public enum Rarity{
        none,
        common,
        rare,
        epic,
        legendary,
        Cursed
    };
}
