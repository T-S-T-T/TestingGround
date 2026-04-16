# Titanfall 2-Style Movement System — Setup Guide

## Files
- `PlayerMovement.cs` — Main physics & movement controller
- `PlayerCamera.cs`   — First-person camera with tilt and FOV effects

---

## Quick Setup

### 1. Hierarchy
```
PlayerRoot (GameObject)
├── PlayerMovement.cs       ← attach here
├── CharacterController     ← auto-required
└── CameraMount (empty child, height = eye level, e.g. y = 1.6)
    └── Main Camera
        └── PlayerCamera.cs ← attach here
```

### 2. CharacterController settings
| Property | Recommended |
|----------|------------|
| Radius   | 0.35       |
| Height   | 1.8        |
| Center Y | 0.9        |
| Skin Width | 0.05     |
| Step Offset | 0.35    |
| Slope Limit | 55°     |

### 3. Layer Masks
Create two layers in **Edit → Project Settings → Tags and Layers**:
- `Ground` — apply to floors, terrain
- `Wall`   — apply to wallrun-able surfaces

Then assign both layers in the **PlayerMovement** inspector.

### 4. Physics settings (Project Settings → Physics)
- Set gravity to `(0, -28, 0)` **or** leave default and rely on the script's internal gravity
  *(the script uses its own gravity value — default Unity gravity is ignored by CharacterController)*

---

## Feature Overview

### Ground Movement
- **Walk**: WASD at `walkSpeed`
- **Sprint**: hold `Left Shift` — transitions to `sprintSpeed`
- Acceleration and deceleration curves are separate for ground vs air

### Jump
- **Normal jump**: `Space` while grounded (includes coyote time)
- **Double jump**: `Space` a second time in mid-air
- **Jump buffering**: press Space just before landing and the jump fires on touch-down

### Wall Running
- Run horizontally along any surface tagged `Wall`
- Requires minimum approach speed (`wallMinSpeed`)
- Camera tilts toward the wall automatically
- Resets the double jump — you can chain: wall run → wall jump → double jump

### Wall Climbing
- Face a wall and move forward with upward velocity
- Triggered when moving up and pressing into a wall at low horizontal speed
- Has a time limit (`climbDuration`)

### Wall Jump
- Press `Space` while wall running or climbing
- Launches the player away from the wall + upward
- A cooldown prevents immediately re-grabbing the same surface

### Sliding
- Hold `Left Ctrl` (or `C`) + `Left Shift` while sprinting on the ground
- Gives a speed boost in the direction of movement
- Gains extra speed on downward slopes
- Character crouches (CharacterController height shrinks)

---

## Customisation Tips

| Want to...                        | Tweak                                |
|----------------------------------|--------------------------------------|
| Snappier air control             | Increase `airAccel`                  |
| Longer wall runs                 | Increase `wallRunDuration`           |
| Higher feeling double jump       | Increase `doubleJumpHeight`          |
| Floatier jumps                   | Reduce `fallMultiplier` toward 1.0   |
| Faster slides                    | Increase `slideSpeed`                |
| More dramatic camera tilt        | Increase `wallTiltAngle` in camera   |
| Bunny-hop momentum               | Allow `horizontalVel` to exceed cap in air (already partially implemented via `maxSpeed` logic) |

---

## Adding an Animation Controller

`PlayerMovement` exposes public properties you can read from an Animator script:
```csharp
// Example AnimationController.cs
var pm = GetComponent<PlayerMovement>();
animator.SetFloat("Speed",       pm.Speed);
animator.SetBool ("IsGrounded",  pm.IsGrounded);
animator.SetBool ("IsSliding",   pm.IsSliding);
animator.SetBool ("IsWallRunning", pm.CurrentWall == PlayerMovement.WallState.Running);
animator.SetBool ("IsClimbing",    pm.CurrentWall == PlayerMovement.WallState.Climbing);
```
