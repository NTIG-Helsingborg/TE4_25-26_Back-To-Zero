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
    private Vector3 startingPosition;
    private float distanceFromStartingPosition;
    private Transform returnPoint; // Add this

    void Start()
    {
        startingPosition = transform.position;
        destinationSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();

        // Create return point - DON'T parent it to the enemy!
        GameObject returnObj = new GameObject("ReturnPoint_" + gameObject.name);
        returnObj.transform.position = startingPosition;
        // REMOVED: returnObj.transform.parent = transform; // This was the problem!
        returnPoint = returnObj.transform;

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

        distanceFromStartingPosition = Vector3.Distance(transform.position, startingPosition);

        // Lose aggro if too far from starting position
        if (isAggro && distanceFromStartingPosition > (aggroRange * 5f))
        {
            isAggro = false;
            destinationSetter.target = returnPoint;
            // Keep pathfinding ENABLED so enemy can return
            aiPath.enabled = true;
            destinationSetter.enabled = true;
        }

        // Stop pathfinding when returned to starting position
        if (!isAggro && distanceFromStartingPosition < 0.5f)
        {
            aiPath.enabled = false;
            destinationSetter.enabled = false;
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
