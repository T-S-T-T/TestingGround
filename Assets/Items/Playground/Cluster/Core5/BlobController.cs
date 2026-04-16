using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master controller for the player-controlled blob.
///
/// Responsibilities:
///   • Spawns and initialises all <see cref="BlobParticle"/> instances at startup.
///   • Reads player input and translates it into movement by displacing the
///     shared <see cref="TargetPosition"/> that all particles are attracted to.
///   • Recalculates the blob's centre-of-mass (CoM) every N fixed frames and
///     keeps this GameObject's transform anchored to it.
///   • Provides a public API for external systems (camera, UI, VFX) to query
///     blob state and apply forces.
///
/// Setup checklist:
///   1. Attach this component to an empty "Blob" GameObject.
///   2. Assign a <see cref="BlobSettings"/> asset in the Inspector.
///   3. Assign a Particle Prefab – a GameObject with
///      <see cref="BlobParticle"/>, Rigidbody, SphereCollider, and
///      a MeshRenderer (standard sphere works great).
///   4. Ensure "BlobParticle" exists as a Physics Layer (Project Settings →
///      Physics → Layer Collision Matrix). Enable self-collision on that layer.
///   5. Press Play.
/// </summary>
public class BlobController : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════
    //  Inspector fields
    // ══════════════════════════════════════════════════════════════════════

    [Header("References")]
    [Tooltip("Configuration asset (BlobSettings ScriptableObject).")]
    [SerializeField] private BlobSettings settings;

    [Tooltip("Prefab spawned for every particle. Must have BlobParticle, " +
             "Rigidbody, and SphereCollider components.")]
    [SerializeField] private GameObject particlePrefab;

    [Tooltip("Optional: material applied to every particle renderer. " +
             "Leave null to keep the prefab's default material.")]
    [SerializeField] private Material particleMaterial;

    [Header("Input")]
    [Tooltip("Name of the horizontal input axis (default: 'Horizontal').")]
    [SerializeField] private string horizontalAxis = "Horizontal";

    [Tooltip("Name of the vertical input axis (default: 'Vertical').")]
    [SerializeField] private string verticalAxis   = "Vertical";

    [Tooltip("Name of the jump button (default: 'Jump').")]
    [SerializeField] private string jumpButton     = "Jump";

    [Header("Debug")]
    [Tooltip("Draw a wire sphere at the target position in Scene view.")]
    [SerializeField] private bool drawTargetGizmo = true;

    // ══════════════════════════════════════════════════════════════════════
    //  Private state
    // ══════════════════════════════════════════════════════════════════════

    private readonly List<BlobParticle> particles = new List<BlobParticle>();

    /// <summary>
    /// The position all particles are spring-attracted toward.
    /// Displacing this point is how player movement is communicated to
    /// the blob without directly moving any individual particle.
    /// </summary>
    private Vector3 targetPosition;

    /// <summary>
    /// Cached centre-of-mass; updated every
    /// <see cref="BlobSettings.centerOfMassUpdateInterval"/> fixed frames.
    /// </summary>
    private Vector3 centerOfMass;

    /// <summary>Running fixed-frame counter for CoM throttle.</summary>
    private int fixedFrameCount;

    /// <summary>Whether the blob is currently considered grounded
    /// (used for jump gating).</summary>
    private bool isGrounded;

    /// <summary>Layer mask for the BlobParticle physics layer.</summary>
    private int particleLayer;

    // ══════════════════════════════════════════════════════════════════════
    //  Public properties (read-only for external systems)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>The position every particle is attracted toward this frame.</summary>
    public Vector3 TargetPosition  => targetPosition;

    /// <summary>Averaged world-space centre of all particles.</summary>
    public Vector3 CenterOfMass    => centerOfMass;

    /// <summary>Total number of live particles.</summary>
    public int     ParticleCount   => particles.Count;

    /// <summary>Approximate world-space velocity of the whole blob,
    /// calculated from the CoM change each fixed step.</summary>
    public Vector3 Velocity        { get; private set; }

    // ══════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        particleLayer  = LayerMask.NameToLayer("BlobParticle");
        centerOfMass   = transform.position;
        targetPosition = transform.position;

        SpawnParticles();
    }

    private void Update()
    {
        HandlePlayerInput();
    }

    private void FixedUpdate()
    {
        fixedFrameCount++;

        if (fixedFrameCount % settings.centerOfMassUpdateInterval == 0)
        {
            Vector3 previousCoM = centerOfMass;
            RecalculateCenterOfMass();
            Velocity        = (centerOfMass - previousCoM) / Time.fixedDeltaTime;
            transform.position = centerOfMass;
        }

        UpdateTargetPosition();
        CheckGrounded();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Spawn
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Instantiates <see cref="BlobSettings.particleCount"/> particles inside
    /// a sphere of radius <see cref="BlobSettings.spawnRadius"/> centred on
    /// this transform's position, then calls
    /// <see cref="BlobParticle.Initialize"/> on each one.
    /// </summary>
    private void SpawnParticles()
    {
        particles.Clear();

        // Parent all particles to this GameObject so the hierarchy is clean.
        // We don't use the parent transform for physics – Rigidbodies inside a
        // moving parent can cause jitter, so the parent stays at CoM only.
        Transform container = new GameObject("Particles").transform;
        container.SetParent(transform, false);

        for (int i = 0; i < settings.particleCount; i++)
        {
            // Uniform random point inside spawn sphere
            Vector3 spawnPos = transform.position
                             + Random.insideUnitSphere * settings.spawnRadius;

            GameObject go = Instantiate(particlePrefab, spawnPos,
                                        Random.rotation, container);
            go.name  = $"Particle_{i:000}";
            go.layer = particleLayer;

            // Apply override material if provided
            if (particleMaterial != null)
            {
                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = particleMaterial;
            }

            var p = go.GetComponent<BlobParticle>();
            if (p == null)
                p = go.AddComponent<BlobParticle>();

            p.Initialize(this, settings);
            particles.Add(p);
        }

        Debug.Log($"[BlobController] Spawned {particles.Count} particles.");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Player input
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reads Unity's Input axes and button in Update.
    /// Stores the desired movement direction for use in FixedUpdate via
    /// a member variable to avoid reading Input inside FixedUpdate
    /// (which can miss events).
    /// </summary>
    private Vector3 rawMoveInput;
    private bool    jumpPressed;

    private void HandlePlayerInput()
    {
        float h = Input.GetAxis(horizontalAxis);
        float v = Input.GetAxis(verticalAxis);

        // Move in the XZ plane; Y comes from jump
        rawMoveInput = new Vector3(h, 0f, v);

        if (Input.GetButtonDown(jumpButton))
            jumpPressed = true;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Target position
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called every FixedUpdate.
    ///
    /// When the player provides directional input the target point is
    /// displaced ahead of the CoM, causing particles to chase it and
    /// "lean into" the direction of travel – giving the blob convincing
    /// deformation on movement.
    ///
    /// When there is no input the target snaps back to the CoM so the
    /// blob rounds itself up.
    ///
    /// Jump applies an upward impulse to every particle simultaneously.
    /// </summary>
    private void UpdateTargetPosition()
    {
        if (rawMoveInput.sqrMagnitude > 0.001f)
        {
            // Desired target is CoM + normalised input * lead distance
            Vector3 desiredTarget = centerOfMass
                                  + rawMoveInput.normalized
                                  * settings.targetLeadDistance;

            // Smoothly move target toward desired position
            targetPosition = Vector3.Lerp(targetPosition, desiredTarget,
                                          Time.fixedDeltaTime * 15f);

            // Apply movement force to every particle
            ApplyForceToAll(rawMoveInput.normalized * settings.moveForce,
                            ForceMode.Force);
        }
        else
        {
            // No input: return target to CoM
            targetPosition = Vector3.Lerp(targetPosition, centerOfMass,
                                          Time.fixedDeltaTime * settings.targetReturnSpeed);
        }

        // Jump
        if (jumpPressed)
        {
            jumpPressed = false;
            if (isGrounded)
                ApplyImpulseToAll(Vector3.up * settings.jumpImpulse);
        }

        // Clear for next frame
        rawMoveInput = Vector3.zero;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Centre of mass
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Averages the world-space positions of all active particles.
    /// Called every <see cref="BlobSettings.centerOfMassUpdateInterval"/>
    /// fixed frames.
    /// </summary>
    private void RecalculateCenterOfMass()
    {
        if (particles.Count == 0) return;

        Vector3 sum = Vector3.zero;
        int     count = 0;

        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            sum  += particles[i].transform.position;
            count++;
        }

        if (count > 0)
            centerOfMass = sum / count;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Grounded check
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Determines whether the blob is grounded by casting a small downward
    /// sphere from the current CoM. Grounded state gates the jump action.
    /// </summary>
    private void CheckGrounded()
    {
        float checkRadius  = settings.spawnRadius * 0.5f;
        float checkDist    = settings.spawnRadius * 0.6f;

        // Only hit non-blob layers
        int mask = ~(1 << particleLayer);
        isGrounded = Physics.CheckSphere(centerOfMass - Vector3.up * checkDist,
                                         checkRadius, mask);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Public force API
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Applies a continuous force to every particle.
    /// </summary>
    /// <param name="force">World-space force vector.</param>
    /// <param name="mode">ForceMode passed to Rigidbody.AddForce.</param>
    public void ApplyForceToAll(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        for (int i = 0; i < particles.Count; i++)
            particles[i]?.Rigidbody.AddForce(force, mode);
    }

    /// <summary>
    /// Applies an instantaneous impulse to every particle (e.g. for jumps,
    /// explosions, or ability effects).
    /// </summary>
    /// <param name="impulse">World-space impulse vector.</param>
    public void ApplyImpulseToAll(Vector3 impulse)
        => ApplyForceToAll(impulse, ForceMode.Impulse);

    /// <summary>
    /// Applies an impulse only to particles within <paramref name="radius"/>
    /// of <paramref name="worldPos"/>. Useful for directional hit-reactions or
    /// surface splashes.
    /// </summary>
    /// <param name="worldPos">Centre of the affected sphere.</param>
    /// <param name="radius">World-space radius.</param>
    /// <param name="impulse">Impulse vector applied to each affected particle.</param>
    public void ApplyImpulseInRadius(Vector3 worldPos, float radius, Vector3 impulse)
    {
        float sqrR = radius * radius;
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            if ((particles[i].transform.position - worldPos).sqrMagnitude <= sqrR)
                particles[i].Rigidbody.AddForce(impulse, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Returns a read-only snapshot of all living particles.
    /// </summary>
    public IReadOnlyList<BlobParticle> GetParticles() => particles.AsReadOnly();

    // ══════════════════════════════════════════════════════════════════════
    //  Validation
    // ══════════════════════════════════════════════════════════════════════

    private void ValidateReferences()
    {
        if (settings == null)
            Debug.LogError("[BlobController] BlobSettings asset is not assigned!", this);

        if (particlePrefab == null)
            Debug.LogError("[BlobController] Particle Prefab is not assigned!", this);

        if (LayerMask.NameToLayer("BlobParticle") == -1)
            Debug.LogWarning("[BlobController] Physics layer 'BlobParticle' not found. " +
                             "Create it in Project Settings → Tags and Layers.", this);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Editor helpers
    // ══════════════════════════════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        if (!drawTargetGizmo) return;

        // Target position
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.7f);
        Gizmos.DrawWireSphere(Application.isPlaying ? targetPosition : transform.position,
                              settings != null ? settings.particleRadius * 2f : 0.3f);

        // Centre of mass
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(centerOfMass,
                              settings != null ? settings.spawnRadius : 1f);
    }
}
