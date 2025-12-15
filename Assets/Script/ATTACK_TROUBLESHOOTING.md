# ?? Lumberjack Not Taking Damage - Troubleshooting Guide

## Quick Diagnosis Tool

### Step 1: Add Debug Component
1. Select **Player** GameObject in Hierarchy
2. Add Component ? Scripts ? **Player Attack Debugger**
3. Check ? **Show Hitbox Gizmo**
4. Check ? **Show Detection Sphere**

### Step 2: Test Attack
1. Press Play
2. Face lumberjack
3. Press attack button
4. Check Console for debug messages

---

## Common Issues & Solutions

### ? Issue #1: "NO ENEMIES DETECTED IN HITBOX"

**Cause:** Layer mask not set up correctly

**Solution:**
```
1. Select Player GameObject
2. Find PlayerMovement3D component
3. Look for "Enemy Layer" field
4. Click on the layer mask dropdown
5. Make sure "Enemy" is CHECKED ?
```

**Visual Check:**
- In Scene view, select player
- You should see a RED wireframe box in front of player (hitbox)
- Attack and watch console for which layers are detected

---

### ? Issue #2: Lumberjack Not on Enemy Layer

**Cause:** Lumberjack GameObject is on wrong layer

**Solution:**
```
1. Select Lumberjack GameObject
2. Look at top of Inspector
3. Layer dropdown should show: "Enemy"
4. If not, click it and select "Enemy"
5. Click "Yes, change children" if asked
```

**How to Create Enemy Layer:**
```
1. Edit ? Project Settings ? Tags and Layers
2. Find first empty User Layer slot
3. Type: "Enemy"
4. Close window
```

---

### ? Issue #3: No Collider on Lumberjack

**Cause:** Lumberjack missing collider component

**Solution:**
```
1. Select Lumberjack GameObject
2. Check if it has a Collider component:
   - Capsule Collider (recommended)
   - Box Collider
   - Mesh Collider
3. If none, Add Component ? Physics ? Capsule Collider
4. Adjust collider size to fit lumberjack model
```

**Collider Settings:**
```
Capsule Collider:
- Radius: 0.5
- Height: 2.0
- Center: (0, 1, 0)
- Is Trigger: ? (unchecked)
```

---

### ? Issue #4: Too Far from Lumberjack

**Cause:** Player attack hitbox doesn't reach lumberjack

**Solution:**
```
1. Select Player
2. PlayerMovement3D component
3. Increase these values:
   - Hitbox Forward Offset: 1.5 (or higher)
   - Hitbox Size X: 2
   - Hitbox Size Y: 2
   - Hitbox Size Z: 2
```

**How to Test:**
```
1. Select Player in Scene view
2. Look for RED wireframe box (hitbox)
3. Walk toward lumberjack
4. Hitbox should overlap lumberjack when attacking
```

---

### ? Issue #5: Animation Event Not Set Up

**Cause:** Attack animation doesn't call damage method

**Solution:**
```
1. Select Player
2. Window ? Animation ? Animation
3. Select attack animation clip
4. Find the frame where weapon hits
5. Add Animation Event:
   - Function: ApplyAttackDamage
   - Time: 0.3-0.5 (mid-swing)
```

**Quick Test:**
```
Press attack ? Check console
Should see: "Hit lumberjack LumberjackName for 25 damage!"
```

---

### ? Issue #6: Layer Mask Value is 0

**Cause:** Enemy layer mask not assigned

**Check in Inspector:**
```
PlayerMovement3D:
  Enemy Layer: Nothing ?  <- WRONG!
  Enemy Layer: Enemy ?    <- CORRECT!
```

**Debug Check:**
```
Console should show:
"Enemy Layer Mask: 64" (or similar non-zero number)

If shows "Enemy Layer Mask: 0" ? NOT SET UP!
```

---

## Step-by-Step Setup Verification

### 1. Layer Setup
```
? "Enemy" layer exists (Edit ? Project Settings ? Tags & Layers)
? "Tree" layer exists
? Lumberjack GameObject ? Layer = Enemy
? Tree GameObject ? Layer = Tree
```

### 2. Player Setup
```
? Player has PlayerMovement3D component
? PlayerMovement3D ? Enemy Layer = Enemy (checked)
? PlayerMovement3D ? Attack Damage = 25
? PlayerMovement3D ? Hitbox Size = (1, 1, 1) or larger
? PlayerMovement3D ? Hitbox Forward Offset = 1.0 or higher
```

### 3. Lumberjack Setup
```
? Lumberjack has LumberjackAI component
? Lumberjack has Collider (Capsule/Box)
? Lumberjack Layer = Enemy
? LumberjackAI ? Health = 100
? LumberjackAI ? Can Fight Back = ? (for testing)
```

### 4. Animation Setup (Critical!)
```
? Player attack animation exists
? Attack animation has Animation Event
? Event calls: "ApplyAttackDamage"
? Event timing: Mid-swing (around 0.3-0.5 seconds)
```

---

## Debug Console Output Guide

### ? WORKING - You Should See:

