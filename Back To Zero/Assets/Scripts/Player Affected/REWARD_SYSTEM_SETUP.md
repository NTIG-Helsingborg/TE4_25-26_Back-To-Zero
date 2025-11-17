# Level Up Reward System Setup Guide

## Overview
This is a Hades-style boon system where players get 3 reward choices per level up. Rewards have rarities (Common, Rare, Legendary, Cursed) and can modify player stats.

## Quick Setup

### 1. Create Reward ScriptableObjects
1. Right-click in Project window → **Create > Rewards > Reward Boon**
2. Name it (e.g., "Power Boost", "Berserker's Rage")
3. Configure:
   - **Reward Name**: Display name
   - **Description**: What it does
   - **Rarity**: Common, Rare, Legendary, or Cursed
   - **Stat Modifications**: Set positive values for buffs, negative for debuffs

### 2. Organize Rewards
**Option A - Resources Folder (Recommended):**
- Create folder: `Assets/Resources/Rewards/`
- Place all RewardSO files here
- RewardManager will auto-load them

**Option B - Manual Assignment:**
- Add RewardManager component to a GameObject in scene
- Assign all RewardSO files to the "All Rewards" array in Inspector

### 3. Setup UI Panels
Each reward panel GameObject needs:
- **Button component** (for clicking)
- **RewardPanelDisplay component** (auto-added by ExperienceManager)
- **UI Elements** (assign in RewardPanelDisplay):
  - Name Text (TextMeshProUGUI)
  - Description Text (TextMeshProUGUI)
  - Stat Changes Text (TextMeshProUGUI)
  - Rarity Text (TextMeshProUGUI) - optional
  - Background Image (Image) - optional, will be colored by rarity

### 4. Configure ExperienceManager
- Ensure `rewardPanels` array has 3 panels assigned
- RewardManager will be auto-created if missing

## Example Rewards

### Common (Small bonuses)
- **Minor Power**: +5% Damage
- **Health Boost**: +15 Max Health
- **Swift Feet**: +5% Move Speed

### Rare (Medium bonuses)
- **Power Surge**: +15% Damage
- **Vitality**: +30 Max Health
- **Wind Walker**: +12% Move Speed

### Legendary (Large bonuses)
- **Divine Strike**: +30% Damage, +10% Attack Speed
- **Immortal Flesh**: +60 Max Health, +15% Defense
- **Master Warrior**: +20% Damage, +20% Attack Speed

### Cursed (High risk/reward)
- **Berserker's Rage**: +50% Damage, -20% Defense
- **Glass Cannon**: +40% Damage, -30 Max Health
- **Blood Pact**: +100 Max Health, -30% Move Speed

## Rarity Weights
Default chances (can be adjusted in RewardManager):
- Common: 60%
- Rare: 25%
- Legendary: 10%
- Cursed: 5%

## How It Works
1. Player levels up → ExperienceManager triggers
2. RewardManager generates 3 random rewards (weighted by rarity)
3. Rewards displayed on panels with rarity colors
4. Player selects one → Reward applied to PlayerStats
5. Game resumes

## Notes
- Rewards stack (can get same stat multiple times)
- Cursed rewards always have both buffs and debuffs
- No duplicate rewards in same selection
- Game pauses during selection (Time.timeScale = 0)

