# Spider Procedural Animation — Unity 3D
## Complete Setup Guide

---

## Files in this package

| File | Purpose |
|---|---|
| `SpiderController.cs` | Player input, Rigidbody movement, coordinates subsystems |
| `LegStepController.cs` | Per-leg step logic — raycasts, arc animation, partner check |
| `FABRIKSolver.cs` | IK solver — bends the bone chain toward the foot target |
| `SpiderBodyAdjustment.cs` | Body height, terrain tilt, breathing bob |
| `SpiderCamera.cs` | Smooth third-person follow camera |
| `SpiderSetupHelper.cs` | Editor utility: auto-creates 8 rest position GameObjects |

---

## Required Unity version & packages

- Unity **2021.3 LTS** or newer (uses `rb.linearVelocity` — rename to `rb.velocity` if on 2020 or earlier)
- No extra packages required. The IK is hand-coded. If you later want Unity's built-in rigs, you can swap `FABRIKSolver` for a `TwoBoneIKConstraint` from the **Animation Rigging** package.

---

## Step 1 — Import scripts

1. In your Unity project, right-click the `Assets` folder → **Create > Folder** → name it `SpiderAnimation`.
2. Drag all 6 `.cs` files into that folder.
3. Wait for Unity to compile (no errors expected).

---

## Step 2 — Build the Hierarchy

Your spider needs this exact object structure. Create it in the **Hierarchy** panel:

```
Spider                          ← root. Has: SpiderController, SpiderBodyAdjustment, Rigidbody
│
├── Body                        ← visual mesh (your spider model or placeholder capsule)
│
├── RestPositions               ← auto-created by the menu tool (Step 3)
│   ├── RestPos_FL
│   ├── RestPos_FR
│   ├── RestPos_ML
│   ├── RestPos_MR
│   ├── RestPos_RL
│   ├── RestPos_RR
│   ├── RestPos_BL
│   └── RestPos_BR
│
├── Leg_FL                      ← one per leg. Has: LegStepController, FABRIKSolver
│   ├── Coxa_FL                 ← bone 0 (hip)
│   │   └── Femur_FL            ← bone 1 (upper leg)
│   │       └── Tibia_FL        ← bone 2 (lower leg)
│   │           └── Foot_FL     ← bone 3 (tip — optional 4th bone)
│   └── PoleTarget_FL           ← empty GO, position it to the side of the knee
│
├── Leg_FR  (same structure)
├── Leg_ML  (same structure)
├── Leg_MR  (same structure)
├── Leg_RL  (same structure)
├── Leg_RR  (same structure)
├── Leg_BL  (same structure)
└── Leg_BR  (same structure)
```

**Quick way to build legs:** Duplicate Leg_FL seven times, rename each, then move/rotate their root positions to match a spider's leg attachment points on the body.

---

## Step 3 — Auto-create Rest Positions

1. Select the **Spider** root in the Hierarchy.
2. In the menu bar: **Tools > Spider > Auto-Create Rest Positions**
3. This creates `RestPositions/RestPos_FL` … `RestPos_BR` with sensible default offsets.
4. Select each `RestPos_XX` and nudge its **local position** in the Scene view until each point sits roughly where you want that foot to rest on flat ground (about 0.3 units below and 0.5–0.7 units to the side of the body centre).

---

## Step 4 — Rigidbody setup

Select the **Spider** root and add a **Rigidbody** component:

| Setting | Value |
|---|---|
| Mass | 1 |
| Drag | 5 |
| Angular Drag | 10 |
| Use Gravity | ✅ |
| Is Kinematic | ❌ |
| Freeze Rotation | X ✅  Y ❌  Z ✅ |
| Interpolate | Interpolate |
| Collision Detection | Continuous |

Add a **Capsule Collider** (or Sphere Collider) to the Spider root — size it to roughly cover the body. The legs themselves do not need colliders.

---

## Step 5 — Configure SpiderController (Inspector)

Select **Spider** root → find `SpiderController` in the Inspector:

