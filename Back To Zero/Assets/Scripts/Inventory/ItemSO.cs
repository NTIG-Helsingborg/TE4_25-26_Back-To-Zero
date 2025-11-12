using UnityEngine;

[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite;
    public string itemDescription;
    public StatToChange statToChange = StatToChange.None;
    public int amountToChangeStat;
    public int dropChance;  
    public int isArtifact; // 0 = no, 1 = yes
    

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
        Intelligence,
        Coin
    };
}
