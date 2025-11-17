using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages level-up reward generation and application (Hades-style boon system)
/// </summary>
public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    [Header("Reward Pool")]
    [Tooltip("All available rewards that can be offered. Can be manually assigned or auto-loaded from Resources/Buffs folders.")]
    [SerializeField] private RewardSO[] allRewards;

    [Header("Auto-Load Settings")]
    [Tooltip("If true, automatically loads rewards from Resources/Rewards and Player/Buffs folders on Start")]
    [SerializeField] private bool autoLoadRewards = true;
    
    [Tooltip("If true, includes rewards from Resources/Rewards folder")]
    [SerializeField] private bool loadFromResources = true;
    
    [Tooltip("If true, includes rewards from Player/Buffs folder")]
    [SerializeField] private bool loadFromBuffsFolder = true;

    [Header("Rarity Weights")]
    [Tooltip("Chance weights for each rarity (higher = more common)")]
    [SerializeField] private int commonWeight = 60;
    [SerializeField] private int rareWeight = 25;
    [SerializeField] private int legendaryWeight = 10;
    [SerializeField] private int cursedWeight = 5;

    private List<RewardSO> currentRewardOptions = new List<RewardSO>();
    private PlayerStats playerStats;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple RewardManager instances detected. Using the most recent one.");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }

        // Auto-load rewards if enabled
        if (autoLoadRewards)
        {
            LoadAllRewards();
        }
        // Otherwise, if no rewards assigned, try to find all rewards in Resources folder (backward compatibility)
        else if (allRewards == null || allRewards.Length == 0)
        {
            LoadRewardsFromResources();
        }
    }

    /// <summary>
    /// Load all rewards from multiple sources and combine them
    /// </summary>
    public void LoadAllRewards()
    {
        List<RewardSO> combinedRewards = new List<RewardSO>();

        // Add manually assigned rewards first
        if (allRewards != null && allRewards.Length > 0)
        {
            combinedRewards.AddRange(allRewards.Where(r => r != null));
        }

        // Load from Resources/Rewards folder
        if (loadFromResources)
        {
            RewardSO[] resourcesRewards = Resources.LoadAll<RewardSO>("Rewards");
            if (resourcesRewards != null && resourcesRewards.Length > 0)
            {
                combinedRewards.AddRange(resourcesRewards);
                Debug.Log($"RewardManager: Loaded {resourcesRewards.Length} rewards from Resources/Rewards folder.");
            }
        }

        // Load from Player/Buffs folder
        if (loadFromBuffsFolder)
        {
            RewardSO[] buffsRewards = LoadRewardsFromBuffsFolder();
            if (buffsRewards != null && buffsRewards.Length > 0)
            {
                combinedRewards.AddRange(buffsRewards);
                Debug.Log($"RewardManager: Loaded {buffsRewards.Length} rewards from Player/Buffs folder.");
            }
        }

        // Remove duplicates (by reference)
        allRewards = combinedRewards.Distinct().ToArray();
        
        Debug.Log($"RewardManager: Total rewards loaded: {allRewards.Length}");
        
        if (allRewards.Length == 0)
        {
            Debug.LogWarning("RewardManager: No rewards found! Please assign rewards manually or ensure they exist in Resources/Rewards or Player/Buffs folders.");
        }
    }

    /// <summary>
    /// Load all rewards from Resources/Rewards folder
    /// </summary>
    void LoadRewardsFromResources()
    {
        RewardSO[] loadedRewards = Resources.LoadAll<RewardSO>("Rewards");
        if (loadedRewards != null && loadedRewards.Length > 0)
        {
            allRewards = loadedRewards;
            Debug.Log($"RewardManager: Loaded {allRewards.Length} rewards from Resources.");
        }
        else
        {
            Debug.LogWarning("RewardManager: No rewards found in Resources/Rewards folder. Please assign rewards manually or create some reward ScriptableObjects.");
        }
    }

    /// <summary>
    /// Load all rewards from Player/Buffs folder
    /// </summary>
    RewardSO[] LoadRewardsFromBuffsFolder()
    {
        List<RewardSO> buffs = new List<RewardSO>();

#if UNITY_EDITOR
        // In editor, use AssetDatabase to find all RewardSO assets in Player/Buffs folder
        // Try multiple possible paths to be more robust
        string[] searchPaths = new[] { "Assets/Player/Buffs", "Player/Buffs" };
        
        foreach (string searchPath in searchPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:RewardSO", new[] { searchPath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RewardSO reward = AssetDatabase.LoadAssetAtPath<RewardSO>(path);
                if (reward != null && !buffs.Contains(reward))
                {
                    buffs.Add(reward);
                }
            }
            
            // If we found rewards, break (don't search other paths)
            if (buffs.Count > 0)
            {
                break;
            }
        }
