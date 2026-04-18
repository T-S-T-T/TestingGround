// ============================================================
//  BubbleShield.shader
//  A transparent hexagonal neon shield effect for Unity.
//
//  Techniques used:
//    - Triplanar UV mapping  (no seams on any mesh)
//    - Hexagonal SDF tiling  (smooth edge lines)
//    - Fresnel rim lighting  (glowing shield edge)
//    - Animated pulse        (breathing / energy feel)
//    - Hit ripple            (driven from C# on impact)
// ============================================================
Shader "Custom/BubbleShield"
{
    Properties
    {
        [Header(Hex Grid)]
        _HexScale           ("Hex Scale",           Range(2,  40))  = 12.0
        _LineWidth          ("Line Width",           Range(0.01, 0.5)) = 0.07
        _LineSharpness      ("Line Sharpness",       Range(5,  80))  = 30.0

        [Header(Color and Emission)]
        _ShieldColor        ("Shield Color",         Color)  = (0.05, 0.4, 1.0, 1.0)
        _EmissionColor      ("Emission Color",       Color)  = (0.0,  0.7, 1.0, 1.0)
        _EmissionIntensity  ("Emission Intensity",   Range(0, 8)) = 2.5

        [Header(Transparency and Fresnel)]
        _BaseAlpha          ("Base Alpha",           Range(0, 1)) = 0.10
        _HexAlpha           ("Hex Line Alpha",       Range(0, 1)) = 0.85
        _FresnelPower       ("Fresnel Power",        Range(0.5, 8)) = 3.0
        _FresnelIntensity   ("Fresnel Intensity",    Range(0, 2)) = 1.0

        [Header(Pulse Animation)]
        _PulseSpeed         ("Pulse Speed",          Range(0, 6)) = 1.5
        _PulseIntensity     ("Pulse Intensity",      Range(0, 1)) = 0.25

        [Header(Hit Ripple)]
        // Set these from BubbleShieldController.cs
        _HitPointWS         ("Hit Point (World)",    Vector) = (0, 0, 0, 0)
        _HitTime            ("Hit Time",             Float)  = -100.0
        _RippleSpeed        ("Ripple Speed",         Range(1, 20)) = 8.0
        _RippleIntensity    ("Ripple Intensity",     Range(0, 3))  = 1.2
        _RippleDecay        ("Ripple Decay",         Range(1, 10)) = 3.0
    }

    SubShader
    {
        // ----------------------------------------------------------
        //  Render queue: Transparent
        //  Two passes so you see hex lines on both inner and outer
        //  faces of the sphere without Z-fighting.
        // ----------------------------------------------------------
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        // Shared HLSL – included by both passes via CGINCLUDE
        CGINCLUDE
        #include "UnityCG.cginc"

        // ---- Property declarations --------------------------------
        float4 _ShieldColor;
        float4 _EmissionColor;
        float  _EmissionIntensity;

        float  _HexScale;
        float  _LineWidth;
        float  _LineSharpness;

        float  _BaseAlpha;
        float  _HexAlpha;
        float  _FresnelPower;
        float  _FresnelIntensity;

        float  _PulseSpeed;
        float  _PulseIntensity;

        float4 _HitPointWS;
        float  _HitTime;
        float  _RippleSpeed;
        float  _RippleIntensity;
        float  _RippleDecay;

        // ---- Vertex data -----------------------------------------
        struct appdata
        {
            float4 vertex   : POSITION;
            float3 normal   : NORMAL;
        };

        struct v2f
        {
            float4 clipPos  : SV_POSITION;
            float3 normalWS : TEXCOORD0;   // world-space normal
            float3 normalOS : TEXCOORD1;   // object-space normal (triplanar)
            float3 posOS    : TEXCOORD2;   // object-space position (triplanar)
            float3 posWS    : TEXCOORD3;   // world-space position (ripple)
            float3 viewDir  : TEXCOORD4;   // world-space view direction
        };

        // ===========================================================
        //  HEX GRID HELPER
        //  Returns: x = edge factor  (0 = hex centre, 1 = hex edge)
        //           y = random value per cell  (0..1)
        //
        //  Pointy-top hexagon layout with circumradius = 1.
        //  Spacing: horizontal = sqrt(3), vertical period = 3.
        // ===========================================================
        float2 HexGrid(float2 uv)
        {
            uv *= _HexScale;

            // sqrt(3) ≈ 1.7320508
            // The two interleaved rectangular grids that tile hex space:
            //   Grid A has period  (sqrt(3), 3)
            //   Grid B is offset by half the period
            const float2 r = float2(1.7320508, 3.0);
            const float2 h = r * 0.5;           // (sqrt(3)/2, 1.5)

            // Fold each UV into [-h, +h] for both grids
            float2 a = fmod(uv,     r) - h;
            float2 b = fmod(uv + h, r) - h;

            // Select the grid cell whose centre is nearer (= Voronoi for hex)
            float2 gv = dot(a, a) < dot(b, b) ? a : b;

            // -------------------------------------------------------
            //  Pointy-top hexagon SDF
            //  For a hex with circumradius 1 the exact distance metric:
            //    d = max( |p.y|, |p.x|*cos30 + |p.y|*sin30 )
            //      = max( |p.y|, |p.x|*0.866 + |p.y|*0.5   )
            //  d == 1  exactly on the six edges/vertices.
            //  d == 0  at the centre.
            // -------------------------------------------------------
            float2 agv = abs(gv);
            float edgeFactor = max(agv.y, agv.x * 0.8660254 + agv.y * 0.5);
            // edgeFactor is in [0, 1]: 0 = centre, 1 = edge

            // Pseudo-random value unique to this hex cell
            // (used for subtle per-cell brightness variation)
            float2 cellId = uv - gv;
            float  cellRand = frac(sin(dot(cellId, float2(127.1, 311.7))) * 43758.5453);

            return float2(edgeFactor, cellRand);
        }

        // ===========================================================
        //  TRIPLANAR HEX SAMPLE
        //  Samples HexGrid on three axis-aligned planes and blends
        //  by world-normal weight – completely seam-free on any mesh.
        // ===========================================================
        float2 TriplanarHex(float3 posOS, float3 normalOS)
        {
            // Blend weights: sharply favour the dominant axis
            float3 w = abs(normalOS);
            w = pow(w, 8.0);
            w /= (w.x + w.y + w.z + 1e-5);   // normalise to sum = 1

            float2 hx = HexGrid(posOS.yz);
            float2 hy = HexGrid(posOS.xz);
            float2 hz = HexGrid(posOS.xy);

            // Blend edge factors and cell randoms separately
            float edge = hx.x * w.x + hy.x * w.y + hz.x * w.z;
            float rand = hx.y * w.x + hy.y * w.y + hz.y * w.z;
            return float2(edge, rand);
        }

        // ===========================================================
        //  VERTEX SHADER  (shared by both passes)
        // ===========================================================
        v2f vert(appdata v)
        {
            v2f o;
            o.clipPos  = UnityObjectToClipPos(v.vertex);
            o.normalWS = UnityObjectToWorldNormal(v.normal);
            o.normalOS = v.normal;
            o.posOS    = v.vertex.xyz;
            o.posWS    = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.viewDir  = normalize(WorldSpaceViewDir(v.vertex));
            return o;
        }

        // ===========================================================
        //  SHARED FRAGMENT COMPUTATION
        //  Returns float4 colour/alpha ready to output.
        //  `facingSign` = +1 for back-face pass, -1 for front-face pass
        //  (we flip the normal for back-face so fresnel still works).
        // ===========================================================
        float4 ShieldFragment(v2f i, float facingSign)
        {
            // -------------------------------------------------------
            //  1. HEX GRID MASK
            // -------------------------------------------------------
            float2 hexData   = TriplanarHex(i.posOS, i.normalOS);
            float  edgeFactor = hexData.x;   // 0 = centre, 1 = edge
            float  cellRand   = hexData.y;

            // Smooth hex line:  1 at the line,  0 in the interior
            float hexLine = smoothstep(
                1.0 - _LineWidth - 1.0 / _LineSharpness,
                1.0 - _LineWidth,
                edgeFactor
            );

            // Subtle per-cell brightness so cells look individually lit
            float cellVariation = lerp(0.6, 1.0, cellRand);

            // -------------------------------------------------------
            //  2. FRESNEL  (rim glow)
            //  Dot product of view direction with normal → edge glow.
            //  For back faces we invert the normal.
            // -------------------------------------------------------
            float3 n = normalize(i.normalWS) * facingSign;
            float  NdotV   = saturate(dot(normalize(i.viewDir), n));
            float  fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;

            // -------------------------------------------------------
            //  3. PULSE  (uniform breathing animation)
            // -------------------------------------------------------
            float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
            pulse = lerp(1.0 - _PulseIntensity, 1.0, pulse);

            // -------------------------------------------------------
            //  4. HIT RIPPLE  (expanding ring from impact point)
            //  _HitTime is set to Time.time on each hit via C# script.
            //  The ripple expands outward, fades with age and distance.
            // -------------------------------------------------------
            float hitAge  = _Time.y - _HitTime;
            float hitDist = length(i.posWS - _HitPointWS.xyz);

            // sin wave radiating outward; negative ages (no hit) give ~0
            float rippleWave   = sin(hitDist * 6.0 - hitAge * _RippleSpeed);
            float rippleEnvAge = exp(-hitAge  * _RippleDecay);        // fades over time
            float rippleEnvDst = exp(-hitDist * 1.5);                 // fades with distance
            float ripple = max(0.0, rippleWave) * rippleEnvAge * rippleEnvDst
                           * _RippleIntensity;

            // -------------------------------------------------------
            //  5. COMBINE EMISSION
            //  hex lines + fresnel rim + ripple, all modulated by pulse
            // -------------------------------------------------------
            float emission = (hexLine * cellVariation + fresnel + ripple) * pulse;
            float3 finalColor = _ShieldColor.rgb
                              + _EmissionColor.rgb * emission * _EmissionIntensity;

            // -------------------------------------------------------
            //  6. ALPHA
            //  * Base transparency fills the interior with subtle tint.
            //  * Hex lines are more opaque.
            //  * Fresnel brightens the rim.
            // -------------------------------------------------------
            float alpha = _BaseAlpha
                        + hexLine * _HexAlpha
                        + fresnel * 0.4
                        + ripple  * 0.3;
            alpha = saturate(alpha);

            // Dim the back-face pass slightly
            alpha *= (facingSign > 0.0 ? 0.4 : 1.0);

            return float4(finalColor, alpha);
        }
        ENDCG

        // ==========================================================
        //  PASS 1 – Back faces (inner shell, rendered first)
        // ==========================================================
        Pass
        {
            Name "BackFace"
            Cull  Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment fragBack

            float4 fragBack(v2f i) : SV_Target
            {
                return ShieldFragment(i, 1.0);   // +1 = back face
            }
            ENDCG
        }

        // ==========================================================
        //  PASS 2 – Front faces (outer shell)
        // ==========================================================
        Pass
        {
            Name "FrontFace"
            Cull  Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment fragFront

            float4 fragFront(v2f i) : SV_Target
            {
                return ShieldFragment(i, -1.0);  // -1 = front face
            }
            ENDCG
        }
    }

    Fallback "Transparent/Diffuse"
}
