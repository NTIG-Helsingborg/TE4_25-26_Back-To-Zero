using UnityEngine;
[CreateAssetMenu(menuName = "Abilities/Blood Explosion")]

public class BloodExplosion : Ability
{
    [Header("Blood Explosion Settings")]
    public float damage;
    public float radius;
    public float range;
    public float speed;
    public float HpCost;

    [Header("References")]
    public GameObject BloodExplosionPrefab;
    [SerializeField] private string firePointChildName = "SpellTransform";

     public override void Activate()
    {
        
    }
    
}