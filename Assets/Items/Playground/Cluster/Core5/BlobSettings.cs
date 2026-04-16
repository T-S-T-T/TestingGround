using UnityEngine;

/// <summary>
/// ScriptableObject that holds all configuration parameters for the Blob simulation.
/// Create via: Assets > Create > Blob > Blob Settings
/// </summary>
[CreateAssetMenu(fileName = "BlobSettings", menuName = "Blob/Blob Settings", order = 0)]
public class BlobSettings : ScriptableObject
{
    // ─────────────────────────────────────────────
    //  SPAWN
    // ─────────────────────────────────────────────
    [Header("Spawn")]
    [Tooltip("How many physics particles make up the blob.")]
    [Range(50, 5000)]
    public int particleCount = 300;

    [Tooltip("Radius of the sphere in which particles are randomly spawned.")]
    [Range(0.1f, 5f)]
    public float spawnRadius = 1.2f;

    // ─────────────────────────────────────────────
    //  PARTICLE PHYSICS
    // ─────────────────────────────────────────────
    [Header("Particle Physics")]
    [Tooltip("Mass of each individual particle Rigidbody.")]
    [Range(0.01f, 2f)]
    public float particleMass = 0.05f;

    [Tooltip("Linear drag applied to each Rigidbody. Higher = more viscous slime.")]
    [Range(0f, 20f)]
    public float particleDrag = 4f;

    [Tooltip("Angular drag applied to each Rigidbody.")]
    [Range(0f, 20f)]
    public float particleAngularDrag = 0.5f;

    [Tooltip("Visual and collision radius of each particle sphere.")]
    [Range(0.05f, 0.5f)]
    public float particleRadius = 0.12f;

    [Tooltip("PhysicsMaterial used for particle colliders. Leave null to auto-generate.")]
    public PhysicsMaterial particlePhysicsMaterial;

    // ─────────────────────────────────────────────
    //  COHESION  (attraction toward the target)
    // ─────────────────────────────────────────────
    [Header("Cohesion")]
    [Tooltip("Base spring strength pulling each particle toward the blob target.")]
    [Range(0f, 500f)]
    public float cohesionStrength = 80f;

    [Tooltip("Distance at which cohesion force is at full strength.")]
    [Range(0.1f, 20f)]
    public float cohesionMaxDistance = 6f;

    [Tooltip("Extra multiplier applied to particles that have strayed very far, " +
             "simulating surface tension snapping them back.")]
    [Range(1f, 10f)]
    public float surfaceTensionMultiplier = 3f;

    [Tooltip("Distance beyond which the surface tension bonus kicks in.")]
    [Range(0.5f, 10f)]
    public float surfaceTensionThreshold = 2.5f;

    // ─────────────────────────────────────────────
    //  MOVEMENT  (player input)
    // ─────────────────────────────────────────────
    [Header("Player Movement")]
    [Tooltip("Force applied to the entire blob when the player provides directional input.")]
    [Range(0f, 500f)]
    public float moveForce = 120f;

    [Tooltip("Maximum speed any single particle can reach.")]
    [Range(0f, 30f)]
    public float maxParticleSpeed = 10f;

    [Tooltip("Impulse applied upward when the player jumps.")]
    [Range(0f, 30f)]
    public float jumpImpulse = 6f;

    [Tooltip("How far ahead of the blob center the movement target travels.")]
    [Range(0f, 5f)]
    public float targetLeadDistance = 1.5f;

    [Tooltip("How quickly the target position returns to center when there is no input.")]
    [Range(0f, 20f)]
    public float targetReturnSpeed = 5f;

    // ─────────────────────────────────────────────
    //  JIGGLE  (organic noise motion)
    // ─────────────────────────────────────────────
    [Header("Jiggle")]
    [Tooltip("Magnitude of the organic Perlin-noise jiggle force on each particle.")]
    [Range(0f, 20f)]
    public float jiggleStrength = 3f;

    [Tooltip("Time-scale of the jiggle noise. Higher = faster wobble.")]
    [Range(0.1f, 10f)]
    public float jiggleFrequency = 1.4f;

    // ─────────────────────────────────────────────
    //  PERFORMANCE
    // ─────────────────────────────────────────────
    [Header("Performance")]
    [Tooltip("Only recalculate the center-of-mass every N fixed frames " +
             "to save cost on large particle counts.")]
    [Range(1, 10)]
    public int centerOfMassUpdateInterval = 2;

    [Tooltip("When enabled, Unity's Job System is used for force accumulation " +
             "(requires Burst package). Falls back to main-thread if unavailable.")]
    public bool useBurstJobs = false;
}
