// ============================================================
//  BubbleShield.shader  (v2 – fixed hex tiling + outline color)
//
//  Key fixes over v1:
//    • Corrected hex SDF: boundary = 1 consistently along ALL
//      six edges AND all six vertices (not just the vertices).
//    • Separate _OutlineColor / _OutlineEmission for hex edges.
// ============================================================
Shader "Custom/BubbleShield"
{
    Properties
    {
        [Header(Hex Grid)]
        _HexScale          ("Hex Scale",           Range(2, 40))     = 12.0
        _LineWidth         ("Line Width",           Range(0.01, 0.4)) = 0.06
        _LineSharpness     ("Line Sharpness",       Range(5, 100))    = 50.0

        [Header(Interior Color)]
        _ShieldColor       ("Shield Color",         Color)  = (0.05, 0.4, 1.0, 1.0)
        _EmissionColor     ("Emission Color",       Color)  = (0.0,  0.5, 1.0, 1.0)
        _EmissionIntensity ("Emission Intensity",   Range(0, 8)) = 1.5

        [Header(Outline Color)]
        _OutlineColor      ("Outline Color",        Color)  = (0.5, 0.95, 1.0, 1.0)
        _OutlineEmission   ("Outline Emission",     Range(0, 12)) = 5.0

        [Header(Transparency and Fresnel)]
        _BaseAlpha         ("Base Alpha",           Range(0, 1)) = 0.04
        _HexAlpha          ("Hex Line Alpha",       Range(0, 1)) = 0.85
        _FresnelPower      ("Fresnel Power",        Range(0.5, 8)) = 3.0
        _FresnelIntensity  ("Fresnel Intensity",    Range(0, 2)) = 1.0

        [Header(Pulse Animation)]
        _PulseSpeed        ("Pulse Speed",          Range(0, 6)) = 1.5
        _PulseIntensity    ("Pulse Intensity",      Range(0, 1)) = 0.20

        [Header(Hit Ripple)]
        _HitPointWS        ("Hit Point (World)",    Vector) = (0, 0, 0, 0)
        _HitTime           ("Hit Time",             Float)  = -100.0
        _RippleSpeed       ("Ripple Speed",         Range(1, 20)) = 8.0
        _RippleIntensity   ("Ripple Intensity",     Range(0, 3))  = 1.2
        _RippleDecay       ("Ripple Decay",         Range(1, 10)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        CGINCLUDE
        #include "UnityCG.cginc"

        // ---- Properties ------------------------------------------------
        float4 _ShieldColor;
        float4 _EmissionColor;
        float  _EmissionIntensity;

        float4 _OutlineColor;
        float  _OutlineEmission;

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

        // ---- Structs ---------------------------------------------------
        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
        };

        struct v2f
        {
            float4 clipPos  : SV_POSITION;
            float3 normalWS : TEXCOORD0;
            float3 normalOS : TEXCOORD1;
            float3 posOS    : TEXCOORD2;
            float3 posWS    : TEXCOORD3;
            float3 viewDir  : TEXCOORD4;
        };

        // ================================================================
        //  HEX GRID  –  CORRECTED SDF
        //
        //  Pointy-top regular hexagon, circumradius = 1.
        //  Grid period: r = (√3, 3)  (2 rows per rectangular tile)
        //
        //  WHY THE OLD VERSION WAS WRONG
        //  ──────────────────────────────
        //  The old formula:  d = max( |y|,  |x|*0.866 + |y|*0.5 )
        //
        //  For a pointy-top hex the FLAT edges are on the LEFT/RIGHT
        //  (at x = ±inradius = ±√3/2 ≈ ±0.866), while the TOP/BOTTOM
        //  come to POINTS.  The old formula only reaches d=1 at the
        //  six vertices; along the flat edges it only ever reaches
        //  d ≈ 0.75 — well below the smoothstep threshold — so lines
        //  were never drawn on those edges.
        //
        //  CORRECTED SDF
        //  ──────────────
        //  The three pairs of parallel edges have inward-facing normals:
        //
        //      n1 = (1,   0)        →  |x|                 ≤ √3/2
        //      n2 = (0.5,  0.866)   →  |0.5·x + 0.866·y|  ≤ √3/2
        //      n3 = (0.5, -0.866)   →  |0.5·x - 0.866·y|  ≤ √3/2
        //
        //  Normalised boundary (divide by inradius √3/2):
        //      edgeFactor = max( |x|,  |0.5x+0.866y|,  |0.5x-0.866y| ) / 0.866
        //
        //  Verification  (should equal 1.0 on the whole boundary):
        //      flat right edge midpoint  (0.866, 0   ) → max(0.866,0.433,0.433)/0.866 = 1.0 ✓
        //      top vertex                (0,     1   ) → max(0,    0.866,0.866)/0.866 = 1.0 ✓
        //      top-right vertex  (0.866, 0.5)          → max(0.866,0.866,0    )/0.866 = 1.0 ✓
        //      centre            (0,     0   )          → 0                            = 0.0 ✓
        // ================================================================
        float2 HexGrid(float2 uv)
        {
            uv *= _HexScale;

            const float2 r = float2(1.7320508, 3.0);  // grid period (√3, 3)
            const float2 h = r * 0.5;                 // half-period (√3/2, 1.5)

            // Two interleaved rectangular grids → hexagonal Voronoi
            float2 a = fmod(uv,     r) - h;
            float2 b = fmod(uv + h, r) - h;
            float2 gv = dot(a, a) < dot(b, b) ? a : b;

            // Per-cell pseudo-random (for subtle brightness variation)
            float2 cellId   = uv - gv;
            float  cellRand = frac(sin(dot(cellId, float2(127.1, 311.7))) * 43758.5453);

            // ----- CORRECTED inradius-normalised SDF -------------------
            float d1 = abs(gv.x);
            float d2 = abs(0.5 * gv.x + 0.8660254 * gv.y);
            float d3 = abs(0.5 * gv.x - 0.8660254 * gv.y);

            float edgeFactor = max(d1, max(d2, d3)) / 0.8660254;
            edgeFactor = saturate(edgeFactor);  // guard against fp overshoot

            return float2(edgeFactor, cellRand);
        }

        // ================================================================
        //  TRIPLANAR HEX SAMPLE  –  seam-free on any mesh
        // ================================================================
        float2 TriplanarHex(float3 posOS, float3 normalOS)
        {
            float3 w = abs(normalOS);
            w = pow(w, 8.0);
            w /= (w.x + w.y + w.z + 1e-5);

            float2 hx = HexGrid(posOS.yz);
            float2 hy = HexGrid(posOS.xz);
            float2 hz = HexGrid(posOS.xy);

            float edge = hx.x * w.x + hy.x * w.y + hz.x * w.z;
            float rand = hx.y * w.x + hy.y * w.y + hz.y * w.z;
            return float2(edge, rand);
        }

        // ================================================================
        //  VERTEX SHADER
        // ================================================================
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

        // ================================================================
        //  SHARED FRAGMENT COMPUTATION
        //  facingSign: +1 = back-face (inner) pass, -1 = front-face pass.
        // ================================================================
        float4 ShieldFragment(v2f i, float facingSign)
        {
            // --------------------------------------------------------
            //  1. HEX GRID
            // --------------------------------------------------------
            float2 hexData    = TriplanarHex(i.posOS, i.normalOS);
            float  edgeFactor = hexData.x;   // 0 = cell interior, 1 = edge
            float  cellRand   = hexData.y;

            // Anti-aliased edge mask: 0 inside, 1 on the edge band
            float eps     = 1.0 / _LineSharpness;
            float hexLine = smoothstep(
                1.0 - _LineWidth - eps,
                1.0 - _LineWidth,
                edgeFactor
            );

            // Subtle per-cell brightness so cells look individually lit
            float cellVariation = lerp(0.7, 1.0, cellRand);

            // --------------------------------------------------------
            //  2. FRESNEL
            // --------------------------------------------------------
            float3 n      = normalize(i.normalWS) * facingSign;
            float  NdotV  = saturate(dot(normalize(i.viewDir), n));
            float  fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;

            // --------------------------------------------------------
            //  3. PULSE  (breathing animation)
            // --------------------------------------------------------
            float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
            pulse = lerp(1.0 - _PulseIntensity, 1.0, pulse);

            // --------------------------------------------------------
            //  4. HIT RIPPLE
            // --------------------------------------------------------
            float hitAge  = _Time.y - _HitTime;
            float hitDist = length(i.posWS - _HitPointWS.xyz);
            float wave    = sin(hitDist * 6.0 - hitAge * _RippleSpeed);
            float env     = exp(-hitAge * _RippleDecay) * exp(-hitDist * 1.5);
            float ripple  = max(0.0, wave) * env * _RippleIntensity;

            // --------------------------------------------------------
            //  5. COLOR COMPOSITION
            //
            //  • Interior area:  _ShieldColor + _EmissionColor * intensity
            //  • Edge lines:     _OutlineColor * _OutlineEmission
            //
            //  hexLine (0→1) crossfades between the two.
            //  Fresnel and ripple are blended toward the outline color.
            // --------------------------------------------------------
            float3 interiorRGB = _ShieldColor.rgb
                               + _EmissionColor.rgb * _EmissionIntensity * cellVariation;

            float3 outlineRGB  = _OutlineColor.rgb * _OutlineEmission;

            // Crossfade: interior where hexLine=0, outline where hexLine=1
            float3 finalColor = lerp(interiorRGB, outlineRGB, hexLine);

            // Fresnel + ripple: tint halfway between emission and outline
            float3 rimRGB = lerp(_EmissionColor.rgb, _OutlineColor.rgb, 0.5);
            finalColor   += rimRGB * (fresnel + ripple) * pulse;

            // --------------------------------------------------------
            //  6. ALPHA
            //
            //  Only the hex LINES, fresnel rim, and hit ripple are opaque.
            //  The interior fill (_BaseAlpha) is kept near-zero so you can
            //  see clean through the shield and read the back-face hex grid.
            // --------------------------------------------------------
            float alpha = _BaseAlpha           // near-zero interior tint
                        + hexLine  * _HexAlpha // lines are crisp
                        + fresnel  * 0.35      // rim glow
                        + ripple   * 0.25;
            alpha = saturate(alpha);

            // Back-face pass (inner shell): slightly dimmer than the front
            // but kept high enough that the far-side hex grid is clearly visible.
            // If you want both sides equally bright set both to 1.0.
            alpha *= (facingSign > 0.0 ? 0.75 : 1.0);

            return float4(finalColor, alpha);
        }
        ENDCG

        // ----------------------------------------------------------
        //  PASS 1 – Back faces (inner shell, rendered first)
        // ----------------------------------------------------------
        Pass
        {
            Name "BackFace"
            Cull  Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment fragBack
            float4 fragBack(v2f i) : SV_Target { return ShieldFragment(i,  1.0); }
            ENDCG
        }

        // ----------------------------------------------------------
        //  PASS 2 – Front faces (outer shell, rendered on top)
        // ----------------------------------------------------------
        Pass
        {
            Name "FrontFace"
            Cull  Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment fragFront
            float4 fragFront(v2f i) : SV_Target { return ShieldFragment(i, -1.0); }
            ENDCG
        }
    }

    Fallback "Transparent/Diffuse"
}
