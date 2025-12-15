# Tree Chop System - Quick Setup Guide

## What Changed

### ? Trees now use CHOP SYSTEM instead of health bars
- **Default: 5 chops** to chop down a tree
- No health bar needed
- Visual feedback with each chop (shake, particles)
- More realistic lumberjack behavior

### ? Lumberjack walks CLOSER to trees
- **Attack Range: 1.5m** (down from 2.5m)
- **Stopping Distance: 1.2m** (down from 2.0m)
- Lumberjack gets right next to tree before chopping
- More accurate chops, less misses

---

## Quick Setup

### Tree Configuration:

1. **Select your tree GameObject**
2. **TreeHealth component settings:**
   ```
   Chops Required: 5  (how many chops to fall)
   Chop Particles: [Wood chips effect]
   Shake Intensity: 0.1
   Shake Duration: 0.2
   Collapse Mode: FallOver (recommended)
   ```

### Lumberjack Configuration:

1. **Select lumberjack GameObject**
2. **LumberjackAI component settings:**
   ```
   Tree Detection Range: 15
   Tree Attack Range: 1.5  ? MUST BE CLOSE!
   Move Speed: 3.5
   Stopping Distance: 1.2  ? STOPS CLOSER!
   Attack Cooldown: 2
   ```

---

## How It Works

### Chop Sequence:
```
1. Lumberjack finds tree (within 15m detection range)
2. Walks to tree until distance = 1.5m or less
3. STOPS and faces tree
4. Chop #1 ? Tree shakes, particles spawn
5. Wait 2 seconds (attack cooldown)
6. Chop #2 ? Tree shakes again
7. ... continues ...
8. Chop #5 ? Tree falls down!
9. Logs spawn (if destroyedTreePrefab set)
```

### Visual Feedback Per Chop:
- ? Tree shakes (0.2 seconds)
- ? Wood chip particles (if assigned)
- ? Chop sound (if assigned)
- ? Console log: "Chopped! 3/5"

---

## Debugging

### Console Output (with Show Debug Logs enabled):

```
[LUMBERJACK] Lumberjack1 found tree: OakTree
[LUMBERJACK] Lumberjack1 | State: SeekingTree | Distance: 8.45m
[LUMBERJACK] Lumberjack1 | State: SeekingTree | Distance: 3.22m
[LUMBERJACK] Lumberjack1 reached tree (distance: 1.48m), starting to chop!
[LUMBERJACK] Lumberjack1 chopped tree! 1/5
[LUMBERJACK] Lumberjack1 chopped tree! 2/5
[LUMBERJACK] Lumberjack1 chopped tree! 3/5
[LUMBERJACK] Lumberjack1 chopped tree! 4/5
[LUMBERJACK] Lumberjack1 chopped tree! 5/5
OakTree has been chopped down!
```

### Scene View (Select Lumberjack):
- **Green sphere**: Detection range (15m)
- **Yellow sphere**: Attack range (1.5m) - SMALL!
- **Cyan line**: Path to tree
- **Text label**: Shows distance and chop count

---

## Troubleshooting

### "Lumberjack not getting close enough"

**Check these settings:**
```
LumberjackAI:
  Tree Attack Range: 1.5 (or less)
  Stopping Distance: 1.2 (or less)

NavMeshAgent:
  Stopping Distance: 1.2 (should match)
  Radius: 0.5 (default)
```

### "Attacks missing the tree"

**Solution:**
- Attack Range is too large
- Set Tree Attack Range to **1.0 - 1.5m** max
- Lumberjack must be VERY close to tree

### "Tree takes 5 chops but doesn't fall"

**Check:**
- TreeHealth component: Chops Required = 5
- Console should show: "1/5", "2/5", ... "5/5"
- Last chop should trigger: "has been chopped down!"

### "Tree shakes but no particles"

**Solution:**
- Assign a particle effect to "Chop Particles" field
- Create simple particle effect:
  - GameObject ? Effects ? Particle System
  - Make it burst wood chips
  - Save as prefab
  - Assign to tree

---

## Customization

### Different Tree Types:

```csharp
// Small tree (quick to chop)
Chops Required: 3
Attack Range: 1.5

// Normal tree
Chops Required: 5
Attack Range: 1.5

// Large tree (hard to chop)
Chops Required: 10
Attack Range: 1.5

// Giant tree (very hard)
Chops Required: 20
Attack Range: 2.0  (bigger trees = larger range)
```

### Faster/Slower Chopping:

```csharp
LumberjackAI:
  Attack Cooldown: 2.0  ? Default
  Attack Cooldown: 1.0  ? Fast chopping
  Attack Cooldown: 3.0  ? Slow, deliberate chopping
```

---

## Visual Improvements

### Better Particles:

1. **Create Wood Chip Effect:**
   ```
   - Small brown particles
   - Burst mode (10-15 particles)
   - Short lifetime (1-2 seconds)
   - Slight gravity
   - Random rotation
   ```

2. **Assign to Tree:**
   - TreeHealth ? Chop Particles ? [Your effect]

### Shake Animation:

```csharp
TreeHealth settings:
  Shake Intensity: 0.05  ? Subtle shake
  Shake Intensity: 0.1   ? Normal shake (default)
  Shake Intensity: 0.2   ? Big shake
  Shake Duration: 0.2    ? Quick snap back
  Shake Duration: 0.5    ? Slow wobble
```

---

## Audio Setup

### Add Sounds:

1. **Chop Sound:**
   - TreeHealth ? Chop Sound ? [Axe hitting wood sound]
   - Plays on each chop

2. **Fall Sound:**
   - TreeHealth ? Fall Sound ? [Tree crashing sound]
   - Plays on final chop (5/5)

3. **Auto-adds AudioSource** if needed

---

## Performance Tips

### Multiple Trees:

- ? System is efficient for many trees
- ? No health bar UI = better performance
- ? Particles only spawn when chopped
- ? Trees clean up after falling

### Many Lumberjacks:

```csharp
// Each lumberjack finds nearest tree independently
// They DON'T fight over same tree (first come, first serve)
// When tree falls, they find next tree automatically
```

---

## Advanced: Custom Chop Counts

### Make specific trees harder:

```csharp
// In Unity Inspector:
Select Tree GameObject
TreeHealth component:
  Chops Required: 10  (instead of 5)
```

### Or via code:

```csharp
// Find tree and change chops required
TreeHealth tree = GetComponent<TreeHealth>();
tree.chopsRequired = 10;  // Make it harder
```

---

## Example Scene Setup

```
Scene:
??? Ground (NavMesh baked)
??? Trees
?   ??? SmallTree (Chops: 3, Layer: Tree)
?   ??? NormalTree (Chops: 5, Layer: Tree)
?   ??? LargeTree (Chops: 10, Layer: Tree)
??? Lumberjacks
    ??? Lumberjack1 (Attack Range: 1.5)
    ??? Lumberjack2 (Attack Range: 1.5)

Press Play:
- Lumberjacks walk to nearest trees
- Each takes 5 chops (or 3/10 depending on tree)
- Trees shake with each chop
- Trees fall after final chop
- Lumberjacks move to next tree
```

---

## Summary

**Key Changes:**
1. ? Trees use chop count (not health) - default 5 chops
2. ? Lumberjack walks VERY close (1.5m attack range)
3. ? Lumberjack stops closer (1.2m stopping distance)
4. ? Visual feedback on each chop (shake + particles)
5. ? Tree falls after reaching chop count

**Result:** More realistic lumberjack behavior! ????

