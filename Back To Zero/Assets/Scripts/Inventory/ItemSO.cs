using UnityEngine;

[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public StatToChange statToChange = new StatToChange();
    public int amountToChangeStat;

    public bool UseItem()
    {
        if(statToChange == StatToChange.Health){
            Health health = GameObject.Find("Player").GetComponent<Health>();
            if (health != null && health.IsFullHealth()){
                return false;
            }
            else if (health != null){
                health.Heal(amountToChangeStat);
                return true;
            }
        }
        return false;
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
