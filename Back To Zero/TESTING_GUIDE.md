# Enemy Tracking Testing Guide

## Current Status
- ✅ **Ranged Small Undead** - Working (after removing duplicate EnemyChase)
- ❓ **Small Undead** - Not chasing (needs testing)
- ❓ **Big Undead** - Not chasing (needs testing)

---

## Quick Tests to Run

### Test 1: Check Console for Errors

**Run the game and look for these messages:**

✅ **Good messages (you WANT to see these):**
```
[EnemyChase] Small Undead(Clone): Initialized successfully
[EnemyChase] Small Undead(Clone): Found player reference: Player
[EnemyChase] Small Undead(Clone): AGGRO ACTIVATED! Distance: 5.23
```

❌ **Bad messages (ERROR - means something is missing):**
```
[EnemyChase] Small Undead(Clone): Missing AIPath component!
[EnemyChase] Small Undead(Clone): Missing AIDestinationSetter component!
[EnemyChase] Small Undead(Clone): Could not find GameObject with tag 'Player'
```

---

### Test 2: Use Debug Helper

**In Unity Editor:**
1. Select a **Small Undead** enemy in the Hierarchy (while game is running)
2. In Inspector, click **Add Component**
3. Type **"EnemyDebugHelper"** and add it
4. Move your player near the enemy
5. **Press 'E' key** - this will dump debug info to console
6. Look for the output and check:
   - Is `Player is NULL`? → Player tag issue
   - Is `AIPath is NULL`? → Missing component on prefab
   - Is `AIPath.enabled: false`? → Pathfinding not activating
   - Is `Distance to player` > 8? → You're too far away

---

### Test 3: Visual Debug (Best Option!)

**In Unity Editor while game is PLAYING:**
1. Select a **Small Undead** enemy in Hierarchy
2. Add **EnemyDebugHelper** component
3. Make sure **Gizmos are enabled** in Scene view (button at top)
4. You should see:
   - **Yellow line** from enemy to player
   - **Red circle** around enemy (aggro range = 8 units)
   - **Text above enemy** showing status

5. Walk your player toward the enemy and watch:
   - When player enters red circle
   - Does "AIPath: OFF" change to "AIPath: ON"?
   - Does the enemy start moving?

---

## Common Issues & Fixes

### Issue 1: "Missing AIPath component"
**Problem:** Enemy prefab is missing pathfinding components

**Fix:**
1. Open prefab: `Assets/Prefabs/Enemies/Small Undead.prefab`
2. Check if these components exist:
   - Seeker (Script)
   - AIPath (2D,3D) (Script)
   - AI Destination Setter (Script)
   - Enemy Chase (Script)
3. If missing, add them via **Add Component** button

---

### Issue 2: "Could not find GameObject with tag 'Player'"
**Problem:** Player doesn't have the correct tag

**Fix:**
1. Select your Player in the Hierarchy
2. At the top of Inspector, change **Tag** dropdown to **"Player"**
3. If "Player" tag doesn't exist:
   - Click **Tag** → **Add Tag**
   - Add "Player" tag
   - Go back and assign it

---

### Issue 3: Enemy detects but doesn't move
**Problem:** `obstacleMask` might be blocking line of sight

**Fix:**
1. Select enemy prefab
2. Find **Enemy Chase (Script)** component
3. Look at **Obstacle Mask** - make sure it ONLY includes walls/obstacles
4. Make sure it does NOT include:
   - Player layer
   - Enemy layer
   - Default layer (if your player is on Default)

---

### Issue 4: Aggro range too small
**Problem:** You need to get very close before enemy reacts

**Fix:**
1. Select enemy prefab
2. In **Enemy Chase (Script)**, increase **Aggro Range** from 8 to 15 (or higher)
3. Test again

---

## Debug Scripts Available

### 1. **QuickDebugCheck**
- **What it does:** Checks entire scene setup on game start
- **How to use:** Attach to any GameObject, press Play
- **Shows:** Player found? A* system active? All enemies have components?

### 2. **EnemyDebugHelper**
- **What it does:** Shows live debug info for ONE enemy
- **How to use:** Attach to a specific enemy, press 'E' while playing
- **Shows:** Distance, component status, pathfinding state

### 3. **AIPathDebugger**
- **What it does:** Detailed A* pathfinding diagnostics
- **How to use:** Attach to enemy, press 'D' while playing
- **Shows:** Complete pathfinding system status

---

## Step-by-Step Debugging Process

**Run this procedure for Small Undead and Big Undead:**

1. **Play the game**
2. **Check console immediately** - look for initialization messages
3. **If you see errors** → Read error message and follow fix above
4. **If no errors**:
   - Add **EnemyDebugHelper** to one enemy
   - Walk toward it
   - Watch the Scene view for visual feedback
   - Press **'E'** when close to see detailed output
5. **Check the output**:
   - All components present? ✅
   - Player found? ✅
   - Distance < 8? ✅
   - AIPath enabled? ← This should turn ON when you're close
6. **If AIPath stays OFF**:
   - Check console for "AGGRO ACTIVATED" message
   - If missing → `HasLineOfSight()` might be returning false
   - Check obstacle mask settings

---

## Expected Behavior

### When Working Correctly:

1. **Game starts:**
   - Console shows: "Initialized successfully" for each enemy
   - Console shows: "Found player reference: Player"

2. **Player approaches enemy (within 8 units):**
   - Console shows: "AGGRO ACTIVATED! Distance: X.XX"
   - Enemy sprite starts moving toward player
   - Enemy keeps chasing until player gets far away (40+ units)

3. **Player runs away:**
   - Enemy chases for a while
   - Eventually gives up and returns to starting position
   - Enemy stops moving when back at start

---

## Quick Checklist

Before asking for more help, verify:

- [ ] Game is actually running (not paused)
- [ ] Player has "Player" tag
- [ ] Player is within 8 units of enemy
- [ ] No console errors on game start
- [ ] Enemy prefabs have all required components:
  - [ ] Seeker
  - [ ] AIPath
  - [ ] AIDestinationSetter
  - [ ] EnemyChase (for melee) OR RangedSmallUndead (for ranged)
- [ ] There's an "A*" GameObject in the scene
- [ ] The A* graph is scanned (can check in A* inspector)

---

## Next Steps

1. **Run Test 1** (check console)
2. **If errors appear** → Follow fixes above
3. **If no errors** → Run Test 3 (visual debug)
4. **Take a screenshot** of:
   - Console output
   - Enemy Inspector showing all components
   - Scene view with enemy selected and gizmos visible
5. **Report back** with what you see!

---

**The debug helpers will tell us exactly why enemies aren't chasing!** 🔍