| Field | What to assign |
|---|---|
| Move Speed | 4 |
| Rotate Speed | 120 |
| Ground Check Distance | 0.4 |
| Ground Layer | Create a layer called `Ground`, assign it to your terrain/floor |
| Body Transform | Drag the `Body` child here |
| Legs (size 8) | Drag all 8 `Leg_XX` GameObjects (which have `LegStepController`) |
| Body Adjust | Drag the `SpiderBodyAdjustment` component (it's on Spider root) |

---

## Step 6 — Configure SpiderBodyAdjustment (Inspector)

Still on the **Spider** root, find `SpiderBodyAdjustment`:

| Field | What to assign |
|---|---|
| Legs (size 8) | Drag all 8 `LegStepController` components — **order matters**: FL, FR, ML, MR, RL, RR, BL, BR |
| Ride Height | 0.55 |
| Height Smooth Speed | 6 |
| Tilt Smooth Speed | 4 |
| Breathe Amount | 0.018 |
| Walk Bob Amount | 0.025 |

---

## Step 7 — Configure each LegStepController

Select **Leg_FL** (the GameObject, not a bone). In `LegStepController`:

| Field | What to assign |
|---|---|
| Rest Position | `RestPos_FL` |
| Step Threshold | 0.35 |
| Step Duration | 0.12 |
| Step Height | 0.12 |
| Step Overshoot | 0.1 |
| Raycast Origin Height | 1.0 |
| Raycast Distance | 2.5 |
| Ground Layer | `Ground` |
| Partner Leg | Drag the **diagonal** partner (see table below) |

**Diagonal partner pairs — these legs won't step at the same time:**

| Leg | Partner |
|---|---|
| FL (front-left) | RR (rear-right) |
| FR (front-right) | RL (rear-left) |
| ML (mid-left) | MR (mid-right) |
| MR (mid-right) | ML (mid-left) |
| RL (rear-left) | FR (front-right) |
| RR (rear-right) | FL (front-left) |
| BL (back-left) | BR (back-right) |
| BR (back-right) | BL (back-left) |

Repeat for all 8 leg GameObjects.

---

## Step 8 — Configure each FABRIKSolver

Select **Leg_FL**. In `FABRIKSolver`:

| Field | What to assign |
|---|---|
| Bones (size 3 or 4) | Drag: `Coxa_FL`, `Femur_FL`, `Tibia_FL` (and `Foot_FL` if you have it) |
| Leg Step | Drag the `LegStepController` on this same Leg_FL object |
| Iterations | 10 |
| Tolerance | 0.001 |
| Pole Target | Drag `PoleTarget_FL` (position it to the outside of the knee joint) |
| Pole Weight | 0.3 |

Repeat for all 8 legs.

---

## Step 9 — Camera setup

1. Select **Main Camera** in the Hierarchy.
2. Add `SpiderCamera` component.
3. Assign `Target` → **Spider** root.
4. Default offset `(0, 2.5, -5)` gives a good third-person view. Adjust to taste.

---

## Step 10 — Ground layer

1. In **Edit > Project Settings > Tags and Layers**, create a layer called `Ground`.
2. Select your Terrain (or floor Plane/Mesh) → set its Layer to `Ground`.
3. Every `LegStepController` and `SpiderController` should have `Ground Layer` set to this layer mask.

---

## Step 11 — Test it

Press **Play**. Use **WASD / Arrow Keys** to move. You should see:

- Legs lifting and planting as the spider moves ✅
- Feet sticking to uneven terrain via raycast ✅
- Diagonal legs never stepping at the same time ✅
- Body rising/dipping as terrain height changes ✅
- Body tilting on slopes ✅
- Gentle breathing bob at idle ✅

---

## Tuning cheat sheet

| I want... | Tweak |
|---|---|
| Legs step more often | Decrease `stepThreshold` (e.g. 0.2) |
| Legs step less often | Increase `stepThreshold` (e.g. 0.5) |
| Snappier steps | Decrease `stepDuration` (e.g. 0.08) |
| More floaty steps | Increase `stepDuration` + `stepHeight` |
| Body follows terrain faster | Increase `heightSmoothSpeed` and `tiltSmoothSpeed` |
| Less wobbly on slopes | Decrease `tiltSmoothSpeed` |
| More dramatic breathing | Increase `breatheAmount` |
| IK looks twitchy | Decrease `Pole Weight` or move PoleTargets further from the leg |
| Legs clip through ground | Reduce `stepHeight`, ensure `Ground Layer` is correct |

---

## Common issues

**Legs are straight / not bending**
→ Check that `Bones` array is ordered root→tip (Coxa → Femur → Tibia).
→ Make sure `boneLengths` sums to more than the distance from root to foot rest position (chain isn't long enough to reach).

**Feet never step**
→ Confirm `Ground Layer` mask is set on both `LegStepController` and the terrain.
→ Check that `restPosition` is assigned on each `LegStepController`.

**Body spins wildly on slopes**
→ Lower `tiltSmoothSpeed` to 2–3. Also ensure your leg order in `SpiderBodyAdjustment.legs[]` is FL, FR, ML, MR, RL, RR, BL, BR.

**Spider slides when not pressing keys**
→ Increase Rigidbody `Drag` to 8–10.

**All legs step at once**
→ Double-check each `LegStepController.partnerLeg` is assigned to a *different* LegStepController, not itself.
