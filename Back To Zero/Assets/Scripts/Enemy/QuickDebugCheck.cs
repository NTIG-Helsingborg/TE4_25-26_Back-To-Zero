using UnityEngine;

/// <summary>
/// Quick debug script to check common issues with enemy pathfinding.
/// Attach to any GameObject and it will check on Start.
/// </summary>
public class QuickDebugCheck : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== PATHFINDING DEBUG CHECK ===");
        
        // Check 1: Is there a player?
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ NO PLAYER FOUND! Make sure your player has the tag 'Player'");
        }
        else
        {
            Debug.Log($"✅ Player found: {player.name}");
        }
        
        // Check 2: Is there an AstarPath?
        if (AstarPath.active == null)
        {
            Debug.LogError("❌ NO A* PATHFINDING SYSTEM! Add an 'A*' GameObject with AstarPath component");
        }
        else
        {
            Debug.Log($"✅ A* Pathfinding active: {AstarPath.active.name}");
            
            if (AstarPath.active.graphs == null || AstarPath.active.graphs.Length == 0)
            {
                Debug.LogError("❌ NO GRAPHS CONFIGURED! Open A* inspector and add a graph");
            }
            else
            {
                Debug.Log($"✅ Found {AstarPath.active.graphs.Length} graph(s)");
            }
        }
        
        // Check 3: Find all enemies with EnemyChase
        EnemyChase[] enemies = FindObjectsOfType<EnemyChase>();
        Debug.Log($"Found {enemies.Length} enemies with EnemyChase script");
        
        foreach (var enemy in enemies)
        {
            var aiPath = enemy.GetComponent<Pathfinding.AIPath>();
            var aiDest = enemy.GetComponent<Pathfinding.AIDestinationSetter>();
            
            if (aiPath == null)
            {
                Debug.LogError($"❌ {enemy.name} is MISSING AIPath component!");
            }
            if (aiDest == null)
            {
                Debug.LogError($"❌ {enemy.name} is MISSING AIDestinationSetter component!");
            }
            
            if (aiPath != null && aiDest != null)
            {
                Debug.Log($"   {enemy.name}: AIPath.enabled={aiPath.enabled}, AIPath.canMove={aiPath.canMove}");
            }
        }
        
        Debug.Log("=== END DEBUG CHECK ===");
    }
}

