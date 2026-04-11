# Tentacle IK System — Unity 3D

A procedural **FABRIK-based Inverse Kinematics** tentacle system with two fully prefab-ready
variants: a simple single-target version and a Bézier-curve-influenced version.

---

## Table of Contents

1. [Overview](#overview)
2. [File Structure](#file-structure)
3. [Version 1 — TentacleIK](#version-1--tentacleik)
   - [Component Reference](#v1-component-reference)
   - [Setup Guide](#v1-setup-guide)
   - [How FABRIK Works](#how-fabrik-works)
4. [Version 2 — TentacleBezierIK](#version-2--tentaclebebezierik)
   - [Component Reference](#v2-component-reference)
   - [Setup Guide](#v2-setup-guide)
   - [How Bézier Influence Works](#how-bezier-influence-works)
5. [Prefab Creation](#prefab-creation)
6. [Editor Gizmos](#editor-gizmos)
7. [Performance Notes](#performance-notes)
8. [Extending the System](#extending-the-system)
9. [FAQ](#faq)

---

## Overview

| Feature                        | TentacleIK (V1) | TentacleBezierIK (V2) |
|-------------------------------|:--------------:|:--------------------:|
| FABRIK solver                  | ✅              | ✅                    |
| Configurable segment count     | ✅              | ✅                    |
| Configurable segment length    | ✅              | ✅                    |
| Single target tracking         | ✅              | ✅                    |
| Bézier curve shaping           | ❌              | ✅                    |
| Arbitrary control points       | ❌              | ✅                    |
| Editor Bézier curve preview    | ❌              | ✅                    |
| LineRenderer visualisation     | ✅              | ✅                    |
| `ExecuteAlways` (edit-mode)    | ✅              | ✅                    |

Both components require a `LineRenderer` on the same GameObject (added automatically
if missing) and work entirely in **world space**.

---

## File Structure

```
Assets/
└── Tentacle/
    ├── Scripts/
    │   ├── TentacleIK.cs           ← Version 1 (base class)
    │   └── TentacleBezierIK.cs     ← Version 2 (extends V1)
    └── Editor/
        ├── TentacleIKEditor.cs     ← Inspector + Scene gizmos for V1
        └── TentacleBezierIKEditor.cs ← Inspector + Bézier gizmos for V2
```

> **Note:** The `Editor/` folder must remain inside `Assets/`. Unity compiles
> everything inside `Editor/` as editor-only code, keeping your builds clean.

---

## Version 1 — TentacleIK

### V1 Component Reference

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| **Segment Count** | `int` (≥ 2) | `8` | Number of bone segments. The chain has `segmentCount + 1` joints. |
| **Segment Length** | `float` (> 0) | `0.5` | World-space length of each segment. |
| **Target** | `Transform` | — | The transform the tentacle tip tries to reach each frame. |
| **Max Iterations** | `int` [1–30] | `10` | FABRIK iterations per frame. More iterations = more accuracy at a small CPU cost. |
| **Tolerance** | `float` | `0.001` | Early-exit threshold (metres). Solving stops once the tip is within this distance of the target. |
| **Base Width** | `float` | `0.15` | `LineRenderer` width at the root joint. |
| **Tip Width** | `float` | `0.02` | `LineRenderer` width at the tip joint. |

#### Context Menu Actions

Right-click the component header in the Inspector to access:

| Action | Effect |
|--------|--------|
| **Generate Bones** | Destroys existing `Bone_N` children and rebuilds the full chain using current `segmentCount` and `segmentLength`. Also configures the `LineRenderer`. |

#### Public API

```csharp
tentacle.SegmentCount    // int   — number of segments
tentacle.SegmentLength   // float — length per segment
tentacle.Target          // Transform — the IK target
tentacle.Bones           // Transform[] — bone chain
tentacle.Positions       // Vector3[] — current world positions (solved each frame)
tentacle.GenerateBones() // Rebuilds the bone hierarchy (also callable from code)
```

---

### V1 Setup Guide

**Step-by-step:**

1. **Create root object**
   ```
   Hierarchy → right-click → Create Empty → name it "Tentacle"
   ```

2. **Attach the component**
   ```
   Add Component → TentacleIK
   ```
   A `LineRenderer` is added automatically.

3. **Configure the chain**
   - Set `Segment Count` (e.g. `8`)
   - Set `Segment Length` (e.g. `0.5`)

4. **Generate bones**
   ```
   Inspector context menu → Generate Bones
   ```
   `Bone_0` through `Bone_8` will appear as children in the hierarchy.

5. **Create a target**
   ```
   Create Empty → name it "TentacleTarget"
   ```
   Assign it to the `Target` field.

6. **Test in Play Mode (or Edit Mode)**
   Move `TentacleTarget` around. The tentacle follows in real time.

7. **Save as Prefab**
   Drag the `Tentacle` root into a `Prefabs/` folder.
   The target object can live outside the prefab (or be a child of it — your choice).

---

### How FABRIK Works

**FABRIK** (Forward And Backward Reaching Inverse Kinematics) is an iterative,
position-based algorithm that is fast, stable, and constraint-free in its basic form.

Each frame, the algorithm runs up to `maxIterations` passes:

```
Given:
  positions[0]     = root anchor (fixed)
  positions[n-1]   = target position
  segmentLength    = fixed bone length

Special case — target out of reach:
  Stretch all joints in a straight line toward the target.

Otherwise, iterate:

  ── Forward pass (tip → root) ──────────────────────
  positions[n-1] = targetPosition         // snap tip to target
  for i = n-2 down to 0:
    dir           = normalize(positions[i] - positions[i+1])
    positions[i]  = positions[i+1] + dir * segmentLength

  ── Backward pass (root → tip) ─────────────────────
  positions[0]   = rootAnchor             // re-anchor root
  for i = 1 to n-1:
    dir           = normalize(positions[i] - positions[i-1])
    positions[i]  = positions[i-1] + dir * segmentLength

  ── Convergence check ──────────────────────────────
  if distance(positions[n-1], target) < tolerance: break
```

After FABRIK, each bone's `Transform.rotation` is updated so the bone
points toward the next joint (`Quaternion.LookRotation`).

---

## Version 2 — TentacleBezierIK

`TentacleBezierIK` **extends** `TentacleIK`. All V1 fields and behaviours apply.
The Bézier influence is computed in the `PostSolve()` hook that runs after FABRIK.

### V2 Component Reference

All V1 fields, plus:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| **Control Points** | `Transform[]` | `[1 element]` | Ordered list of transforms used as intermediate Bézier control points. Null entries are skipped. |
| **Bezier Influence** | `float` [0–1] | `0.6` | Blending weight between pure FABRIK (`0`) and full Bézier shape (`1`). The tip is always pinned to the target. |

---

### V2 Setup Guide

Follow all V1 steps, then:

5. **Create control point objects**
   ```
   Create Empty → name it "CP_0"   (repeat for more points)
   ```

6. **Assign control points**
   Expand the `Control Points` array in the Inspector.
   Set `Size` to the number of control points you want.
   Drag each `CP_N` into the corresponding slot.

7. **Position control points**
   Drag the `CP_N` objects anywhere in the scene. The Bézier preview
   (magenta curve) updates in real time in the Scene view.

8. **Tune Bezier Influence**
   - `0.0` — pure FABRIK, control points have no effect
   - `0.5` — halfway blend (good default for organic motion)
   - `1.0` — joints fully follow the Bézier curve shape

9. **Save as Prefab** (same as V1)

#### Recommended control point counts

| Look | Control points |
|------|---------------|
| Gentle S-curve | 1 |
| Double bend | 2 |
| Coiling tentacle | 3–4 |
| Complex spiral | 5+ |

---

### How Bézier Influence Works

After FABRIK produces a position array, the Bézier step runs:

```
Step 1 — Build curve control polygon:
  curvePts = [ root, CP_0, CP_1, ..., CP_N, target ]

Step 2 — For each interior joint i in [1, n-2]:
  t           = i / (n - 1)            // uniform parameterisation
  bezierPos   = DeCasteljau(curvePts, t)
  positions[i] = Lerp(positions[i], bezierPos, bezierInfluence)

Step 3 — Pin tip:
  positions[n-1] = target.position     // always exact
```

**De Casteljau algorithm** (generalised, any degree):

```
temp = copy of curvePts
for r = 1 to len(curvePts) - 1:
  for i = 0 to len(curvePts) - r - 1:
    temp[i] = Lerp(temp[i], temp[i+1], t)
return temp[0]
```

This gives a smooth, degree-`(numControlPoints + 1)` Bézier curve.
Because the tip is always pinned to the target, the tentacle **always reaches
its destination** regardless of influence value.

#### Public API (V2 extras)

```csharp
// Sample the Bézier curve at t ∈ [0,1] — useful for tools/debug
Vector3 point = tentacle.SampleCurve(0.5f);

tentacle.ControlPoints    // Transform[] — the control point array
tentacle.BezierInfluence  // float [0,1]
```

---

## Prefab Creation

Both tentacles are designed to be self-contained prefabs.

**Recommended prefab structure:**

```
Tentacle (root — TentacleIK or TentacleBezierIK, LineRenderer)
├── Bone_0
├── Bone_1
│    ...
└── Bone_N
```

The `Target` and optional `Control Points` are **intentionally separate** scene
objects so they can be moved at runtime without breaking the prefab.
You may nest them under the prefab root or leave them free — both work fine.

**Instantiation from code:**

```csharp
var go       = Instantiate(tentaclePrefab, spawnPos, Quaternion.identity);
var tentacle = go.GetComponent<TentacleIK>();

// Assign the target at runtime
tentacle.GetType()
        .GetField("target", System.Reflection.BindingFlags.NonPublic |
                             System.Reflection.BindingFlags.Instance)
        .SetValue(tentacle, myTargetTransform);

// Or expose a public setter by adding to TentacleIK.cs:
// public void SetTarget(Transform t) => target = t;
```

---

## Editor Gizmos

### TentacleIKEditor (V1)

| Gizmo | Colour | Meaning |
|-------|--------|---------|
| White sphere | White | Root joint |
| Yellow spheres | Yellow | Interior joints |
| Orange/red sphere | Orange-red | Tip joint |
| Green lines | Green | Bone segments |
| Dashed blue line | Cyan | Vector from tip to target |
| Faint white disc | White (8 % alpha) | Maximum reach radius |

### TentacleBezierIKEditor (V2)

All V1 gizmos, plus:

| Gizmo | Colour | Meaning |
|-------|--------|---------|
| Magenta polyline | Magenta | Bézier curve preview (48 samples) |
| Orange dashes | Orange | Control polygon (root → CPs → target) |
| Orange spheres + labels | Orange | Control point positions (CP 0, CP 1, …) |
| Cone on curve | Magenta | Direction arrow at curve midpoint |

Gizmos are only visible in the Scene view and are stripped from builds.

---

## Performance Notes

| Concern | Detail |
|---------|--------|
| **Iterations** | Each FABRIK iteration is O(n) in joint count. The default of `10` iterations with `8` joints is negligible. Reduce `maxIterations` to `4–6` for many simultaneous tentacles. |
| **Width curve** | The `AnimationCurve` for `LineRenderer` is only rebuilt when `baseWidth` or `tipWidth` changes — not every frame. |
| **Bezier step** | De Casteljau is O(d²) in curve degree `d`. With 1–3 control points this is trivial. |
| **`ExecuteAlways`** | Both components update in edit mode. Disable `ExecuteAlways` in the scripts if you don't need live Scene-view preview and want editor performance back. |
| **Many tentacles** | For 50+ tentacles consider moving the solve to a `Job` using Unity's Burst compiler. The position arrays are already plain `Vector3[]` — straightforward to port. |

---

## Extending the System

### Adding angular constraints

Override `PostSolve()` in a subclass and clamp each joint angle
after FABRIK:

```csharp
protected override void PostSolve()
{
    for (int i = 1; i < positions.Length - 1; i++)
    {
        Vector3 inDir  = (positions[i]   - positions[i - 1]).normalized;
        Vector3 outDir = (positions[i+1] - positions[i]  ).normalized;
        float   angle  = Vector3.Angle(inDir, outDir);
        if (angle > maxBendAngle)
        {
            // Clamp outDir to maxBendAngle around inDir
            outDir = Vector3.RotateTowards(inDir, outDir,
                         maxBendAngle * Mathf.Deg2Rad, 0f);
            positions[i + 1] = positions[i] + outDir * SegmentLength;
        }
    }
}
```

### Smooth damping (spring follow)

Replace `SolveFABRIK(target.position)` with a smoothed target:

```csharp
private Vector3 _smoothTarget;

protected override void LateUpdate()
{
    _smoothTarget = Vector3.SmoothDamp(
        _smoothTarget, target.position, ref _vel, smoothTime);
    // ... rest of pipeline using _smoothTarget
}
```

### Procedural waving (no target needed)

Drive `target.position` from a sine wave in your own controller:

```csharp
void Update()
{
    float x = Mathf.Sin(Time.time * frequency) * amplitude;
    waveTarget.localPosition = new Vector3(x, 0, reach);
}
```

---

## FAQ

**Q: The tentacle stretches in a straight line instead of curving.**
A: The target is outside the chain's total reach (`segmentCount × segmentLength`).
Move the target closer, or increase the total reach.

**Q: Can I animate the segment count or length at runtime?**
A: Changing these at runtime requires calling `GenerateBones()` again, which
destroys and rebuilds the child hierarchy. Plan your configuration upfront
and treat it as a prefab parameter rather than a runtime variable.

**Q: The tentacle jitters at high Bezier Influence.**
A: This happens when FABRIK and the Bézier shape conflict strongly. Try
reducing `maxIterations` to `4–6`, or reducing `bezierInfluence` to `0.4–0.7`.
You can also add a `Vector3.SmoothDamp` on the final positions array.

**Q: Can I use this with a 2D project?**
A: Yes — the math is the same. Lock the Z-axis by zeroing Z on positions after
each FABRIK pass, and set the `LineRenderer` to use the `Sprites/Default`
material (already the default).

**Q: How do I make the tentacle thicker at the base and taper to a point?**
A: Set `Base Width` to your desired thickness and `Tip Width` to a small value
(e.g. `0.01`). The `LineRenderer` width curve interpolates linearly between them.

**Q: The control points don't seem to do anything.**
A: Check that `Bezier Influence` is greater than `0` and that the control
points are assigned (not null) in the Inspector. The magenta Bézier preview
curve in the Scene view shows exactly where joints will be pulled to.
