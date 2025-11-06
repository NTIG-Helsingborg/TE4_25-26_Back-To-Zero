using NUnit.Framework;
using Pathfinding;
using UnityEngine;


public class EnemyChase : MonoBehaviour
{

    public Transform player;
    public float aggroRange = 8f;
    public LayerMask obstacleMask; // Assign walls/obstacles in inspector

    private AIDestinationSetter destinationSetter;
    private AIPath aiPath;
    private bool isAggro = false;

    void Start()
    {
        destinationSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();

        // Disable pathfinding at start
        aiPath.enabled = false;
        destinationSetter.enabled = false;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // Acquire aggro
        if (!isAggro && distance <= aggroRange && HasLineOfSight())
        {
            isAggro = true;
            destinationSetter.target = player;
            aiPath.enabled = true;
            destinationSetter.enabled = true;
        }
    }

    bool HasLineOfSight()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        if (Physics2D.Raycast(transform.position, direction, distance, obstacleMask))
        {
            return false; // Hit a wall — no vision
        }
        return true; // Clear line of sight
    }
}
