using System.Collections;
using UnityEngine;

public enum SpawnMode
{
    AroundPlayer,       // Spawn around player position (original behavior)
    FromPosition        // Spawn from specific position(s) when player enters trigger
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] SwarmerPrefab;

    [Header("Spawn Mode")]
    [Tooltip("AroundPlayer: Spawns enemies around player position\nFromPosition: Spawns from specific positions when player enters trigger")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.AroundPlayer;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private int maxSpawnAttempts = 20;

    [Header("Position-Based Spawning (Only used when Spawn Mode = FromPosition)")]
    [Tooltip("Specific spawn positions. If empty, uses this GameObject's position")]
    [SerializeField] private Transform[] spawnPositions;
    
    [Tooltip("If true, requires player to enter trigger area before spawning starts")]
    [SerializeField] private bool requireTriggerEnter = false;
    
    [Tooltip("If true, only spawns once when player enters (doesn't spawn for every wave)")]
    [SerializeField] private bool spawnOnceOnEnter = false;
    
    [Tooltip("Random offset radius around spawn positions (0 = exact position)")]
    [SerializeField] private float positionRandomRadius = 0f;

    private PlayerReferenceAssigner playerAssigner;
    private WaveManager waveManager;
    private Coroutine waveSpawnCoroutine;
    private int lastWaveNumber = 0;
    private bool playerHasEntered = false;
    private bool hasSpawnedOnce = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get or create PlayerReferenceAssigner
        playerAssigner = GetComponent<PlayerReferenceAssigner>();
        if (playerAssigner == null)
        {
            playerAssigner = gameObject.AddComponent<PlayerReferenceAssigner>();
        }

        // Get WaveManager
        waveManager = WaveManager.Instance;
        if (waveManager == null)
        {
            Debug.LogWarning("WaveManager not found! Creating one...");
            GameObject waveManagerObj = new GameObject("WaveManager");
            waveManager = waveManagerObj.AddComponent<WaveManager>();
        }

        // Setup trigger if using position-based spawning with trigger requirement
        if (spawnMode == SpawnMode.FromPosition && requireTriggerEnter)
        {
            SetupTrigger();
        }

        // Start wave spawning
        waveSpawnCoroutine = StartCoroutine(WaveSpawningLoop());
    }

    /// <summary>
    /// Sets up a trigger collider if one doesn't exist
    /// </summary>
    private void SetupTrigger()
    {
        Collider2D existingCollider = GetComponent<Collider2D>();
        if (existingCollider == null)
        {
            BoxCollider2D trigger = gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(10f, 10f); // Default size, user can adjust
            Debug.Log($"[EnemySpawner] Added trigger collider to {gameObject.name}. Adjust size in Inspector if needed.");
        }
        else if (!existingCollider.isTrigger)
        {
            existingCollider.isTrigger = true;
            Debug.Log($"[EnemySpawner] Made existing collider a trigger on {gameObject.name}.");
        }
    }

    /// <summary>
    /// Called when player enters the trigger area
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (requireTriggerEnter && other.CompareTag("Player"))
        {
            playerHasEntered = true;
            Debug.Log($"[EnemySpawner] Player entered trigger area on {gameObject.name}. Spawning enabled.");
        }
    }

    private IEnumerator WaveSpawningLoop()
    {
        while (true)
        {
            // Wait for wave to be active (not waiting for timer)
            while (waveManager == null || !waveManager.IsWaveActive() || waveManager.IsWaitingForNextWave())
            {
                yield return new WaitForSeconds(0.1f);
            }

            // If using trigger-based spawning, wait for player to enter
            if (spawnMode == SpawnMode.FromPosition && requireTriggerEnter && !playerHasEntered)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // If spawn once on enter and already spawned, skip
            if (spawnMode == SpawnMode.FromPosition && spawnOnceOnEnter && hasSpawnedOnce)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // Check if this is a new wave
            int currentWave = waveManager.GetCurrentWave();
            if (currentWave == lastWaveNumber)
            {
                // Same wave, wait a bit and check again
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // New wave detected!
            lastWaveNumber = currentWave;
            Debug.Log($"Spawning enemies for Wave {currentWave}");

            // Spawn enemies gradually for this wave
            int enemiesToSpawn = waveManager.GetEnemiesPerWave();
            Debug.Log($"Attempting to spawn {enemiesToSpawn} enemies for Wave {currentWave}");
            
            // Get adjusted spawn interval based on wave
            float adjustedInterval = waveManager.GetSpawnIntervalMultiplier(spawnInterval);

            int enemiesSpawned = 0;
            // Spawn enemies one by one with intervals
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                // Check if wave is still active and matches (might have been reset)
                if (waveManager == null || !waveManager.IsWaveActive() || 
                    waveManager.GetCurrentWave() != currentWave)
                {
                    Debug.LogWarning($"Wave changed or became inactive during spawn. Stopping spawn at {i}/{enemiesToSpawn}");
                    break;
                }

                // Use swarmer prefabs
                if (SwarmerPrefab == null || SwarmerPrefab.Length == 0)
                {
                    Debug.LogWarning("SwarmerPrefab array is null or empty!");
                    continue;
                }

                GameObject enemyToSpawn = SwarmerPrefab[Random.Range(0, SwarmerPrefab.Length)];
                
                if (enemyToSpawn == null)
                {
                    Debug.LogWarning("Enemy prefab is null!");
                    continue;
                }
                
                // Get spawn position based on mode
                Vector3 spawnPosition = GetSpawnPosition();
                
                GameObject newEnemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
                
                if (newEnemy == null)
                {
                    Debug.LogError("Failed to instantiate enemy!");
                    continue;
                }
                
                // Mark this enemy as wave-spawned so only these count toward wave completion.
                if (newEnemy.GetComponent<WaveSpawnedMarker>() == null)
                {
                    newEnemy.AddComponent<WaveSpawnedMarker>();
                }

                // Automatically assign player reference to spawned enemy
                if (playerAssigner != null)
                {
                    playerAssigner.AssignPlayerToEnemy(newEnemy);
                }

                // Notify wave manager
                if (waveManager != null)
                {
                    waveManager.OnEnemySpawned();
                    enemiesSpawned++;
                }

                // Wait before spawning next enemy (except for the last one)
                if (i < enemiesToSpawn - 1)
                {
                    yield return new WaitForSeconds(adjustedInterval);
                }
            }

            Debug.Log($"Finished spawning {enemiesSpawned}/{enemiesToSpawn} enemies for Wave {currentWave}");

            // Mark as spawned if using spawn once mode
            if (spawnMode == SpawnMode.FromPosition && spawnOnceOnEnter)
            {
                hasSpawnedOnce = true;
            }

            // Wait for wave to complete (all enemies killed)
            // Keep checking until wave is no longer active (completed) or timer started
            while (waveManager != null && 
                   waveManager.IsWaveActive() && 
                   waveManager.GetCurrentWave() == currentWave &&
                   !waveManager.IsWaitingForNextWave())
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Wave completed, timer will start
            // Wait for timer to complete and new wave to start
            while (waveManager != null && 
                   (waveManager.IsWaitingForNextWave() || !waveManager.IsWaveActive()))
            {
                yield return new WaitForSeconds(0.1f);
            }

            // New wave started, will loop back and spawn enemies
        }
    }

    /// <summary>
    /// Gets the spawn position based on the current spawn mode
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        if (spawnMode == SpawnMode.FromPosition)
        {
            return GetPositionBasedSpawn();
        }
        else
        {
            // Original behavior: spawn around player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 playerPosition = player != null ? player.transform.position : Vector3.zero;
            return GetValidSpawnPosition(playerPosition);
        }
    }

    /// <summary>
    /// Gets a spawn position from the configured spawn positions
    /// </summary>
    private Vector3 GetPositionBasedSpawn()
    {
        Vector3 basePosition;

        // If spawn positions array is empty or null, use this GameObject's position
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            basePosition = transform.position;
        }
        else
        {
            // Pick a random spawn position from the array
            Transform selectedPosition = spawnPositions[Random.Range(0, spawnPositions.Length)];
            basePosition = selectedPosition != null ? selectedPosition.position : transform.position;
        }

        // Add random offset if configured
        if (positionRandomRadius > 0f)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, positionRandomRadius);
            basePosition += new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0f
            );
        }

        // Validate position (check for walls) if wall layer is set
        if (wallLayer.value != 0)
        {
            if (IsValidSpawnPosition(basePosition))
            {
                return basePosition;
            }
            else
            {
                // Try to find a valid position nearby
                for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = Random.Range(0f, positionRandomRadius + 1f);
                    Vector3 testPos = basePosition + new Vector3(
                        Mathf.Cos(angle) * distance,
                        Mathf.Sin(angle) * distance,
                        0f
                    );
                    if (IsValidSpawnPosition(testPos))
                    {
                        return testPos;
                    }
                }
            }
        }

        return basePosition;
    }

    /// <summary>
    /// Finds a valid spawn position that's not in walls and away from player
    /// </summary>
    private Vector3 GetValidSpawnPosition(Vector3 playerPosition)
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Random angle and distance from player
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minDistanceFromPlayer, spawnRadius);
            
            Vector3 spawnPos = playerPosition + new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0f
            );

            // Check if position is valid (not in wall)
            if (IsValidSpawnPosition(spawnPos))
            {
                return spawnPos;
            }
        }

        // If we couldn't find a valid position after max attempts, spawn at a safe distance
        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return playerPosition + new Vector3(
            Mathf.Cos(fallbackAngle) * minDistanceFromPlayer,
            Mathf.Sin(fallbackAngle) * minDistanceFromPlayer,
            0f
        );
    }

    /// <summary>
    /// Checks if a spawn position is valid (not in a wall)
    /// </summary>
    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if there's a wall at this position using overlap circle
        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f, wallLayer);
        return hit == null;
    }
}
