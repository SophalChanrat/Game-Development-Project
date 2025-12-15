# Lumberjack Combat System - Quick Setup Guide

## ? Changes Made

The lumberjack can now take damage from all player attack sources!

### Updated Systems:

1. ? **Player Melee Attacks** (`CharacterMovement.cs`)
2. ? **Particle/Skill Damage** (`ParticleDamage.cs`)
3. ? **Legacy Skill Effects** (`SkillEffect.cs`)
4. ? **Lock-On System** (`CinemachineLockOn.cs`)

---

## ?? How It Works

### Player Can Now Damage Lumberjacks:

```
Player Attack ? Detects Lumberjack ? Applies Damage ? Lumberjack Fights Back!
```

### Lumberjack Response:
```
If Can Fight Back = true:
  Take Damage ? Switch to ChasingPlayer State ? Attack Player!
  
If Can Fight Back = false:
  Take Damage ? Continue chopping trees ? Die at 0 HP
```

---

## ?? Setup in Unity

### 1. Lumberjack GameObject Setup:

```
Lumberjack GameObject:
?? Layer: Enemy (IMPORTANT!)
?? Collider (any type)
?? NavMeshAgent
?? Animator
?? LumberjackAI Script
   ?? Health: 100
   ?? Can Fight Back: ? (check to attack player)
   ?? Player Damage: 20
   ?? Tree Layer: Tree
```

### 2. Player Settings:

```
Player GameObject:
?? PlayerMovement3D Script
   ?? Attack Damage: 25
   ?? Attack Range: 2
   ?? Enemy Layer: Enemy (must include lumberjack!)
```

**CRITICAL:** Make sure lumberjack is on the "Enemy" layer!

---

## ?? Layer Configuration

### Required Layers:

1. **Enemy Layer:**
   - Regular enemies (EnemyAI)
   - Lumberjacks (LumberjackAI)
   - Both can be targeted by player

2. **Tree Layer:**
   - Trees (TreeHealth)
   - Only targeted by lumberjacks

### Setup:
```
1. Edit ? Project Settings ? Tags and Layers
2. Ensure these layers exist:
   - Enemy (usually Layer 6 or 7)
   - Tree (usually Layer 8)
3. Set lumberjack GameObject ? Layer ? Enemy
4. Set tree GameObjects ? Layer ? Tree
```

---

## ?? Combat Flow

### Scenario 1: Player Attacks Lumberjack (Fight Back Enabled)

```
1. Player attacks lumberjack
   ?
2. Lumberjack takes damage
   ?
3. Lumberjack switches from "SeekingTree" to "ChasingPlayer"
   ?
4. Lumberjack chases and attacks player
   ?
5. After timeout (10s default), returns to chopping trees
```

### Scenario 2: Player Attacks Lumberjack (Fight Back Disabled)

```
1. Player attacks lumberjack
   ?
2. Lumberjack takes damage
   ?
3. Lumberjack continues chopping trees (ignores player)
   ?
4. Lumberjack dies at 0 HP
```

### Scenario 3: Lumberjack Finds Trees While Chasing Player

```
Player attacks lumberjack
   ?
Lumberjack chases player (2+ seconds)
   ?
Tree appears in detection range
   ?
Lumberjack says "Found tree, switching back!"
   ?
Returns to chopping trees (trees = priority!)
```

---

## ??? Lumberjack Settings Explained

### Combat Settings:

| Setting | Default | Description |
|---------|---------|-------------|
| **Health** | 100 | Hit points |
| **Can Fight Back** | ? | Attack player when damaged |
| **Player Damage** | 20 | Damage per hit to player |
| **Player Attack Range** | 2.0m | How close to get before attacking |
| **Chase Timeout** | 10s | How long to chase before giving up |

### Tree Priority System:

```
Priority 1: Trees (primary job)
Priority 2: Player (if attacked and fight back enabled)
Priority 3: Wander (if nothing else to do)
```

**Note:** Even while chasing player, lumberjack will switch back to trees if one appears!

---

## ?? Attack Types Supported

### 1. Melee Attack (Primary):
- Player's basic attack animation
- Called via animation event: `ApplyAttackDamage()`
- Range: 2 meters
- Damage: 25 (configurable)

