using NUnit.Framework;
using UnityEngine;

public class EnemyChase : MonoBehaviour
{

    public GameObject player; // Reference to the player object
    public float speed;
    public float attackRange;

    private float distanceToPlayer;
    private bool isChasing = false;
    private bool hasLineOfSight = false;
    private Vector3 startingPosition;
    private Vector3 roamPosition;

    public static Vector3 GetRandomDirection()
    {
      return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    private Vector3 getRoamingPosition(){
        return startingPosition + GetRandomDirection() * Random.Range(2f, 5f);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startingPosition = transform.position;
        roamPosition = getRoamingPosition();
    }

    // Update is called once per frame
    void Update()
    {
        distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        Vector2 direction = player.transform.position - transform.position;

        if (isChasing == false)
        {
            if (Vector2.Distance(transform.position, roamPosition) < 0.2f)
            {
                roamPosition = getRoamingPosition();
            }
            transform.position = Vector2.MoveTowards(this.transform.position, roamPosition, speed * Time.deltaTime);
        }


        if (hasLineOfSight)
        {
            if (distanceToPlayer < attackRange)
            {
                isChasing = true;
            } 

            if (isChasing == true)
            {
                transform.position = Vector2.MoveTowards(this.transform.position, player.transform.position, speed * Time.deltaTime);
            }  
        }  
        else
        {
            isChasing = false;
        }
    }

    private void FixedUpdate()
    {
        RaycastHit2D ray = Physics2D.Raycast(transform.position, player.transform.position - transform.position);
        if (ray.collider != null)
        {
            hasLineOfSight = ray.collider.CompareTag("Player");
            if(hasLineOfSight)
            {
                Debug.DrawRay(transform.position, player.transform.position - transform.position, Color.green);
            }
            else
            {
                Debug.DrawRay(transform.position, player.transform.position - transform.position, Color.red);
            }
        }
    }
}
