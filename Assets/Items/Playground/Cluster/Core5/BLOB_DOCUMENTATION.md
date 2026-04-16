# Unity Blob Simulation — Full Documentation

> **Engine:** Unity 2021.3 LTS or later  
> **Render Pipeline:** Works with Built-in, URP, and HDRP (materials may differ)  
> **Physics backend:** PhysX (Unity's default)

---

## Table of Contents

1. [Overview](#1-overview)  
2. [Architecture](#2-architecture)  
3. [File Reference](#3-file-reference)  
4. [Quick-Start Setup](#4-quick-start-setup)  
5. [BlobSettings Reference](#5-blobsettings-reference)  
6. [BlobParticle Reference](#6-blobparticle-reference)  
7. [BlobController Reference](#7-blobcontroller-reference)  
8. [BlobCamera Reference](#8-blobcamera-reference)  
9. [BlobPerformanceScaler Reference](#9-blobperformancescaler-reference)  
10. [Physics Design Explanation](#10-physics-design-explanation)  
11. [Performance Guide](#11-performance-guide)  
12. [Customisation & Extension](#12-customisation--extension)  
13. [Troubleshooting](#13-troubleshooting)  
14. [Upgrade Path: Burst / Jobs](#14-upgrade-path-burst--jobs)  

---

## 1. Overview

The blob is a **particle-based soft body**: instead of using a skinned mesh or
a procedural deformer, it is made of hundreds (or thousands) of small spherical
Rigidbody objects that:

- **Attract** toward a shared target point (cohesion / surface tension).
- **Repel** each other naturally through PhysX sphere–sphere collision.
- **Jiggle** independently via per-particle Perlin noise.

Player input displaces the shared target point, causing all particles to chase
it and "lean" into the direction of travel — producing convincing squash-and-stretch
without any animator involvement.

The simulation is self-contained: no external physics libraries or packages
are required beyond Unity's built-in PhysX engine.

---

## 2. Architecture

```
BlobController (MonoBehaviour)
│
├─ BlobSettings (ScriptableObject)   ← shared config, Inspector-friendly
│
├─ BlobParticle × N (MonoBehaviour)  ← per-particle physics brain
│     ├─ Rigidbody
│     └─ SphereCollider
│
├─ BlobCamera (MonoBehaviour)        ← smooth follow camera
│
└─ BlobPerformanceScaler (optional)  ← FPS-based particle culling
```

### Data flow (one fixed frame)

```
Player presses ← →
        │
        ▼
BlobController.HandlePlayerInput()
  stores rawMoveInput
        │
        ▼ (FixedUpdate)
BlobController.UpdateTargetPosition()
  displaces TargetPosition ahead of CoM
  calls ApplyForceToAll()
        │
        ▼ (FixedUpdate on each particle)
BlobParticle.ApplyCohesion()
  spring-pulls particle toward TargetPosition
        │
BlobParticle.ApplyJiggle()
  adds noise force
        │
BlobParticle.ClampSpeed()
  caps velocity
        │
        ▼
PhysX integrates Rigidbodies,
resolves sphere-sphere contacts (repulsion)
        │
        ▼
BlobController.RecalculateCenterOfMass()
  averages all particle positions → CoM
  updates controller transform
```

---

## 3. File Reference

| File | Purpose |
|------|---------|
| `BlobSettings.cs` | ScriptableObject holding every tunable parameter. |
| `BlobParticle.cs` | MonoBehaviour on every particle: cohesion, jiggle, speed cap. |
| `BlobController.cs` | Spawner, CoM tracker, player input, public force API. |
| `BlobCamera.cs` | Smooth orbit camera that follows the blob. |
| `BlobPerformanceScaler.cs` | Optional: auto-culls particles if FPS drops. |

---

## 4. Quick-Start Setup

### 4.1 Create a Physics Layer

1. Open **Edit → Project Settings → Tags and Layers**.
2. Add a new User Layer called exactly **`BlobParticle`**.
3. Open **Edit → Project Settings → Physics**.
4. In the Layer Collision Matrix, ensure `BlobParticle ↔ BlobParticle` is **checked**
   (particles need to collide with each other).
5. Optionally uncheck `BlobParticle ↔ Default` if you don't want particles
   to collide with regular scene geometry on that layer.

### 4.2 Create the Particle Prefab

1. Create an empty GameObject and add:
   - **Sphere** (or any mesh) as a child, or use `GameObject → 3D Object → Sphere`
     and remove the default collider.
   - **Rigidbody** – all settings are overridden by `BlobParticle.Initialize()`.
   - **SphereCollider** – radius is overridden by `BlobParticle.Initialize()`.
   - **BlobParticle** script.
2. Save as a Prefab in your `Assets` folder.

### 4.3 Create a BlobSettings Asset

1. Right-click in the Project window → **Create → Blob → Blob Settings**.
2. Name it `DefaultBlobSettings` (or any name you prefer).
3. Adjust parameters (see §5 for full reference).

### 4.4 Create the Blob GameObject

1. Create an empty GameObject in your scene. Name it `Blob`.
2. Add **BlobController** component.
3. Assign the `BlobSettings` asset and the Particle Prefab in the Inspector.
4. Optionally add **BlobPerformanceScaler**.

### 4.5 Set Up the Camera

1. Select your **Main Camera**.
2. Add **BlobCamera** component.
3. Assign the `Blob` GameObject's BlobController in the `Blob` field.

### 4.6 Add a Ground

Create a large **Plane** or **Terrain** so the blob has something to rest on.
Assign a PhysicsMaterial with some friction to it.

### 4.7 Press Play

The blob should spawn, settle under gravity, and respond to WASD / arrow keys
and Space (jump).

---

## 5. BlobSettings Reference

All parameters live in the `BlobSettings` ScriptableObject.

### Spawn

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `particleCount` | int | 300 | Total particles. 100–500 is interactive on most PCs; 1000–5000 requires optimisation (see §11). |
| `spawnRadius` | float | 1.2 | Radius of the initial spawn sphere in world units. |

### Particle Physics

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `particleMass` | float | 0.05 | Mass of each Rigidbody. Lower = more responsive to small forces. |
| `particleDrag` | float | 4.0 | Linear drag. Higher = more viscous, slower-moving blob. |
| `particleAngularDrag` | float | 0.5 | Rotation drag (mostly aesthetic). |
| `particleRadius` | float | 0.12 | Collision and visual radius of each sphere. |
| `particlePhysicsMaterial` | PhysicMaterial | null | Override material. Leave null to auto-generate a zero-bounce material. |

### Cohesion

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cohesionStrength` | float | 80 | Base spring force magnitude per particle. The main "stickiness" dial. |
| `cohesionMaxDistance` | float | 6.0 | Distance at which cohesion reaches full strength. |
| `surfaceTensionMultiplier` | float | 3.0 | Extra cohesion multiplier for far-stray particles. |
| `surfaceTensionThreshold` | float | 2.5 | Distance beyond which the multiplier activates. |

### Player Movement

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `moveForce` | float | 120 | Force magnitude applied to all particles on input. |
| `maxParticleSpeed` | float | 10 | Hard velocity cap per particle (m/s). |
| `jumpImpulse` | float | 6 | Upward impulse per particle on jump. |
| `targetLeadDistance` | float | 1.5 | How far ahead of CoM the target point travels. Higher = more deformation. |
| `targetReturnSpeed` | float | 5 | How quickly the target snaps back when input stops. |

### Jiggle

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `jiggleStrength` | float | 3.0 | Magnitude of Perlin noise force. Higher = slimier. |
| `jiggleFrequency` | float | 1.4 | Time-scale of the noise. Higher = faster oscillation. |

### Performance

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `centerOfMassUpdateInterval` | int | 2 | Recalculate CoM every N fixed frames. 1 = every frame (most accurate). |
| `useBurstJobs` | bool | false | Reserved for Burst upgrade path (§14). |

---

## 6. BlobParticle Reference

`BlobParticle` is a **component**; you never call it directly except through
`Initialize()` (called automatically by `BlobController`).

### Public API

```csharp
// Called once by BlobController after Instantiate:
void Initialize(BlobController controller, BlobSettings settings);

// Read-only accessor used by BlobController for impulse application:
Rigidbody Rigidbody { get; }
```

### Internal methods (private)

| Method | Description |
|--------|-------------|
| `ApplyCohesion()` | Spring force toward `BlobController.TargetPosition`. |
| `ApplyJiggle()` | Per-particle Perlin noise force. |
| `ClampSpeed()` | Hard velocity cap at `BlobSettings.maxParticleSpeed`. |

---

## 7. BlobController Reference

### Inspector Fields

| Field | Description |
|-------|-------------|
| `settings` | BlobSettings ScriptableObject asset. |
| `particlePrefab` | Prefab with BlobParticle, Rigidbody, SphereCollider. |
| `particleMaterial` | Optional shared material for all particles. |
| `horizontalAxis` | Input axis name for left/right (default: `"Horizontal"`). |
| `verticalAxis` | Input axis name for forward/back (default: `"Vertical"`). |
| `jumpButton` | Input button name for jump (default: `"Jump"`). |
| `drawTargetGizmo` | Show wire-sphere gizmos in Scene view. |

### Public Properties

```csharp
Vector3  TargetPosition  // The point particles are attracted to.
Vector3  CenterOfMass    // Averaged position of all particles.
int      ParticleCount   // Live particle count.
Vector3  Velocity        // Estimated blob CoM velocity.
```

### Public Methods

```csharp
// Apply a continuous force to every particle.
void ApplyForceToAll(Vector3 force, ForceMode mode = ForceMode.Force);

// Apply an instantaneous impulse to every particle.
void ApplyImpulseToAll(Vector3 impulse);

// Apply an impulse only to particles within a world-space radius.
void ApplyImpulseInRadius(Vector3 worldPos, float radius, Vector3 impulse);

// Returns all live particles (read-only).
IReadOnlyList<BlobParticle> GetParticles();
```

### Example: External explosion

```csharp
// Blow the blob away from an explosion point
var blobCtrl = FindObjectOfType<BlobController>();
Vector3 direction = (blobCtrl.CenterOfMass - explosionPos).normalized;
blobCtrl.ApplyImpulseInRadius(explosionPos, 5f, direction * 15f);
```

---

## 8. BlobCamera Reference

### Inspector Fields

| Field | Description |
|-------|-------------|
| `blob` | Target BlobController. |
| `followDistance` | Desired camera–blob distance. |
| `heightOffset` | Camera height above CoM. |
| `positionSmoothing` | Position lag (1 = slowest, 30 = snappiest). |
| `rotationSmoothing` | Look-at lag. |
| `velocityOrbitStrength` | [0..1] How strongly the camera drifts behind velocity. |
| `orbitSpeedThreshold` | Min blob speed before auto-orbit engages (m/s). |
| `enableManualOrbit` | Allow right-click drag / right stick orbit. |
| `mouseSensitivity` | Degrees per mouse delta unit. |
| `stickSensitivity` | Degrees per second on right stick. |
| `minVerticalAngle` | Minimum pitch (can go slightly below horizon). |
| `maxVerticalAngle` | Maximum pitch (looking down at blob). |
| `enableCollision` | SphereCast to avoid clipping through walls. |
| `collisionMask` | Layers checked for collision. |
| `minDistance` | Closest the camera can get when pushed by collision. |

---

## 9. BlobPerformanceScaler Reference

Attach to the same GameObject as `BlobController`.

| Field | Description |
|-------|-------------|
| `targetFPS` | FPS floor. Below this, particles are culled. |
| `fpsHysteresis` | Extra headroom before re-enabling particles. |
| `sampleInterval` | Seconds between FPS checks. |
| `cullBatchSize` | Max particles removed per check. |
| `minParticles` | Absolute lower bound on particle count. |
| `autoScale` | If false, only logs warnings; no destructive action. |

---

## 10. Physics Design Explanation

### Why not Spring Joints?

Unity's `SpringJoint` component is typically used for soft-body work.
For this system, connecting every particle to every other particle would require
`O(n²)` joints (45,000 joints for 300 particles), which is prohibitive.
Instead we use a single shared attraction target (CoM / player target) as a
"virtual anchor" for all particles simultaneously. This scales as `O(n)`.

### Why not OverlapSphere for repulsion?

Calling `Physics.OverlapSphere` inside `FixedUpdate` for each of N particles
also scales as `O(n²)`. Instead the simulation relies entirely on PhysX's
built-in narrow-phase collision detection between SphereColliders. PhysX uses
a broadphase (typically SAP or MBP) to make this sub-O(n²) in practice and
runs on a separate thread — effectively free compared to script-side queries.

### Cohesion as a spring

The cohesion force is proportional to the distance from the particle to the
target position (clamped to a maximum distance). This is Hooke's Law:

```
F = k × Δx
```

where `k = cohesionStrength` and `Δx` is the displacement from target.
The `Mathf.Clamp01(dist / maxDistance)` normalisation prevents particles that
are very close to the target from being over-damped.

### Surface tension

Beyond `surfaceTensionThreshold`, an extra multiplier is added. This simulates
the real physical property that surface tension grows as a film stretches —
stray particles snap back harder the further they travel, keeping the blob
cohesive even during fast movement.

### Jiggle via Perlin noise

Each particle has three independent noise offsets (X, Y, Z) seeded randomly at
spawn. Perlin noise is used instead of `Random.insideUnitSphere` because Perlin
is continuous and smooth: the resulting motion looks like organic oscillation
rather than incoherent rattling.

---

## 11. Performance Guide

### Rough particle-count guidelines

| Hardware | Comfortable count |
|----------|-------------------|
| Low-end laptop (integrated GPU, 4-core) | 100–200 |
| Mid-range gaming PC | 300–800 |
| High-end PC | 800–3000 |
| Console / high-end mobile | 200–500 |

These figures assume the default fixed-timestep (0.02 s). Halving the fixed
timestep halves the CPU budget for physics.

### Key knobs

1. **`particleCount`** — The single biggest lever. Halving it roughly halves both
   the per-frame script cost and PhysX contact work.

2. **`centerOfMassUpdateInterval`** — Set to 3–5 on large counts. The CoM
   doesn't need to be exact every frame.

3. **`particleRadius`** — Larger radii mean fewer contact pairs in PhysX's
   broadphase (particles are more spaced out). Useful for maintaining
   appearance with fewer particles.

4. **Fixed Timestep** (`Project Settings → Time → Fixed Timestep`) — Raising it
   from 0.02 to 0.033 (30 Hz physics) reduces CPU load by ~40 % at the cost of
   slightly choppier physics.

5. **Renderer LOD** — Swap the particle mesh for a lower-poly sphere (e.g. a
   6-sided UV sphere) at moderate distances. At 500+ particles the renderer cost
   is significant.

6. **Burst Jobs** — See §14.

### Profiling checklist

- Open the Profiler (`Window → Analysis → Profiler`) while in Play Mode.
- Watch the `Physics.Simulate` marker — this is PhysX time.
- Watch `BlobParticle.FixedUpdate` — this is script time.
- If PhysX dominates: reduce `particleCount` or increase `particleRadius`.
- If script dominates: try the Burst upgrade path (§14).

---

## 12. Customisation & Extension

### Change movement to top-down (mouse aim)

Replace the `HandlePlayerInput` section in `BlobController` with a raycast from
the camera to the mouse world position, then set `targetPosition` to that point.

### Make the blob squash when it lands

Subscribe to `BlobController`'s blob velocity and detect the frame when downward
velocity drops to near zero. Call `ApplyImpulseInRadius` with an outward radial
impulse from the CoM to simulate squash:

```csharp
void OnBlobLanded(BlobController blob)
{
    Vector3 coM = blob.CenterOfMass;
    var particles = blob.GetParticles();
    foreach (var p in particles)
    {
        Vector3 outward = (p.transform.position - coM).normalized;
        outward.y = 0f;
        p.Rigidbody.AddForce(outward * squashImpulse, ForceMode.Impulse);
    }
}
```

### Colour the blob by velocity

Add a script that reads `blob.Velocity.magnitude` each frame and feeds it into
`material.SetColor()` on a shared Renderer material.

### Two-player blob

Create two independent `BlobController` GameObjects, each with their own
`BlobSettings` and input axes (`"Horizontal"` vs `"Horizontal2"`, etc.).

### Metaball / marching-cubes surface

For a smooth, continuous surface instead of visible spheres:

- Import a third-party metaball or marching cubes Unity package.
- Feed `blob.GetParticles()` positions each frame to the marching cubes grid.
- Disable particle `MeshRenderer`s.

### Sound design integration

Listen to the average kinetic energy of the blob each frame and drive an audio
mixer parameter to blend between "settled" and "excited" slime sounds:

```csharp
float avgSpeed = 0f;
foreach (var p in blob.GetParticles())
    avgSpeed += p.Rigidbody.velocity.magnitude;
avgSpeed /= blob.ParticleCount;
AudioMixer.SetFloat("BlobEnergy", avgSpeed);
```

---

## 13. Troubleshooting

**Particles explode outward on Play**  
→ `cohesionStrength` is too low relative to `particleMass`. Increase cohesion,
   reduce mass, or lower `spawnRadius` so particles start closer together.

**Blob doesn't stop / slides forever**  
→ Increase `particleDrag` (try 6–10). Also ensure the ground plane has a
   PhysicsMaterial with non-zero `staticFriction`.

**Blob stretches apart when moving fast**  
→ Increase `cohesionStrength` and/or `surfaceTensionMultiplier`. Reduce
   `moveForce` or `targetLeadDistance`.

**Jump doesn't work / feels floaty**  
→ Verify `isGrounded` is detecting correctly: check `spawnRadius` matches the
   actual blob size, and that the ground layer is not excluded in `CheckGrounded`'s
   layer mask. Increase Unity gravity (`Physics.gravity`) for snappier jump arcs.

**`BlobParticle` layer warning in Console**  
→ You haven't created the `BlobParticle` physics layer. Follow step 4.1 in §4.

**Camera clips through walls**  
→ Ensure `BlobCamera.collisionMask` includes the wall layer and
   `enableCollision = true`.

**Low FPS with 300 particles**  
→ Check the Profiler. If PhysX is the bottleneck, increase `particleRadius`
   (reduces contact count) or reduce `particleCount`. If script is the
   bottleneck, try Burst (§14).

---

## 14. Upgrade Path: Burst / Jobs

For particle counts beyond ~800, moving cohesion and jiggle force calculation
to Unity's Job System with Burst compilation can give 5–15× speedup on the
script side.

### Prerequisites

Install from Package Manager:
- `com.unity.burst` (Burst Compiler)
- `com.unity.collections` (NativeArray support)
- `com.unity.jobs` (IJob / IJobParallelFor)

### Sketch of a Burst job for cohesion

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct CohesionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> positions;
    [ReadOnly] public Vector3              targetPos;
    [ReadOnly] public float                cohesionStrength;
    [ReadOnly] public float                maxDistance;
    [ReadOnly] public float                tensionThreshold;
    [ReadOnly] public float                tensionMultiplier;

    public NativeArray<Vector3> outForces;

    public void Execute(int i)
    {
        Vector3 toTarget = targetPos - positions[i];
        float   dist     = toTarget.magnitude;
        if (dist < 0.001f) { outForces[i] = Vector3.zero; return; }

        float t        = Mathf.Clamp01(dist / maxDistance);
        float strength = cohesionStrength * t;

        if (dist > tensionThreshold)
            strength += cohesionStrength
                      * ((dist - tensionThreshold) / maxDistance)
                      * tensionMultiplier;

        outForces[i] = toTarget / dist * strength;
    }
}
```

In `BlobController.FixedUpdate`, schedule this job, complete it, then apply
`outForces[i]` to each `Rigidbody.AddForce`. This moves the per-particle maths
off the main thread and into Burst-compiled SIMD code.

> **Tip:** Rigidbody.AddForce must still be called from the main thread. The job
> only calculates the force vectors; the AddForce loop remains in script but is
> much faster without the maths overhead.

---

*End of documentation.*
