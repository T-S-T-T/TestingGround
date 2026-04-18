# BubbleShield – Unity Shader Documentation

A transparent, blue neon hexagonal shield shader for Unity (Built-in Render Pipeline).  
This document explains **every technique used**, how to set it up, and how to extend it.

---

## Table of Contents

1. [Quick Setup](#1-quick-setup)
2. [How It Works – Concept Overview](#2-how-it-works--concept-overview)
3. [Technique Deep-Dives](#3-technique-deep-dives)
   - 3.1 [ShaderLab Structure](#31-shaderlab-structure)
   - 3.2 [Triplanar UV Mapping](#32-triplanar-uv-mapping)
   - 3.3 [Hexagonal SDF Tiling](#33-hexagonal-sdf-tiling)
   - 3.4 [Fresnel Rim Glow](#34-fresnel-rim-glow)
   - 3.5 [Pulse Animation](#35-pulse-animation)
   - 3.6 [Hit Ripple](#36-hit-ripple)
   - 3.7 [Two-Pass Transparency](#37-two-pass-transparency)
4. [Inspector Properties Reference](#4-inspector-properties-reference)
5. [C# Controller API](#5-c-controller-api)
6. [Customisation Ideas](#6-customisation-ideas)
7. [Glossary](#7-glossary)

---

## 1. Quick Setup

### Step 1 – Import files

Place these three files anywhere inside your `Assets/` folder:

```
Assets/
  Shaders/
    BubbleShield.shader
  Scripts/
    BubbleShieldController.cs
```

### Step 2 – Create the material

1. Right-click in the **Project** panel → **Create → Material**.
2. In the material's Inspector, set **Shader** to `Custom/BubbleShield`.
3. Adjust colours and parameters to taste.

### Step 3 – Set up the GameObject

1. Create a **Sphere** (or any convex mesh): `GameObject → 3D Object → Sphere`.
2. Make it slightly larger than the object you want to protect.
3. Drag the BubbleShield material onto the sphere.
4. Add the `BubbleShieldController` component to the sphere.
5. Check **"Is Trigger"** on the sphere's Sphere Collider if you want automatic hit detection.

> **Tip:** disable "Cast Shadows" and "Receive Shadows" on the shield's MeshRenderer  
> for a cleaner look.

---

## 2. How It Works – Concept Overview

The shader runs two **fragment (pixel) shader passes** over a sphere mesh.  
For every pixel on that sphere the GPU:

```
posOS  ─── TriplanarHex() ──► hexLine mask
normalWS ── Fresnel() ──────► rim brightness
Time ──────── Pulse() ──────► breathing scale
posWS ─────── Ripple() ─────► hit shockwave
                │
                ▼
     finalColor = base + emission * (hexLine + rim + ripple) * pulse
     finalAlpha = baseAlpha + hexLine*hexAlpha + rim + ripple
```

---

## 3. Technique Deep-Dives

### 3.1 ShaderLab Structure

Unity shaders are written in **ShaderLab**, a declarative wrapper around HLSL.

```hlsl
Shader "Custom/BubbleShield"
{
    Properties { ... }   // exposed to the Inspector / C# Material API

    SubShader
    {
        Tags { "Queue" = "Transparent" ... }  // rendering order

        CGINCLUDE                // shared HLSL (used by both passes)
            struct appdata { ... }
            struct v2f     { ... }
            float HexGrid(...)  { ... }
            v2f   vert(...)     { ... }
            float4 ShieldFragment(...) { ... }
        ENDCG

        Pass { Cull Front  ... }   // back-face pass  (inner shell)
        Pass { Cull Back   ... }   // front-face pass (outer shell)
    }
}
```

**`CGINCLUDE` / `ENDCG`** lets you write code once and share it across passes —  
keeping the shader DRY and readable.

---

### 3.2 Triplanar UV Mapping

A plain UV unwrap on a sphere has *polar distortion* — the hexagons would  
squish at the top and bottom.  

**Triplanar mapping** solves this by projecting the pattern from three orthogonal  
planes (XY, XZ, YZ) and blending them based on the surface normal:

```hlsl
// 1. Blend weights from the normal
float3 w = abs(normalOS);   // how much each axis faces the camera
w = pow(w, 8.0);            // sharpen: only the dominant axis contributes
w /= (w.x + w.y + w.z);    // normalise so weights sum to 1.0

// 2. Sample the hex pattern on each plane using the position as UV
float2 hx = HexGrid(posOS.yz);   // YZ plane
float2 hy = HexGrid(posOS.xz);   // XZ plane
float2 hz = HexGrid(posOS.xy);   // XY plane

// 3. Weighted blend
float edge = hx.x * w.x + hy.x * w.y + hz.x * w.z;
```

**Object-space** coordinates are used (not world-space) so the pattern stays  
fixed relative to the shield mesh regardless of where it moves.

---

### 3.3 Hexagonal SDF Tiling

A regular hexagon grid is created using the **two-interleaved-grids** trick.

#### Why two grids?

A plain `fmod(uv, period)` tiles a rectangle.  
Hexagons need each row shifted by half a column.  
Two rectangular grids, offset by `(period/2, period/2)`, together tile all of hex space:

```hlsl
const float2 r = float2(1.732, 3.0);  // period: (√3, 3)
const float2 h = r * 0.5;             // half-period

float2 a = fmod(uv,     r) - h;   // grid A: position relative to nearest A-centre
float2 b = fmod(uv + h, r) - h;   // grid B: position relative to nearest B-centre

// Voronoi: pick whichever centre is closer
float2 gv = dot(a, a) < dot(b, b) ? a : b;
```

`gv` is now the **local coordinate inside the hex** — `(0,0)` at the centre,  
reaching outward to the edges at distance ≈1.

#### Hexagon SDF (Signed Distance Field)

For a *pointy-top* regular hexagon with circumradius 1:

```hlsl
float2 agv = abs(gv);                             // fold to one sextant
float d = max(agv.y,                              // vertical band
              agv.x * 0.866 + agv.y * 0.5);      // 60° rotated band
// d = 0 at centre, d = 1 at edge
```

This is an *exact* hex metric — **not an approximation** — derived from the  
three pairs of parallel lines that define a regular hexagon.

#### Creating the visible line

```hlsl
float hexLine = smoothstep(
    1.0 - _LineWidth - epsilon,
    1.0 - _LineWidth,
    edgeFactor
);
```

`smoothstep` creates an anti-aliased ramp:  
- `0` → deep inside the hex (transparent)  
- `1` → exactly on the edge (fully lit)

Increasing `_LineWidth` makes the lines thicker.  
Increasing `_LineSharpness` makes the transition harder / more neon.

---

### 3.4 Fresnel Rim Glow

The **Fresnel effect** makes surfaces brighter at glancing angles —  
exactly how energy shields look in sci-fi films.

```hlsl
float NdotV   = saturate(dot(normalize(viewDir), normalize(normal)));
float fresnel = pow(1.0 - NdotV, _FresnelPower);
```

| `NdotV` | Angle     | Result                  |
|---------|-----------|-------------------------|
| 1.0     | head-on   | `1 - 1 = 0` → dim       |
| 0.0     | edge-on   | `1 - 0 = 1` → bright    |

`_FresnelPower` controls how abruptly it falls off:
- Low value (1–2): soft, diffuse glow
- High value (5–8): sharp, thin rim only

---

### 3.5 Pulse Animation

```hlsl
float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
pulse = lerp(1.0 - _PulseIntensity, 1.0, pulse);
```

`_Time.y` is Unity's built-in uniform — the time since startup in seconds.  
- `sin(...)` oscillates between −1 and +1.  
- `* 0.5 + 0.5` maps it to `[0, 1]`.  
- `lerp` maps it to `[1 - intensity, 1]` so the shield never goes completely dark.

The resulting scalar multiplies the entire emission, giving a breathing effect.

---

### 3.6 Hit Ripple

When `BubbleShieldController.RegisterHit()` is called, it writes:
- `_HitPointWS` – the world-space contact position  
- `_HitTime` – the current `Time.time`

The shader then computes:

```hlsl
float hitAge  = _Time.y - _HitTime;               // seconds since impact
float hitDist = length(posWS - _HitPointWS);      // metres from impact

// Sine wave expanding outward from the hit point
float wave    = sin(hitDist * 6.0 - hitAge * _RippleSpeed);

// Envelope: fade with age AND distance
float env     = exp(-hitAge * _RippleDecay)        // time decay
              * exp(-hitDist * 1.5);               // spatial decay

float ripple  = max(0.0, wave) * env * _RippleIntensity;
```

The `max(0, wave)` keeps only the positive half of the sine — creating  
distinct glowing rings rather than alternating light/dark bands.

---

### 3.7 Two-Pass Transparency

Transparent objects need special handling to look correct.  
A single-pass `Cull Off` would draw back and front faces in arbitrary order,  
causing Z-sorting artefacts.

Two passes solve this by guaranteeing correct render order:

| Pass | `Cull`  | What it draws     | Why              |
|------|---------|-------------------|------------------|
| 1    | `Front` | Back faces first  | Inner shell, darker |
| 2    | `Back`  | Front faces last  | Outer shell, full brightness |

Both passes use:
```hlsl
ZWrite Off                          // don't write to the depth buffer
Blend SrcAlpha OneMinusSrcAlpha    // standard alpha blending
```

`ZWrite Off` prevents the transparent shield from occluding objects behind it.

---

## 4. Inspector Properties Reference

### Hex Grid

| Property        | Default | Description |
|----------------|---------|-------------|
| Hex Scale       | 12      | Number of hexagons across the object. Higher = smaller hexagons. |
| Line Width      | 0.07    | Thickness of hex edges (0 = no lines, 0.5 = all lines). |
| Line Sharpness  | 30      | Hard vs. soft edge transition. |

### Color and Emission

| Property            | Default          | Description |
|---------------------|------------------|-------------|
| Shield Color        | (0.05, 0.4, 1, 1)| Base tint of the whole shield. |
| Emission Color      | (0, 0.7, 1, 1)   | Colour of the glowing lines and rim. |
| Emission Intensity  | 2.5              | How bright the glow is. Values >1 are HDR — use with Bloom post-processing. |

### Transparency and Fresnel

| Property          | Default | Description |
|-------------------|---------|-------------|
| Base Alpha        | 0.10    | Opacity of the hex interior (background fill). |
| Hex Line Alpha    | 0.85    | Opacity of the hex edges. |
| Fresnel Power     | 3.0     | How quickly the rim glow falls off toward the centre. |
| Fresnel Intensity | 1.0     | Brightness multiplier for the rim. |

### Pulse Animation

| Property        | Default | Description |
|----------------|---------|-------------|
| Pulse Speed     | 1.5     | Oscillations per second. |
| Pulse Intensity | 0.25    | How much the brightness varies (0 = no pulse). |

### Hit Ripple

| Property          | Default | Description |
|-------------------|---------|-------------|
| Ripple Speed      | 8.0     | Outward travel speed of the ring (higher = faster). |
| Ripple Intensity  | 1.2     | Brightness of the expanding ring. |
| Ripple Decay      | 3.0     | How quickly the ripple fades after impact (higher = faster). |

---

## 5. C# Controller API

```csharp
// Attach to the shield GameObject
BubbleShieldController shield = GetComponent<BubbleShieldController>();

// Trigger a hit ripple at a world-space position
shield.RegisterHit(contactPoint);

// Trigger ripple AND red damage flash
shield.TakeDamage(contactPoint, damageAmount);

// Change hex density at runtime
shield.SetHexScale(20f);

// Tint the shield green
shield.SetShieldColor(Color.green);
```

### OnTriggerEnter (automatic)

If you enable **Is Trigger** on the shield's collider, `BubbleShieldController`  
automatically calls `RegisterHit()` with the closest surface point whenever  
another collider enters it.

---

## 6. Customisation Ideas

### Fire / Lava shield
```
Shield Color    → (1.0, 0.3, 0.0)
Emission Color  → (1.0, 0.6, 0.0)
Pulse Speed     → 3.0
```

### EMP / Electricity shield
```
Shield Color    → (0.8, 0.9, 1.0)
Emission Color  → (1.0, 1.0, 1.0)
Line Width      → 0.03   ← thin crackling lines
Fresnel Power   → 6.0    ← very sharp rim
Pulse Speed     → 4.0
Pulse Intensity → 0.6    ← strong flicker
```

### Making hexagons glow individually

Add a `_Time.y`-driven hash to `cellRand` in `HexGrid()` to make individual  
cells flicker asynchronously — great for a flickering / damaged shield look.

### URP / HDRP port

Replace `#include "UnityCG.cginc"` with `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"`,  
rename vertex/fragment stage tags, and swap `_Time.y` for `_TimeParameters.x`.

---

## 7. Glossary

| Term | Meaning |
|------|---------|
| **SDF** | Signed Distance Field — a function that returns how far a point is from a shape's surface (negative = inside). |
| **Fresnel** | The physical phenomenon where reflectivity increases at glancing angles. |
| **Triplanar mapping** | Projecting a texture from three axis-aligned planes and blending by surface normal to avoid UV seams. |
| **CGINCLUDE** | ShaderLab block that lets HLSL code be shared across multiple passes in the same SubShader. |
| **Blend SrcAlpha OneMinusSrcAlpha** | Standard alpha blending: `output = src.rgb * src.a + dst.rgb * (1 - src.a)`. |
| **ZWrite Off** | Prevents the transparent object from writing to the depth buffer, avoiding occlusion artefacts. |
| **HDR emission** | Emission values greater than 1 that, combined with a Bloom post-process effect, create a real glow. |
| **Property ID** | Integer handle returned by `Shader.PropertyToID()` — faster than using string names in `Material.SetFloat()` etc. |
