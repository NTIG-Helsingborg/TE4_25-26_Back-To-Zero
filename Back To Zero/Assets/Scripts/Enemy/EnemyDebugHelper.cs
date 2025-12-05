using UnityEngine;
using Pathfinding;

/// <summary>
/// Attach this to individual enemies to see why they aren't chasing.
/// Shows real-time debug info in the scene view.
/// </summary>
public class EnemyDebugHelper : MonoBehaviour
{
    private EnemyChase enemyChase;
    private AIPath aiPath;
    private AIDestinationSetter destinationSetter;
    private Transform player;
    
    void Start()
    {
        enemyChase = GetComponent<EnemyChase>();
        aiPath = GetComponent<AIPath>();
        destinationSetter = GetComponent<AIDestinationSetter>();
        
        // Try to find player
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        Debug.Log($"[{gameObject.name}] Setup - EnemyChase:{enemyChase != null}, AIPath:{aiPath != null}, Dest:{destinationSetter != null}, Player:{player != null}");
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (player == null) return;
        
        // Draw line to player
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, player.position);
        
        // Draw aggro range (assuming 8f default)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 8f);
        
        // Show distance
        float distance = Vector3.Distance(transform.position, player.position);
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
            $"Dist: {distance:F1}\n" +
            $"AIPath: {(aiPath != null ? (aiPath.enabled ? "ON" : "OFF") : "NULL")}\n" +
            $"CanMove: {(aiPath != null ? aiPath.canMove : false)}\n" +
            $"DestSet: {(destinationSetter != null ? (destinationSetter.enabled ? "ON" : "OFF") : "NULL")}");
#endif
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) // Press E to debug this enemy
        {
            Debug.Log($"=== DEBUG: {gameObject.name} ===");
            Debug.Log($"Position: {transform.position}");
            
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                Debug.Log($"Distance to player: {distance}");
            }
            else
            {
                Debug.LogError("Player is NULL!");
            }
            
            if (aiPath != null)
            {
                Debug.Log($"AIPath.enabled: {aiPath.enabled}");
                Debug.Log($"AIPath.canMove: {aiPath.canMove}");
                Debug.Log($"AIPath.isStopped: {aiPath.isStopped}");
                Debug.Log($"AIPath.hasPath: {aiPath.hasPath}");
                Debug.Log($"AIPath.destination: {aiPath.destination}");
            }
            else
            {
                Debug.LogError("AIPath is NULL!");
            }
            
            if (destinationSetter != null)
            {
                Debug.Log($"AIDestinationSetter.enabled: {destinationSetter.enabled}");
                Debug.Log($"AIDestinationSetter.target: {(destinationSetter.target != null ? destinationSetter.target.name : "NULL")}");
            }
            else
            {
                Debug.LogError("AIDestinationSetter is NULL!");
            }
            
            if (enemyChase != null)
            {
                Debug.Log($"EnemyChase script exists");
            }
            else
            {
                Debug.LogError("EnemyChase is NULL!");
            }
            
            Debug.Log("===============");
        }
    }
}

