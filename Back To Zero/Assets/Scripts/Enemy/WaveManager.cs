using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Simple wave system - tracks current wave and resets on death
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Settings")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int baseEnemiesPerWave = 5;
    [SerializeField] private int enemiesPerWaveIncrease = 2;
    [SerializeField] private float timeBetweenWaves = 5f;
    
    [Header("Wave Scaling")]
    [SerializeField] private float spawnSpeedIncrease = 0.1f;
    [SerializeField] private float minSpawnInterval = 1f;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI waveText;

    private int enemiesPerWave;
    private int enemiesSpawnedThisWave = 0;
    private int enemiesKilledThisWave = 0;
    private bool isWaveActive = false;
    private bool isWaitingForNextWave = false;
    private Coroutine waveTimerCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WaveManager instances detected. Using the most recent one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        StartWave(currentWave);
    }

    /// <summary>
    /// Starts a new wave
    /// </summary>
    public void StartWave(int waveNumber)
    {
        currentWave = waveNumber;
        enemiesSpawnedThisWave = 0;
        enemiesKilledThisWave = 0;
        isWaveActive = true;

        // Calculate enemies for this wave (scales with wave number)
        enemiesPerWave = baseEnemiesPerWave + (waveNumber - 1) * enemiesPerWaveIncrease;

        UpdateWaveUI();
        Debug.Log($"Wave {currentWave} started! Enemies to spawn: {enemiesPerWave}");
    }

    /// <summary>
    /// Resets waves back to wave 1
    /// </summary>
    public void ResetWaves()
    {
        // Stop any running timer
        if (waveTimerCoroutine != null)
        {
            StopCoroutine(waveTimerCoroutine);
            waveTimerCoroutine = null;
        }

        currentWave = 1;
        enemiesSpawnedThisWave = 0;
        enemiesKilledThisWave = 0;
        isWaveActive = false;
        isWaitingForNextWave = false;

        // Clear all enemies
        ClearAllEnemies();

        // Restart wave 1 (this will update UI)
        StartWave(1);

        Debug.Log("Waves reset to Wave 1");
    }

    /// <summary>
    /// Called when an enemy is spawned
    /// </summary>
    public void OnEnemySpawned()
    {
        enemiesSpawnedThisWave++;
    }

    /// <summary>
    /// Checks if we can spawn more enemies for this wave
    /// </summary>
    public bool CanSpawnMoreEnemies()
    {
        return isWaveActive && enemiesSpawnedThisWave < enemiesPerWave;
    }

    /// <summary>
    /// Called when an enemy is killed
    /// </summary>
    public void OnEnemyKilled()
    {
        if (!isWaveActive)
        {
            Debug.LogWarning("OnEnemyKilled called but wave is not active!");
            return;
        }

        enemiesKilledThisWave++;
        Debug.Log($"Enemy killed! {enemiesKilledThisWave}/{enemiesPerWave} killed for Wave {currentWave}");

        // Wave completes when we've killed all enemies that were spawned
        if (enemiesKilledThisWave >= enemiesPerWave)
        {
            Debug.Log($"Wave {currentWave} complete! All {enemiesKilledThisWave} enemies killed.");
            CompleteWave();
        }
    }

    /// <summary>
    /// Completes the current wave and starts a timer for the next wave
    /// </summary>
    private void CompleteWave()
    {
        isWaveActive = false;
        Debug.Log($"Wave {currentWave} completed! Starting timer for next wave...");

        // Start timer for next wave
        if (waveTimerCoroutine != null)
        {
            StopCoroutine(waveTimerCoroutine);
        }
        waveTimerCoroutine = StartCoroutine(WaveTimer());
    }

    /// <summary>
    /// Timer between waves - when it completes, starts the next wave
    /// </summary>
    private IEnumerator WaveTimer()
    {
        isWaitingForNextWave = true;
        Debug.Log($"Timer started: {timeBetweenWaves} seconds until Wave {currentWave + 1}");
        
        float timer = timeBetweenWaves;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        // Timer complete, start next wave
        isWaitingForNextWave = false;
        currentWave++;
        Debug.Log($"Timer complete! Starting Wave {currentWave}");
        StartWave(currentWave);
    }

    /// <summary>
    /// Clears all enemies from the scene
    /// </summary>
    private void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
    }

    /// <summary>
    /// Gets the spawn interval multiplier for the current wave (lower = faster spawning)
    /// </summary>
    public float GetSpawnIntervalMultiplier(float baseInterval)
    {
        float reduction = (currentWave - 1) * spawnSpeedIncrease;
        float interval = baseInterval - reduction;
        return Mathf.Max(interval, minSpawnInterval);
    }

    /// <summary>
    /// Updates the wave UI display
    /// </summary>
    private void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = $"Wave {currentWave}";
        }
    }

    // Public getters
    public int GetCurrentWave() => currentWave;
    public int GetEnemiesKilledThisWave() => enemiesKilledThisWave;
    public int GetEnemiesPerWave() => enemiesPerWave;
    public bool IsWaveActive() => isWaveActive;
    public bool IsWaitingForNextWave() => isWaitingForNextWave;
    public float GetTimeBetweenWaves() => timeBetweenWaves;
}

