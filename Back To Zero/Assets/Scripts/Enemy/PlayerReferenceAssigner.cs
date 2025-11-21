using UnityEngine;

/// <summary>
/// Automatically assigns the player Transform to all enemy scripts in the scene.
/// Can be attached to any GameObject or called manually.
/// </summary>
public class PlayerReferenceAssigner : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, automatically assigns player references on Start")]
    [SerializeField] private bool assignOnStart = true;
    
    // Note: assignToSpawnedEnemies is reserved for future runtime spawning functionality
    // [Tooltip("If true, also assigns to enemies spawned at runtime")]
    // [SerializeField] private bool assignToSpawnedEnemies = false;

    private Transform playerTransform;

    void Start()
    {
        if (assignOnStart)
        {
            AssignPlayerToAllEnemies();
        }
    }

    /// <summary>
    /// Finds and assigns the player Transform to all enemies in the scene
    /// </summary>
    public void AssignPlayerToAllEnemies()
    {
        // Find player
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("[PlayerReferenceAssigner] Player not found! Make sure player has 'Player' tag.");
                return;
            }
        }

        int assignedCount = 0;

        // Assign to EnemyChase components
        EnemyChase[] enemyChases = FindObjectsByType<EnemyChase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EnemyChase enemyChase in enemyChases)
        {
            // Use reflection to set the private SerializeField
            var playerField = typeof(EnemyChase).GetField("player", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerField != null)
            {
                var currentValue = playerField.GetValue(enemyChase) as Transform;
                if (currentValue == null)
                {
                    playerField.SetValue(enemyChase, playerTransform);
                    assignedCount++;
                }
            }
        }

        // Assign to RangedSmallUndead components
        RangedSmallUndead[] rangedEnemies = FindObjectsByType<RangedSmallUndead>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (RangedSmallUndead rangedEnemy in rangedEnemies)
        {
            var playerField = typeof(RangedSmallUndead).GetField("player", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerField != null)
            {
                var currentValue = playerField.GetValue(rangedEnemy) as Transform;
                if (currentValue == null)
                {
                    playerField.SetValue(rangedEnemy, playerTransform);
                    assignedCount++;
                }
            }
        }

        // Assign to BossAttack components
        BossAttack[] bossAttacks = FindObjectsByType<BossAttack>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BossAttack bossAttack in bossAttacks)
        {
            if (bossAttack.playerTransform == null)
            {
                bossAttack.playerTransform = playerTransform;
                assignedCount++;
            }
        }

        Debug.Log($"[PlayerReferenceAssigner] Assigned player reference to {assignedCount} enemy components.");
    }

    /// <summary>
    /// Assigns player reference to a specific enemy GameObject
    /// Useful for runtime spawning
    /// </summary>
    public void AssignPlayerToEnemy(GameObject enemy)
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("[PlayerReferenceAssigner] Player not found!");
                return;
            }
        }

        // Assign to EnemyChase
        EnemyChase enemyChase = enemy.GetComponent<EnemyChase>();
        if (enemyChase != null)
        {
            var playerField = typeof(EnemyChase).GetField("player", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerField != null)
            {
                playerField.SetValue(enemyChase, playerTransform);
            }
        }

        // Assign to RangedSmallUndead
        RangedSmallUndead rangedEnemy = enemy.GetComponent<RangedSmallUndead>();
        if (rangedEnemy != null)
        {
            var playerField = typeof(RangedSmallUndead).GetField("player", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerField != null)
            {
                playerField.SetValue(rangedEnemy, playerTransform);
            }
        }

        // Assign to BossAttack
        BossAttack bossAttack = enemy.GetComponent<BossAttack>();
        if (bossAttack != null)
        {
            bossAttack.playerTransform = playerTransform;
        }
    }
}