#else
        // At runtime, try to load from Resources if buffs are moved there
        // Or use a pre-compiled list if needed
        RewardSO[] runtimeBuffs = Resources.LoadAll<RewardSO>("Buffs");
        if (runtimeBuffs != null && runtimeBuffs.Length > 0)
        {
            buffs.AddRange(runtimeBuffs);
        }
#endif

        return buffs.ToArray();
    }

    /// <summary>
    /// Generate 3 random rewards for level up (Hades-style)
    /// </summary>
    public List<RewardSO> GenerateRewards(int count = 3)
    {
        if (allRewards == null || allRewards.Length == 0)
        {
            Debug.LogError("RewardManager: No rewards available! Cannot generate rewards.");
            return new List<RewardSO>();
        }

        List<RewardSO> selectedRewards = new List<RewardSO>();
        List<RewardSO> availableRewards = new List<RewardSO>(allRewards);

        // Calculate total weight
        int totalWeight = commonWeight + rareWeight + legendaryWeight + cursedWeight;

        for (int i = 0; i < count && availableRewards.Count > 0; i++)
        {
            // Determine rarity based on weights
            RewardRarity selectedRarity = GetRandomRarity(totalWeight);

            // Filter rewards by selected rarity
            List<RewardSO> rarityFiltered = availableRewards
                .Where(r => r.rarity == selectedRarity)
                .ToList();

            // If no rewards of that rarity, fall back to any available reward
            if (rarityFiltered.Count == 0)
            {
                rarityFiltered = availableRewards;
            }

            // Pick random reward from filtered list
            RewardSO selectedReward = rarityFiltered[Random.Range(0, rarityFiltered.Count)];

            selectedRewards.Add(selectedReward);
            availableRewards.Remove(selectedReward); // Prevent duplicates in same selection
        }

        currentRewardOptions = selectedRewards;
        return selectedRewards;
    }

    /// <summary>
    /// Get random rarity based on weights
    /// </summary>
    RewardRarity GetRandomRarity(int totalWeight)
    {
        int random = Random.Range(0, totalWeight);

        if (random < commonWeight)
            return RewardRarity.Common;
        random -= commonWeight;

        if (random < rareWeight)
            return RewardRarity.Rare;
        random -= rareWeight;

        if (random < legendaryWeight)
            return RewardRarity.Legendary;

        return RewardRarity.Cursed;
    }

    /// <summary>
    /// Apply a selected reward to the player
    /// </summary>
    public void ApplyReward(RewardSO reward)
    {
        if (reward == null)
        {
            Debug.LogError("RewardManager: Cannot apply null reward!");
            return;
        }

        if (playerStats == null)
        {
            Debug.LogError("RewardManager: PlayerStats not found! Cannot apply reward.");
            return;
        }

        Debug.Log($"Applying reward: {reward.rewardName} ({RewardRarityHelper.GetRarityName(reward.rarity)})");

        // Apply stat changes
        if (reward.damageMultiplierChange != 0)
        {
            playerStats.AddStatBonus(StatType.DamageMultiplier, reward.damageMultiplierChange);
        }

        if (reward.attackSpeedChange != 0)
        {
            playerStats.AddStatBonus(StatType.AttackSpeed, reward.attackSpeedChange);
        }

        if (reward.maxHealthChange != 0)
        {
            playerStats.AddStatBonus(StatType.MaxHealth, reward.maxHealthChange);
        }

        if (reward.defenseMultiplierChange != 0)
        {
            playerStats.AddStatBonus(StatType.DefenseMultiplier, reward.defenseMultiplierChange);
        }

        if (reward.moveSpeedChange != 0)
        {
            playerStats.AddStatBonus(StatType.MoveSpeed, reward.moveSpeedChange);
        }

        // Note: Other stats like crit chance, lifesteal, cooldown reduction would need
        // to be stored separately or added to PlayerStats if you want them tracked
        // For now, we'll log them
        if (reward.critChanceChange != 0 || reward.lifestealChange != 0 || 
            reward.abilityCooldownReduction != 0 || reward.statusEffectChanceChange != 0)
        {
            Debug.Log($"Reward has special effects that aren't yet implemented in PlayerStats: " +
                     $"Crit: {reward.critChanceChange}, Lifesteal: {reward.lifestealChange}, " +
                     $"Cooldown: {reward.abilityCooldownReduction}, Status: {reward.statusEffectChanceChange}");
        }
    }

    /// <summary>
    /// Get current reward options (for UI display)
    /// </summary>
    public List<RewardSO> GetCurrentRewards()
    {
        return currentRewardOptions;
    }
}

