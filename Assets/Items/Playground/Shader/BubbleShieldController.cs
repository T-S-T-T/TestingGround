// ============================================================
//  BubbleShieldController.cs
//
//  Attach this to the same GameObject that has a MeshRenderer
//  using the BubbleShield material.
//
//  Features:
//    - Automatic material instance creation (no shared-material edits)
//    - RegisterHit()  – triggers the expanding ripple effect
//    - TakeDamage()   – flashes the shield red briefly
//    - Public API for runtime property tweaks
// ============================================================

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BubbleShieldController : MonoBehaviour
{
    // ----------------------------------------------------------
    //  Inspector-exposed runtime parameters
    //  (mirror the shader Properties so designers can tweak in
    //   the Inspector without opening the material directly)
    // ----------------------------------------------------------
    [Header("Hex Grid")]
    [Range(2f, 40f)]  public float hexScale       = 12f;
    [Range(0.01f, 0.5f)] public float lineWidth   = 0.07f;

    [Header("Appearance")]
    public Color shieldColor    = new Color(0.05f, 0.4f,  1f,   1f);
    public Color emissionColor  = new Color(0f,    0.7f,  1f,   1f);
    [Range(0f, 8f)]  public float emissionIntensity = 2.5f;
    [Range(0f, 1f)]  public float baseAlpha          = 0.10f;
    [Range(0f, 1f)]  public float hexAlpha            = 0.85f;
    [Range(0.5f, 8f)] public float fresnelPower      = 3f;

    [Header("Pulse Animation")]
    [Range(0f, 6f)]  public float pulseSpeed     = 1.5f;
    [Range(0f, 1f)]  public float pulseIntensity = 0.25f;

    [Header("Hit Ripple")]
    [Range(1f, 20f)] public float rippleSpeed    = 8f;
    [Range(0f, 3f)]  public float rippleIntensity = 1.2f;
    [Range(1f, 10f)] public float rippleDecay    = 3f;

    [Header("Damage Flash")]
    public Color    damageColor    = new Color(1f, 0.1f, 0.1f, 1f);
    [Range(0f, 1f)] public float damageFlashTime = 0.3f;

    // ----------------------------------------------------------
    //  Shader property IDs  (cached for performance)
    //  Using Shader.PropertyToID is ~5× faster than string lookups
    // ----------------------------------------------------------
    static readonly int ID_HexScale          = Shader.PropertyToID("_HexScale");
    static readonly int ID_LineWidth         = Shader.PropertyToID("_LineWidth");
    static readonly int ID_ShieldColor       = Shader.PropertyToID("_ShieldColor");
    static readonly int ID_EmissionColor     = Shader.PropertyToID("_EmissionColor");
    static readonly int ID_EmissionIntensity = Shader.PropertyToID("_EmissionIntensity");
    static readonly int ID_BaseAlpha         = Shader.PropertyToID("_BaseAlpha");
    static readonly int ID_HexAlpha          = Shader.PropertyToID("_HexAlpha");
    static readonly int ID_FresnelPower      = Shader.PropertyToID("_FresnelPower");
    static readonly int ID_PulseSpeed        = Shader.PropertyToID("_PulseSpeed");
    static readonly int ID_PulseIntensity    = Shader.PropertyToID("_PulseIntensity");
    static readonly int ID_HitPointWS        = Shader.PropertyToID("_HitPointWS");
    static readonly int ID_HitTime           = Shader.PropertyToID("_HitTime");
    static readonly int ID_RippleSpeed       = Shader.PropertyToID("_RippleSpeed");
    static readonly int ID_RippleIntensity   = Shader.PropertyToID("_RippleIntensity");
    static readonly int ID_RippleDecay       = Shader.PropertyToID("_RippleDecay");

    // ----------------------------------------------------------
    //  Private state
    // ----------------------------------------------------------
    MeshRenderer  _renderer;
    Material      _mat;             // instanced material
    Color         _originalShield;
    Color         _originalEmission;
    Coroutine     _damageCoroutine;

    // ============================================================
    //  LIFECYCLE
    // ============================================================

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();

        // Create a per-instance material so we don't pollute the
        // shared asset (important when multiple shields are active).
        _mat = _renderer.material;          // Unity clones it automatically

        _originalShield   = shieldColor;
        _originalEmission = emissionColor;

        ApplyAllProperties();
    }

    void OnValidate()
    {
        // Called in Editor when Inspector values change – live preview
        if (_mat != null)
            ApplyAllProperties();
    }

    // ============================================================
    //  PUBLIC API
    // ============================================================

    /// <summary>
    /// Call this when a projectile/attack hits the shield.
    /// Pass the world-space contact point.
    /// </summary>
    public void RegisterHit(Vector3 worldPosition)
    {
        _mat.SetVector(ID_HitPointWS, worldPosition);
        _mat.SetFloat (ID_HitTime,    Time.time);
    }

    /// <summary>
    /// Triggers a RegisterHit AND flashes the shield red briefly.
    /// </summary>
    public void TakeDamage(Vector3 worldPosition, float amount = 10f)
    {
        RegisterHit(worldPosition);

        if (_damageCoroutine != null)
            StopCoroutine(_damageCoroutine);

        _damageCoroutine = StartCoroutine(DamageFlash());
    }

    /// <summary>
    /// Smoothly set the hex scale at runtime.
    /// </summary>
    public void SetHexScale(float scale)
    {
        hexScale = scale;
        _mat.SetFloat(ID_HexScale, scale);
    }

    /// <summary>
    /// Change the shield tint at runtime.
    /// </summary>
    public void SetShieldColor(Color color)
    {
        shieldColor = color;
        _mat.SetColor(ID_ShieldColor, color);
    }

    // ============================================================
    //  PRIVATE HELPERS
    // ============================================================

    /// Push every Inspector property to the instanced material.
    void ApplyAllProperties()
    {
        _mat.SetFloat (ID_HexScale,          hexScale);
        _mat.SetFloat (ID_LineWidth,         lineWidth);
        _mat.SetColor (ID_ShieldColor,       shieldColor);
        _mat.SetColor (ID_EmissionColor,     emissionColor);
        _mat.SetFloat (ID_EmissionIntensity, emissionIntensity);
        _mat.SetFloat (ID_BaseAlpha,         baseAlpha);
        _mat.SetFloat (ID_HexAlpha,          hexAlpha);
        _mat.SetFloat (ID_FresnelPower,      fresnelPower);
        _mat.SetFloat (ID_PulseSpeed,        pulseSpeed);
        _mat.SetFloat (ID_PulseIntensity,    pulseIntensity);
        _mat.SetFloat (ID_RippleSpeed,       rippleSpeed);
        _mat.SetFloat (ID_RippleIntensity,   rippleIntensity);
        _mat.SetFloat (ID_RippleDecay,       rippleDecay);
    }

    /// Flash shield to damage colour and back.
    IEnumerator DamageFlash()
    {
        float elapsed = 0f;
        float halfTime = damageFlashTime * 0.5f;

        // Lerp toward damage colour
        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            _mat.SetColor(ID_ShieldColor,   Color.Lerp(_originalShield,   damageColor, t));
            _mat.SetColor(ID_EmissionColor, Color.Lerp(_originalEmission, damageColor, t));
            yield return null;
        }

        elapsed = 0f;

        // Lerp back to original colour
        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            _mat.SetColor(ID_ShieldColor,   Color.Lerp(damageColor, _originalShield,   t));
            _mat.SetColor(ID_EmissionColor, Color.Lerp(damageColor, _originalEmission, t));
            yield return null;
        }

        _mat.SetColor(ID_ShieldColor,   _originalShield);
        _mat.SetColor(ID_EmissionColor, _originalEmission);
    }

    // ============================================================
    //  EXAMPLE: hook into Unity's physics (optional)
    //  Attach a Collider to the shield GameObject and enable
    //  "Is Trigger" to use this automatically.
    // ============================================================
    void OnTriggerEnter(Collider other)
    {
        // Get the closest surface point as the hit location
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        RegisterHit(hitPoint);
    }
}