### 2. Skill/Particle Attacks:
- Fire meteors, ice shards, etc.
- Particle collision damage
- Radius-based damage
- All work on lumberjacks!

### 3. Lock-On System:
- ? Can lock onto lumberjacks
- ? Same as regular enemies
- ? Switch between multiple targets

---

## ?? Troubleshooting

### "Player can't damage lumberjack"

**Check:**
1. Lumberjack Layer = Enemy ?
2. Player ? Enemy Layer mask includes Enemy ?
3. Lumberjack has Collider ?
4. Lumberjack Health > 0 ?

### "Lumberjack doesn't fight back"

**Check:**
1. Can Fight Back = ? (checked)
2. Player tag = "Player" ?
3. Player has PlayerHealth component ?

### "Can't lock onto lumberjack"

**Check:**
1. Lumberjack Layer = Enemy ?
2. CinemachineLockOn ? Enemy Layer includes Enemy ?
3. Lumberjack within lock-on range (15m default) ?

### "Lumberjack ignores trees after fighting"

**This is intentional!** After chase timeout:
- Returns to Idle state
- Searches for trees again
- Resumes normal lumberjack behavior

---

## ?? Damage Flow Diagram

```
PLAYER ATTACK
    ?
    ??? OverlapBox (hitbox detection)
    ?      ?
    ?      ??? Check for EnemyAI ? TakeDamage()
    ?      ??? Check for LumberjackAI ? TakeDamage()
    ?
    ??? Particle Collision
           ?
           ??? Check for EnemyAI ? TakeDamage()
           ??? Check for LumberjackAI ? TakeDamage()

LUMBERJACK RESPONSE
    ?
    ??? If Can Fight Back:
    ?      ??? Chase Player ? Attack
    ?
    ??? If Can't Fight Back:
           ??? Ignore ? Continue working
```

---

## ?? Example Configurations

### Peaceful Lumberjack (Non-Hostile):
```
Health: 50
Can Fight Back: ? (unchecked)
```
**Result:** Takes damage, doesn't fight back, continues chopping trees until death.

### Aggressive Lumberjack (Hostile):
```
Health: 150
Can Fight Back: ? (checked)
Player Damage: 30
Chase Timeout: 15
```
**Result:** Fights back hard, chases longer, deals more damage.

### Boss Lumberjack (Very Hard):
```
Health: 500
Can Fight Back: ?
Player Damage: 50
Attack Cooldown: 1.0
Chase Timeout: 30
```
**Result:** Tank lumberjack that hunts player relentlessly!

---

## ?? Tips

1. **Balance Health:**
   - Regular enemy: 100 HP
   - Lumberjack: 100-150 HP (they're tougher!)
   
2. **Layer Management:**
   - Keep lumberjacks on Enemy layer
   - Keep trees on Tree layer
   - Don't mix them!

3. **Fight Back Setting:**
   - Early game: Disable fight back (peaceful)
   - Mid game: Enable fight back (challenge)
   - Boss fight: Max stats + enabled

4. **Tree Priority:**
   - Lumberjacks ALWAYS prefer trees over combat
   - Use this strategically in level design
   - Place trees to distract lumberjacks!

---

## ?? Animation Events

Make sure lumberjack animations have these events:

```
Attack Animation:
  ?? Event at hit frame: "ApplyAttackDamage()" (if using animation events)

Death Animation:
  ?? Event at end: Optional cleanup
```

---

## ?? Console Debug Messages

Enable "Show Debug Logs" to see:

```
[LUMBERJACK] Lumberjack1 took 25 damage! Health: 75
[LUMBERJACK] Lumberjack1 | State: ChasingPlayer
[LUMBERJACK] Lumberjack1 attacked player for 20 damage!
[LUMBERJACK] Lumberjack1 found tree while chasing, switching back
```

---

## ? Summary

**What works now:**
- ? Player can attack lumberjacks
- ? Lumberjacks take damage from all sources
- ? Lumberjacks can fight back (optional)
- ? Lock-on works on lumberjacks
- ? Skills/particles damage lumberjacks
- ? Trees remain priority #1
- ? Lumberjacks die at 0 HP

**Lumberjack Layer = Enemy** is the most important setting! ??

