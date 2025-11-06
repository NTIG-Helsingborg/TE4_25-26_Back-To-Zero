using UnityEngine;
using System.Collections;
[CreateAssetMenu]
public class BloodKnife : Ability
{
    [Header("Blood Knife Settings")]
    public float damage;
    public float range;
    public float speed;
    public float HpCost;



    public GameObject playerObject;
    public GameObject BloodKnifePrefab;
    public Transform firePoint;

    [SerializeField] private string firePointChildName = "SpellTransform";


    public void Active()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (firePoint == null)
        {
            foreach (var t in playerObject.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == firePointChildName)
                {
                    firePoint = t;
                    break;
                }
            }
        }

        if (firePoint == null)
        {
            Debug.LogError($"[BloodKnife] Child '{firePointChildName}' not found on Player.");
            return;
        }
    } 
    // Update is called once per frame
    void Update()
    {
        Instantiate(BloodKnifePrefab, firePoint.position, firePoint.rotation);
    }
}
