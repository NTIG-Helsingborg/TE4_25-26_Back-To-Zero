using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] private GameObject[] SwarmerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private int maxSpawnAttempts = 20;

    private PlayerReferenceAssigner playerAssigner;
    private WaveManager waveManager;
    private Coroutine waveSpawnCoroutine;
    private int lastWaveNumber = 0;

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

        // Start wave spawning
        waveSpawnCoroutine = StartCoroutine(WaveSpawningLoop());
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
            
            // Get player position for spawn calculations
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 playerPosition = player != null ? player.transform.position : Vector3.zero;

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
                
                // Find a valid spawn position (not in walls)
                Vector3 spawnPosition = GetValidSpawnPosition(playerPosition);
                
                GameObject newEnemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
                
                if (newEnemy == null)
                {
                    Debug.LogError("Failed to instantiate enemy!");
                    continue;
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
