using UnityEngine;

[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public StatToChange statToChange = new StatToChange();
    public int amountToChangeStat;

    public void UseItem()
    {
        if(statToChange == StatToChange.Health){
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                Health health = player.GetComponent<Health>();
                if (health != null)
                {
                    health.Heal(amountToChangeStat);
                }
                else
                {
                    Debug.LogError("ItemSO: Player doesn't have a Health component!");
                }
            }
            else
            {
                Debug.LogError("ItemSO: Couldn't find GameObject named 'Player'!");
            }
        }
    }

    public enum StatToChange
    {
        None,
        Health,
        Power,
        Agility,
        Intelligence
    };
}
