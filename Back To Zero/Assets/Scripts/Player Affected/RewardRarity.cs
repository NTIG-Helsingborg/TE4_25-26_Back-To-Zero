using UnityEngine;

/// <summary>
/// Rarity levels for level-up rewards (boons), similar to Hades
/// </summary>
public enum RewardRarity
{
    Common,     // Basic boons - small bonuses
    Rare,       // Better boons - medium bonuses
    Legendary,  // Powerful boons - large bonuses
    Cursed      // High-risk, high-reward boons - huge bonuses with penalties
}

/// <summary>
/// Helper class for reward rarity colors and display
/// </summary>
public static class RewardRarityHelper
{
    public static Color GetRarityColor(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common:
                return new Color(0.8f, 0.8f, 0.8f); // Light gray/white
            case RewardRarity.Rare:
                return new Color(0.2f, 0.6f, 1f); // Blue
            case RewardRarity.Legendary:
                return new Color(0.8f, 0.4f, 1f); // Purple/Gold
            case RewardRarity.Cursed:
                return new Color(0.8f, 0.1f, 0.1f); // Dark red
            default:
                return Color.white;
        }
    }

    public static string GetRarityName(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common:
                return "Common";
            case RewardRarity.Rare:
                return "Rare";
            case RewardRarity.Legendary:
                return "Legendary";
            case RewardRarity.Cursed:
                return "Cursed";
            default:
                return "Unknown";
        }
    }
}

