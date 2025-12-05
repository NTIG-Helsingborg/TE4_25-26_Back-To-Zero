using UnityEngine;

/// <summary>
/// TEMPORARY TEST SCRIPT - Attach to enemies to bypass line of sight check.
/// This helps us confirm if line of sight is the only problem.
/// DO NOT USE IN FINAL GAME - this makes enemies see through walls!
/// </summary>
public class BypassLineOfSightTest : MonoBehaviour
{
    private EnemyChase enemyChase;
    
    void Start()
    {
        enemyChase = GetComponent<EnemyChase>();
        if (enemyChase != null)
        {
            Debug.Log($"[BypassLineOfSightTest] {gameObject.name}: LINE OF SIGHT CHECK DISABLED FOR TESTING");
            Debug.Log("Enemy will now chase through walls - this is for testing only!");
            
            // We can't directly modify the obstacleMask, but we can use reflection
            var obstacleMaskField = typeof(EnemyChase).GetField("obstacleMask", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (obstacleMaskField != null)
            {
                // Set obstacle mask to "Nothing" layer (no obstacles)
                obstacleMaskField.SetValue(enemyChase, LayerMask.GetMask("Nothing"));
                Debug.Log("Obstacle mask set to 'Nothing' - enemy will ignore all obstacles");
            }
        }
    }
}

