using UnityEngine;

/// <summary>
/// Attached to every individual particle that makes up the blob.
///
/// Responsibilities:
///   • Cohesion  – spring force toward the shared <see cref="BlobController.TargetPosition"/>.
///   • Surface tension – stronger pull for particles that have drifted far away.
///   • Jiggle    – per-particle Perlin-noise force that gives organic wobble.
///   • Speed cap – prevents particles from flying off at unrealistic velocities.
///
/// The particle relies on Unity's built-in PhysX collision to handle repulsion
/// between neighbours; no manual O(n²) overlap queries are needed.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BlobParticle : MonoBehaviour
{
    // ── Cached components ──────────────────────────────────────────────────
    private Rigidbody       rb;
    private SphereCollider  col;

    // ── References set by BlobController ──────────────────────────────────
    private BlobController blobController;
    private BlobSettings   settings;

    // ── Per-particle randomised noise offset (so particles don't all jiggle
    //    in sync, which would look robotic) ─────────────────────────────────
    private float noiseOffsetX;
    private float noiseOffsetY;
    private float noiseOffsetZ;

    // ── Public accessors ───────────────────────────────────────────────────
    /// <summary>The particle's Rigidbody, exposed so BlobController can apply
    /// impulses (e.g. jump) without GetComponent calls.</summary>
    public Rigidbody Rigidbody => rb;

    // ══════════════════════════════════════════════════════════════════════
    //  Initialisation
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called once by <see cref="BlobController"/> immediately after
    /// instantiation. Must be called before the first FixedUpdate.
    /// </summary>
    /// <param name="controller">The owning BlobController.</param>
    /// <param name="cfg">Shared settings asset.</param>
    public void Initialize(BlobController controller, BlobSettings cfg)
    {
        blobController = controller;
        settings       = cfg;

        // ── Rigidbody setup ───────────────────────────────────────────
        rb                   = GetComponent<Rigidbody>();
        rb.mass              = cfg.particleMass;
        rb.linearDamping              = cfg.particleDrag;
        rb.angularDamping       = cfg.particleAngularDrag;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation     = RigidbodyInterpolation.Interpolate;

        // ── Collider setup ────────────────────────────────────────────
        col        = GetComponent<SphereCollider>();
        col.radius = cfg.particleRadius;

        // Apply shared PhysicsMaterial (or auto-generate one)
        if (cfg.particlePhysicsMaterial != null)
        {
            col.material = cfg.particlePhysicsMaterial;
        }
        else
        {
            var mat                 = new PhysicsMaterial("BlobMat");
            mat.bounciness          = 0f;
            mat.dynamicFriction     = 0.1f;
            mat.staticFriction      = 0.1f;
            mat.frictionCombine     = PhysicsMaterialCombine.Minimum;
            mat.bounceCombine       = PhysicsMaterialCombine.Minimum;
            col.material            = mat;
        }

        // Scale the visual mesh to match collider radius
        transform.localScale = Vector3.one * (cfg.particleRadius * 2f);

        // ── Per-particle noise offsets (seeded randomly) ──────────────
        noiseOffsetX = Random.Range(0f, 999f);
        noiseOffsetY = Random.Range(0f, 999f);
        noiseOffsetZ = Random.Range(0f, 999f);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Physics update
    // ══════════════════════════════════════════════════════════════════════

    private void FixedUpdate()
    {
        if (blobController == null || settings == null) return;

        ApplyCohesion();
        ApplyJiggle();
        ClampSpeed();
    }

    // ── Cohesion ──────────────────────────────────────────────────────────

    /// <summary>
    /// Pulls this particle toward the blob's target position with a
    /// distance-proportional spring force. Particles that have strayed
    /// past <see cref="BlobSettings.surfaceTensionThreshold"/> receive an
    /// additional multiplier to simulate surface tension.
    /// </summary>
    private void ApplyCohesion()
    {
        Vector3 toTarget  = blobController.TargetPosition - transform.position;
        float   dist      = toTarget.magnitude;

        if (dist < 0.001f) return;   // already at target – skip

        // Normalised distance [0..1] within the max cohesion range
        float t = Mathf.Clamp01(dist / settings.cohesionMaxDistance);

        float strength = settings.cohesionStrength * t;

        // Surface-tension snap: extra pull for stray particles
        if (dist > settings.surfaceTensionThreshold)
        {
            float excess = (dist - settings.surfaceTensionThreshold)
                         / settings.cohesionMaxDistance;
            strength += settings.cohesionStrength
                      * excess
                      * settings.surfaceTensionMultiplier;
        }

        rb.AddForce(toTarget.normalized * strength, ForceMode.Force);
    }

    // ── Jiggle ────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a small Perlin-noise based force each physics step.
    /// Because every particle has a unique noise offset, the blob surface
    /// ripples organically rather than oscillating as one rigid unit.
    /// </summary>
    private void ApplyJiggle()
    {
        float t = Time.time * settings.jiggleFrequency;

        // Perlin noise returns [0..1]; remap to [-1..1]
        float nx = (Mathf.PerlinNoise(t + noiseOffsetX, 0f)        - 0.5f) * 2f;
        float ny = (Mathf.PerlinNoise(0f,               t + noiseOffsetY) - 0.5f) * 2f;
        float nz = (Mathf.PerlinNoise(t + noiseOffsetZ, t + noiseOffsetZ) - 0.5f) * 2f;

        rb.AddForce(new Vector3(nx, ny, nz) * settings.jiggleStrength,
                    ForceMode.Force);
    }

    // ── Speed cap ────────────────────────────────────────────────────────

    /// <summary>
    /// Hard-clamps the particle's linear velocity so no single particle
    /// flies out of the blob due to force accumulation or collision spikes.
    /// </summary>
    private void ClampSpeed()
    {
        if (rb.linearVelocity.sqrMagnitude >
            settings.maxParticleSpeed * settings.maxParticleSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * settings.maxParticleSpeed;
        }
    }
}
