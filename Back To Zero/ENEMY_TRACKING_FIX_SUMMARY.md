# Enemy Tracking Fix Summary
**Date:** December 4, 2025

## Problem
Enemy tracking wasn't working for ANY enemies (both melee and ranged). Enemies would detect the player but wouldn't chase them.

## Root Causes Found

### 1. **Missing Player Reference Resolution (EnemyChase.cs)**
- The script would return early every frame if `player` was null
- Never attempted to find the player GameObject using the "Player" tag
- **Fixed:** Added `TryResolvePlayerReference()` method

### 2. **Pathfinding Components Never Enabled (Ranged Small Undead.cs)**
- Components were disabled in `Awake()`
- When aggro triggered, `isAggro = true` was set, but pathfinding components were never re-enabled
- **Fixed:** Now properly enables `AIPath` and `AIDestinationSetter` when aggro is acquired

### 3. **Wrong Component Enable Order**
- `AIDestinationSetter` was being enabled before `AIPath`
- This can cause issues because `AIDestinationSetter.OnEnable()` looks for the `IAstarAI` component
- **Fixed:** Now enables `AIPath` FIRST, then `AIDestinationSetter`

## Files Modified

### 1. `Assets/Scripts/Enemy/EnemyChase.cs`
**Changes:**
- Added `TryResolvePlayerReference()` method
- Now attempts to find player in both `Start()` and `Update()` if not assigned
- Reordered component enabling: `AIPath` before `AIDestinationSetter`
- Added debug logging for player reference resolution

### 2. `Assets/Scripts/Enemy/Ranged Small Undead.cs`
**Changes:**
- Now enables `AIPath` and `AIDestinationSetter` when aggro is acquired
- Sets `aiPath.canMove = true` when aggro is acquired
- Reordered component enabling: `AIPath` before `AIDestinationSetter`
- Properly disables pathfinding when losing aggro

### 3. `Assets/Scripts/Enemy/AIPathDebugger.cs` (NEW)
**Purpose:** Diagnostic tool to help identify pathfinding issues
**Usage:** Attach to any enemy to see detailed debug information
- Shows AIPath component status
- Shows destination and target information
- Checks if AstarPath system is active
- Press 'D' key to manually trigger debug output

## How Enemy Tracking Works Now

### Melee Enemies (EnemyChase.cs)
1. Enemy starts with pathfinding disabled
2. Every frame, checks distance to player and line of sight
3. When player is within `aggroRange` AND has line of sight:
   - Enables `AIPath` component
   - Sets destination to player via `AIDestinationSetter`
   - Enables `AIDestinationSetter`
   - Enemy starts chasing
4. Loses aggro if too far from starting position
5. Returns to starting position when aggro is lost

### Ranged Enemies (Ranged Small Undead.cs)
1. Enemy starts with pathfinding disabled
2. Every frame, checks distance to player and line of sight
3. When player is within `aggroRange` AND has line of sight:
   - Enables `AIPath` component
   - Sets `canMove = true`
   - Sets destination to player via `AIDestinationSetter`  
   - Enables `AIDestinationSetter`
   - Enemy starts chasing AND attacking
4. Loses aggro if too far from starting position (3x aggro range)
5. Can dash away from player when very close

## Testing Instructions

### Quick Test
1. Open any scene with enemies
2. Make sure there's an "A*" GameObject in the scene with `AstarPath` component
3. Make sure the graph is scanned (should happen automatically on startup)
4. Run the game
5. Get close to an enemy
6. **Enemy should now chase you!**

### Detailed Debug Test
1. Attach `AIPathDebugger` component to an enemy
2. Run the game
3. Get close to the enemy
4. Press 'D' key to see debug output in console
5. Check the following values:
   - `AIPath.enabled` should be `true` when chasing
   - `AIPath.canMove` should be `true` when chasing
   - `AIPath.isStopped` should be `false` when chasing
   - `AIDestinationSetter.target` should be "Player"
   - `AstarPath.active` should NOT be null

### Common Issues to Check

#### If enemies still don't move:
1. **Check Player Tag:** Make sure your player GameObject has the tag "Player"
2. **Check A* Graph:** Open the A* inspector and make sure the graph is scanned
3. **Check Enemy Prefabs:** Make sure enemies have these components:
   - `AIPath` component
   - `AIDestinationSetter` component
   - `Seeker` component (required by AIPath)
   - `EnemyChase` or `RangedSmallUndead` script
4. **Check ObstacleMask:** Make sure the `obstacleMask` in enemy scripts doesn't block vision to player
5. **Check aggroRange:** Make sure you're getting close enough to trigger aggro

#### If Path/Graph errors appear:
1. Open Unity Editor
2. Select the "A*" GameObject in scene
3. In Inspector, find the AstarPath component
4. Click "Scan" button to regenerate the navmesh
5. Make sure your tilemap/ground has colliders for graph generation

## Code Changes Summary

### EnemyChase.cs - Before:
```csharp
void Update()
{
    if (player == null) return; // Would never find player!
    // ... rest of code
}
```

### EnemyChase.cs - After:
```csharp
void Update()
{
    if (player == null)
    {
        TryResolvePlayerReference();
        if (player == null) return;
    }
    // ... rest of code
}
```

### Ranged Small Undead.cs - Before:
```csharp
if (!isAggro && distanceToPlayer <= aggroRange && hasLineOfSight)
{
    isAggro = true; // Just sets flag, doesn't enable pathfinding!
}
```

### Ranged Small Undead.cs - After:
```csharp
if (!isAggro && distanceToPlayer <= aggroRange && hasLineOfSight)
{
    isAggro = true;
    // NOW ACTUALLY ENABLES PATHFINDING!
    if (aiPath != null)
    {
        aiPath.enabled = true;
        aiPath.canMove = true;
    }
    if (destinationSetter != null)
    {
        destinationSetter.target = player;
        destinationSetter.enabled = true;
    }
}
```

## What Was NOT Broken

The A* Pathfinding system itself was working fine! The issues were all in how the enemy scripts were using it:
- Not finding the player reference
- Not enabling the pathfinding components
- Enabling components in the wrong order

## Next Steps

1. **Test in all your scenes** to make sure enemies chase correctly
2. **Remove AIPathDebugger** components once everything works
3. **Adjust aggro ranges** if needed for game balance
4. **Consider adding** the player reference resolution to other scripts if they have similar issues

## Additional Notes

- The `KnockbackReceiver` script temporarily stops pathfinding during knockback - this is correct behavior
- The `BossAttack` script doesn't use pathfinding - bosses attack in place
- If you add new enemy types, make sure they follow the same pattern:
  1. Disable pathfinding at start
  2. Enable AIPath BEFORE AIDestinationSetter
  3. Set destination target
  4. Set canMove = true

---

**All fixes have been applied and are ready to test!**

