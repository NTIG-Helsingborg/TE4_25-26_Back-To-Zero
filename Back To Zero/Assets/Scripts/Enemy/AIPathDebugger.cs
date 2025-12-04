using UnityEngine;
using Pathfinding;

/// <summary>
/// Debug script to help identify why AIPath enemies aren't moving.
/// Attach this to any enemy with AIPath to see detailed debug info.
/// </summary>
public class AIPathDebugger : MonoBehaviour
{
    private AIPath aiPath;
    private AIDestinationSetter destinationSetter;
    private Seeker seeker;

    void Start()
    {
        aiPath = GetComponent<AIPath>();
        destinationSetter = GetComponent<AIDestinationSetter>();
        seeker = GetComponent<Seeker>();
        
        if (aiPath == null)
        {
            Debug.LogError($"[AIPathDebugger] {gameObject.name}: NO AIPath component found!");
        }
        if (destinationSetter == null)
        {
            Debug.LogWarning($"[AIPathDebugger] {gameObject.name}: NO AIDestinationSetter component found!");
        }
        if (seeker == null)
        {
            Debug.LogError($"[AIPathDebugger] {gameObject.name}: NO Seeker component found!");
        }
    }

    void Update()
    {
        if (aiPath == null) return;

        // Only log when there's a potential problem
        if (destinationSetter != null && destinationSetter.target != null && destinationSetter.enabled)
        {
            bool hasIssue = !aiPath.enabled || !aiPath.canMove || aiPath.isStopped;
            
            if (hasIssue || Input.GetKeyDown(KeyCode.D))
            {
                Debug.Log($"=== AIPath Debug for {gameObject.name} ===");
                Debug.Log($"AIPath.enabled: {aiPath.enabled}");
                Debug.Log($"AIPath.canMove: {aiPath.canMove}");
                Debug.Log($"AIPath.isStopped: {aiPath.isStopped}");
                Debug.Log($"AIPath.maxSpeed: {aiPath.maxSpeed}");
                Debug.Log($"AIPath.hasPath: {aiPath.hasPath}");
                Debug.Log($"AIPath.pathPending: {aiPath.pathPending}");
                Debug.Log($"AIPath.reachedDestination: {aiPath.reachedDestination}");
                Debug.Log($"AIPath.reachedEndOfPath: {aiPath.reachedEndOfPath}");
                Debug.Log($"AIPath.destination: {aiPath.destination}");
                Debug.Log($"AIDestinationSetter.enabled: {destinationSetter.enabled}");
                Debug.Log($"AIDestinationSetter.target: {destinationSetter.target?.name ?? "NULL"}");
                
                if (seeker != null)
                {
                    Debug.Log($"Seeker.enabled: {seeker.enabled}");
                }
                
                if (AstarPath.active == null)
                {
                    Debug.LogError("AstarPath.active is NULL! No pathfinding system found in scene!");
                }
                else
                {
                    Debug.Log($"AstarPath.active: {AstarPath.active.name}");
                    Debug.Log($"AstarPath graphs count: {AstarPath.active.graphs?.Length ?? 0}");
                }
                Debug.Log("================");
            }
        }
    }
}

