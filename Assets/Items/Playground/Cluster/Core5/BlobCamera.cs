using UnityEngine;

/// <summary>
/// Smooth follow camera for the player blob.
///
/// Features:
///   • Follows the blob's centre of mass with configurable lag.
///   • Orbit offset rotates slowly based on the blob's velocity direction,
///     keeping the camera slightly behind the direction of travel.
///   • Optional manual orbit via right stick / mouse drag.
///   • Collision avoidance: pulls the camera forward if geometry is between
///     the camera and the blob.
///
/// Setup:
///   Attach to the Main Camera (or any camera). Assign the BlobController
///   reference in the Inspector.
/// </summary>
public class BlobCamera : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════
    //  Inspector fields
    // ══════════════════════════════════════════════════════════════════════

    [Header("Target")]
    [Tooltip("The BlobController to follow.")]
    [SerializeField] private BlobController blob;

    [Header("Follow")]
    [Tooltip("Desired distance from the blob centre.")]
    [SerializeField] private float followDistance = 8f;

    [Tooltip("Height offset above the blob centre.")]
    [SerializeField] private float heightOffset = 3f;

    [Tooltip("Positional follow smoothing. Lower = more lag, higher = snappier.")]
    [SerializeField] [Range(1f, 30f)] private float positionSmoothing = 6f;

    [Tooltip("Rotational smoothing of the camera's look direction.")]
    [SerializeField] [Range(1f, 30f)] private float rotationSmoothing = 5f;

    [Header("Auto-Orbit (velocity-based)")]
    [Tooltip("How strongly the camera drifts behind the blob's direction of travel.")]
    [SerializeField] [Range(0f, 1f)] private float velocityOrbitStrength = 0.4f;

    [Tooltip("Minimum blob speed (m/s) before auto-orbit kicks in.")]
    [SerializeField] private float orbitSpeedThreshold = 0.5f;

    [Header("Manual Orbit (mouse / right stick)")]
    [Tooltip("Enable orbit via mouse drag (hold right-click) or right analogue stick.")]
    [SerializeField] private bool enableManualOrbit = true;

    [SerializeField] private float mouseSensitivity  = 3f;
    [SerializeField] private float stickSensitivity  = 90f; // degrees/sec

    [Tooltip("Vertical orbit clamp (degrees above horizon).")]
    [SerializeField] [Range(-30f, 0f)]  private float minVerticalAngle = -15f;
    [SerializeField] [Range(10f, 80f)]  private float maxVerticalAngle  = 60f;

    [Header("Collision")]
    [Tooltip("Pull the camera forward to avoid clipping through geometry.")]
    [SerializeField] private bool enableCollision = true;

    [Tooltip("Layers checked for camera collision.")]
    [SerializeField] private LayerMask collisionMask = ~0;   // everything by default

    [Tooltip("Minimum allowed distance when pushed by collision.")]
    [SerializeField] private float minDistance = 1.5f;

    // ══════════════════════════════════════════════════════════════════════
    //  Private state
    // ══════════════════════════════════════════════════════════════════════

    private Vector3 currentVelocity;   // used by SmoothDamp
    private float   yaw;               // current horizontal angle (degrees)
    private float   pitch;             // current vertical angle (degrees)

    // ══════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Start()
    {
        if (blob == null)
        {
            Debug.LogError("[BlobCamera] BlobController reference not set!", this);
            enabled = false;
            return;
        }

        // Initialise orbit angles from the camera's current world rotation
        Vector3 euler = transform.eulerAngles;
        yaw   = euler.y;
        pitch = euler.x;

        // Lock cursor for orbit control
        if (enableManualOrbit)
            Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (blob == null) return;

        UpdateOrbitAngles();

        Vector3 target   = blob.CenterOfMass + Vector3.up * heightOffset;
        float   distance = CalculateActualDistance();

        // Desired camera position
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    desired  = target - rotation * Vector3.forward * distance;

        // Smooth position
        transform.position = Vector3.SmoothDamp(transform.position, desired,
                                                 ref currentVelocity,
                                                 1f / positionSmoothing,
                                                 Mathf.Infinity, Time.deltaTime);

        // Look at blob
        Quaternion lookRot = Quaternion.LookRotation(target - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot,
                                               Time.deltaTime * rotationSmoothing);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Orbit
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateOrbitAngles()
    {
        // ── Manual orbit ──────────────────────────────────────────────
        if (enableManualOrbit)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Right stick (typical Unity axes)
            float stickX = Input.GetAxis("RightStickHorizontal") * stickSensitivity * Time.deltaTime;
            float stickY = Input.GetAxis("RightStickVertical")   * stickSensitivity * Time.deltaTime;

            yaw   += mouseX + stickX;
            pitch -= mouseY + stickY;
            pitch  = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        }

        // ── Auto orbit (velocity-based) ───────────────────────────────
        Vector3 blobVel = blob.Velocity;
        blobVel.y = 0f;   // only horizontal velocity affects yaw

        if (blobVel.magnitude > orbitSpeedThreshold)
        {
            float desiredYaw = Mathf.Atan2(blobVel.x, blobVel.z) * Mathf.Rad2Deg;
            yaw = Mathf.LerpAngle(yaw, desiredYaw,
                                  Time.deltaTime * velocityOrbitStrength * blobVel.magnitude);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Collision avoidance
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the largest distance (up to <see cref="followDistance"/>) that
    /// keeps the camera outside of geometry.
    /// </summary>
    private float CalculateActualDistance()
    {
        if (!enableCollision) return followDistance;

        Vector3 target = blob.CenterOfMass + Vector3.up * heightOffset;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direction = -(rot * Vector3.forward);

        if (Physics.SphereCast(target, 0.3f, direction, out RaycastHit hit,
                               followDistance, collisionMask,
                               QueryTriggerInteraction.Ignore))
        {
            return Mathf.Max(hit.distance * 0.9f, minDistance);
        }

        return followDistance;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Cleanup
    // ══════════════════════════════════════════════════════════════════════

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
    }
}
