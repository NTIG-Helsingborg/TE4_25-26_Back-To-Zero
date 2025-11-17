using UnityEngine;

/// <summary>
/// Helper class with example reward data
/// Use this as a reference when creating RewardSO ScriptableObjects
/// 
/// To create rewards:
/// 1. Right-click in Project window
/// 2. Create > Rewards > Reward Boon
/// 3. Configure the reward using the examples below as reference
/// 4. Place them in a Resources/Rewards folder (or assign to RewardManager manually)
/// </summary>
public static class RewardExamples
{
    // Example reward configurations (for reference when creating ScriptableObjects)
    
    /* COMMON REWARDS - Small bonuses
     * 
     * "Minor Power Boost"
     * +5% Damage
     * 
     * "Health Boost"
     * +15 Max Health
     * 
     * "Swift Feet"
     * +5% Move Speed
     * 
     * "Tough Skin"
     * +5% Defense
     */
    
    /* RARE REWARDS - Medium bonuses
     * 
     * "Power Surge"
     * +15% Damage
     * 
     * "Vitality"
     * +30 Max Health
     * 
     * "Wind Walker"
     * +12% Move Speed
     * 
     * "Iron Will"
     * +10% Defense, +10 Max Health
     */
    
    /* LEGENDARY REWARDS - Large bonuses
     * 
     * "Divine Strike"
     * +30% Damage, +10% Attack Speed
     * 
     * "Immortal Flesh"
     * +60 Max Health, +15% Defense
     * 
     * "Lightning Reflexes"
     * +25% Move Speed, +15% Attack Speed
     * 
     * "Master Warrior"
     * +20% Damage, +20% Attack Speed, +10% Crit Chance
     */
    
    /* CURSED REWARDS - High risk, high reward
     * 
     * "Berserker's Rage"
     * +50% Damage, -20% Defense
     * 
     * "Glass Cannon"
     * +40% Damage, -30 Max Health
     * 
     * "Cursed Speed"
     * +40% Move Speed, -25% Defense
     * 
     * "Blood Pact"
     * +100 Max Health, -30% Move Speed
     * 
     * "Reckless Fury"
     * +60% Damage, +20% Attack Speed, -40% Defense
     */
}

