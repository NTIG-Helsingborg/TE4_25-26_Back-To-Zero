# Artifact System - Quick Setup Guide

## Overview
I've extended your existing `ItemSO` system to support **Artifacts** - permanent stat buffs that enhance player abilities.

## What Changed

### 1. **ItemSO.cs** - Extended
- Added `ItemType` enum: **Consumable** or **Artifact**
- Consumables work as before (heal, use once)
- Artifacts provide **permanent stat buffs**

### 2. **PlayerStats.cs** - New Component
- Manages all player stat modifiers
- Automatically applies to existing PlayerMove and Health components
- Centralizes buff calculations

## Quick Setup (3 Steps)

### Step 1: Add PlayerStats to Player
1. Select your **Player** GameObject
2. Add Component → **PlayerStats**
3. Configure base stats in the Inspector

### Step 2: Create Artifact Items
1. Right-click in Project → **Create → ItemSO**
2. Configure the item:
   - **Item Type**: Set to **Artifact**
   - **Stat To Change**: Choose buff type
   - **Amount To Change Stat**: Set buff amount

### Step 3: Use Artifacts
Artifacts work exactly like your existing items:
- Pick them up with the `Item` component
- They appear in inventory
- Click to "use" (applies permanent buff)
- The item is consumed from inventory

## Stat Mappings

### Health → Max Health
- **Amount**: Direct HP increase
- Example: 50 = +50 max HP

### Power → Damage Multiplier  
- **Amount**: Percentage increase
- Example: 25 = +25% damage

### Agility → Move Speed
- **Amount**: Speed increase (×0.1)
- Example: 10 = +1.0 move speed

### Intelligence → Attack Speed
- **Amount**: Percentage increase  
- Example: 20 = +20% attack speed

## Example Artifacts

### "Speed Boots" (Agility)
- Item Type: **Artifact**
- Stat To Change: **Agility**
- Amount: **15**
- Effect: +1.5 move speed permanently

### "Ring of Power" (Power)
- Item Type: **Artifact**
- Stat To Change: **Power**
- Amount: **30**
- Effect: +30% damage permanently

### "Titan's Heart" (Health)
- Item Type: **Artifact**
- Stat To Change: **Health**
- Amount: **50**
- Effect: +50 max HP permanently

## That's It!

The system uses your existing:
- ✅ ItemSO system
- ✅ Inventory UI
- ✅ Item pickup system
- ✅ Item usage system

Just set `Item Type = Artifact` and you're done! 🎉
