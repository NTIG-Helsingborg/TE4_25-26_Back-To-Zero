using UnityEngine;

/// <summary>
/// Static class to track power bonuses from equipped artifacts as percentage increases
/// </summary>
public static class PowerBonus
{
    private static int totalPowerPercentage = 0;

    /// <summary>
    /// Add power percentage bonus (can be negative to remove)
    /// </summary>
    public static void AddPowerBonus(int percentAmount)
    {
        totalPowerPercentage += percentAmount;
        Debug.Log($"PowerBonus: Added {percentAmount}% power. Total: {totalPowerPercentage}%");
    }

    /// <summary>
    /// Get the total power percentage (e.g., 10 means 10%)
    /// </summary>
    public static int GetTotalPowerPercentage()
    {
        return totalPowerPercentage;
    }

    /// <summary>
    /// Get the damage multiplier (e.g., 10% = 1.1x)
    /// </summary>
    public static float GetDamageMultiplier()
    {
        return 1f + (totalPowerPercentage / 100f);
    }

    /// <summary>
    /// Reset all power bonuses (useful for game restart)
    /// </summary>
    public static void Reset()
    {
        totalPowerPercentage = 0;
    }
}