```
[ATTACK DEBUG] Total Hits Detected: 1
[ATTACK DEBUG] ? Found 1 enemies in hitbox:
[ATTACK DEBUG] - Hit: Lumberjack
[ATTACK DEBUG]   Layer: Enemy
[ATTACK DEBUG]   Distance: 1.5m
[ATTACK DEBUG]   ? Has LumberjackAI (Health: 100)

Hit lumberjack Lumberjack for 25 damage!
[LUMBERJACK] Lumberjack took 25 damage! Health: 75
```

### ? NOT WORKING - Common Errors:

#### Error 1: No Hits
```
[ATTACK DEBUG] ? NO ENEMIES DETECTED IN HITBOX!
[ATTACK DEBUG] Total objects in hitbox (all layers): 0
```
**Fix:** You're too far away or hitbox is too small

#### Error 2: Wrong Layer
```
[ATTACK DEBUG] Total objects in hitbox (all layers): 1
[ATTACK DEBUG] - Found: Lumberjack on layer Default
```
**Fix:** Change lumberjack to Enemy layer

#### Error 3: No Component
```
[ATTACK DEBUG] - Hit: Lumberjack
[ATTACK DEBUG]   ? No EnemyAI or LumberjackAI component!
```
**Fix:** Add LumberjackAI component to lumberjack

#### Error 4: Layer Mask Not Set
```
[ATTACK DEBUG] Enemy Layer Mask: 0
[ATTACK DEBUG] Total Hits Detected: 0
```
**Fix:** Set Enemy Layer in PlayerMovement3D

---

## Visual Debugging in Scene View

### What You Should See:

**When Player is Selected:**
```
?? Red Wireframe Box = Attack hitbox (where damage happens)
?? Blue Line = Forward direction
?? Green Wire Sphere = Attack range reference (legacy)
?? Yellow Wire Box = Predicted hitbox position (from debugger)
```

**What to Check:**
1. Red hitbox should extend in front of player
2. When facing lumberjack, hitbox should overlap lumberjack
3. If hitbox is behind or to the side ? Problem with forward offset

---

## Quick Fix Checklist

Use this order to fix the issue:

### Priority 1: Layers
```
? 1. Create "Enemy" layer
? 2. Set lumberjack ? Layer = Enemy
? 3. Player ? Enemy Layer mask = Enemy ?
```

### Priority 2: Collider
```
? 4. Add Capsule Collider to lumberjack
? 5. Adjust collider to fit lumberjack
? 6. Make sure Is Trigger = ? (unchecked)
```

### Priority 3: Components
```
? 7. Lumberjack has LumberjackAI script
? 8. Player has PlayerMovement3D script
? 9. Both scripts are enabled ?
```

### Priority 4: Animation
```
? 10. Attack animation has Animation Event
? 11. Event calls "ApplyAttackDamage"
? 12. Event timing is correct (mid-swing)
```

---

## Test Scene Setup

**Minimal working setup:**

```
1. Create empty scene
2. Add Ground plane
3. Add Player:
   - CharacterController
   - PlayerMovement3D
     - Enemy Layer = Enemy
     - Attack Damage = 25
     - Hitbox Size = (2, 2, 2)
   - Animator with attack animation + event

4. Add Lumberjack (5 units away):
   - Layer = Enemy
   - Capsule Collider
   - NavMeshAgent
   - LumberjackAI
   - Animator

5. Press Play ? Walk to lumberjack ? Attack
   
Expected: "Hit lumberjack for 25 damage!" in console
```

---

## Still Not Working?

### Enable All Debug Logs:

1. **Player Debugger:**
   ```
   Add PlayerAttackDebugger component
   Enable all checkboxes
   ```

2. **Lumberjack Logs:**
   ```
   LumberjackAI ? Show Debug Logs = ?
   ```

3. **Attack Lumberjack:**
   ```
   Press attack button
   Copy ALL console output
   Check against examples above
   ```

### Common Output Meanings:

| Console Message | Meaning | Fix |
|----------------|---------|-----|
| "Enemy Layer Mask: 0" | Layer not set | Set Enemy Layer in inspector |
| "Total Hits: 0" | Nothing in hitbox | Get closer or increase hitbox size |
| "Found: X on layer Default" | Wrong layer | Change to Enemy layer |
| "No EnemyAI or LumberjackAI" | Missing script | Add LumberjackAI component |
| "Hit lumberjack for X damage!" | ? WORKING! | Success! |

---

## Advanced: Layer Mask Binary Check

If you're still having issues, check layer mask value:

```csharp
// In console, check this value:
Enemy Layer Mask: 64  // Binary: 01000000 (Layer 6)
Enemy Layer Mask: 128 // Binary: 10000000 (Layer 7)

// If you see:
Enemy Layer Mask: 0   // ? NOT SET!
```

**Fix:**
1. Make sure Enemy layer is assigned a number (6, 7, or 8)
2. Player ? Enemy Layer ? Check that specific layer

---

## Summary

**Most common issue: Layer not set correctly!**

**Quick fix:**
1. ? Lumberjack Layer = Enemy
2. ? Player ? Enemy Layer mask = Enemy
3. ? Attack animation has Animation Event

**If still broken:**
- Add PlayerAttackDebugger
- Check console output
- Compare with examples above
