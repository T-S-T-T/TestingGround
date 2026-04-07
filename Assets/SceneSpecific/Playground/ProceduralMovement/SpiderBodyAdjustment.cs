using UnityEngine;

/// <summary>
/// SpiderBodyAdjustment (fixed — three bugs addressed)
///
/// BUG 1 — Body scraping ground / height jitter:
///   Old code: set transform.position.y directly every LateUpdate.
///   Rigidbody physics (gravity, collision) runs in FixedUpdate and also sets Y.
///   They conflict → jitter and the body sinking into the ground.
///   Fix: apply an upward spring FORCE via the Rigidbody instead of moving
///   the transform directly. Physics resolves everything in one place.
///
/// BUG 2 — Rotation jitter:
///   Old code: Quaternion.FromToRotation(up, groundNormal) * transform.rotation
///   This computes a delta and multiplies it onto the existing rotation every frame.
///   Any drift accumulates and oscillates.
///   Fix: compute the ABSOLUTE target rotation directly with Quaternion.LookRotation,
///   then Slerp toward it. No delta — no drift accumulation.
///
/// BUG 3 — Flip on W:
///   Old code: applied tilt directly to the root transform, which shares rotation
///   with SpiderController's steering. The two systems fought → flip.
///   Fix: tilt is applied ONLY to the visual bodyChild transform (local rotation).
///   The root transform stays upright (Y-rotation only) at all times.
/// </summary>
public class SpiderBodyAdjustment : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Leg data — assign all 8 in order: FL FR ML MR RL RR BL BR")]
    public LegStepController[] legs;

    [Header("Body child — the visual mesh, NOT the root")]
    [Tooltip("This is the child GameObject that holds your spider mesh. " +
             "Tilt is applied here so it never touches the root's rotation.")]
    public Transform bodyChild;

    [Header("Height — spring force (replaces direct transform.position.y)")]
    [Tooltip("Target distance above the average foot height.")]
    public float rideHeight = 0.7f;

    [Tooltip("Spring stiffness. Higher = snappier. Start around 80.")]
    public float springStrength = 80f;

    [Tooltip("Spring damping. Higher = less bounce. Usually springStrength * 0.6.")]
    public float springDamper = 50f;

    [Tooltip("Maximum upward force the spring can apply per frame. " +
             "Prevents explosive correction if the spider spawns far above ground.")]
    public float maxSpringForce = 200f;

    [Header("Tilt (applied to bodyChild only)")]
    [Tooltip("How quickly the visual body tilts to match terrain. " +
             "Keep below 6 to avoid oscillation.")]
    public float tiltSmoothSpeed = 4f;

    [Tooltip("Maximum tilt angle in degrees. Clamps extreme slopes.")]
    public float maxTiltDegrees = 25f;

    [Header("Idle breathe")]
    public float breatheAmount = 0.015f;
    public float breatheSpeed = 1.2f;

    [Header("Walk bob (visual body only)")]
    public float walkBobAmount = 0.02f;
    public float walkBobSpeed = 9f;

    // ── Private ───────────────────────────────────────────────────────────────

    private Rigidbody rb;
    private SpiderController spider;
    private float breathTimer;
    private float bobTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spider = GetComponent<SpiderController>();

        if (bodyChild == null)
            Debug.LogWarning("[SpiderBodyAdjustment] bodyChild is not assigned. " +
                             "Tilt will not work and may cause flipping.", this);
    }

    // Height spring runs in FixedUpdate — same physics tick as Rigidbody.
    void FixedUpdate()
    {
        ApplyHeightSpring();
    }

    // Tilt and bob run in LateUpdate — after all leg steps have resolved.
    void LateUpdate()
    {
        if (legs == null || legs.Length == 0) return;
        ApplyTilt();
        ApplyBreatheAndBob();
    }

    // ── Height spring ─────────────────────────────────────────────────────────

    void ApplyHeightSpring()
    {
        if (legs == null || legs.Length == 0) return;

        // Average of all foot Y positions
        float avgFootY = 0f;
        foreach (var leg in legs)
            avgFootY += leg.FootPosition.y;
        avgFootY /= legs.Length;

        float targetY = avgFootY + rideHeight;
        float currentY = transform.position.y;
        float error = targetY - currentY;          // positive = too low
        float velY = rb.linearVelocity.y;

        // Spring-damper formula: F = k*error - d*velocity
        float force = (error * springStrength) - (velY * springDamper);
        force = Mathf.Clamp(force, -maxSpringForce, maxSpringForce);

        rb.AddForce(Vector3.up * force, ForceMode.Force);
    }

    // ── Tilt (bodyChild local rotation only) ──────────────────────────────────

    void ApplyTilt()
    {
        if (bodyChild == null || legs.Length < 8) return;

        // Build ground normal from foot positions.
        // Front pair: [0]=FL, [1]=FR  /  Rear pair: [6]=BL, [7]=BR
        Vector3 frontMid = (legs[0].FootPosition + legs[1].FootPosition) * 0.5f;
        Vector3 rearMid = (legs[6].FootPosition + legs[7].FootPosition) * 0.5f;
        Vector3 leftMid = (legs[0].FootPosition + legs[2].FootPosition
                          + legs[4].FootPosition + legs[6].FootPosition) * 0.25f;
        Vector3 rightMid = (legs[1].FootPosition + legs[3].FootPosition
                          + legs[5].FootPosition + legs[7].FootPosition) * 0.25f;

        Vector3 fwd = (frontMid - rearMid).normalized;
        Vector3 right = (rightMid - leftMid).normalized;

        // Fallback if degenerate (e.g. all feet at same height)
        if (fwd == Vector3.zero) fwd = transform.forward;
        if (right == Vector3.zero) right = transform.right;

        Vector3 groundNormal = Vector3.Cross(fwd, right).normalized;
        if (groundNormal == Vector3.zero) return;

        // Clamp tilt so the spider can't fold completely on steep terrain
        float tiltAngle = Vector3.Angle(Vector3.up, groundNormal);
        if (tiltAngle > maxTiltDegrees)
            groundNormal = Vector3.Slerp(Vector3.up, groundNormal,
                                         maxTiltDegrees / tiltAngle);

        // Build an ABSOLUTE target rotation for the body child:
        // face the root's forward, but tilt "up" toward the ground normal.
        // Using LookRotation avoids delta-accumulation drift entirely.
        Quaternion targetRot = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized,
            groundNormal);

        // Apply as LOCAL rotation on the body child
        // (world-space target converted to local because the root may have Y rotation)
        Quaternion localTarget = Quaternion.Inverse(transform.rotation) * targetRot;

        bodyChild.localRotation = Quaternion.Slerp(
            bodyChild.localRotation,
            localTarget,
            Time.deltaTime * tiltSmoothSpeed);
    }

    // ── Breathe & walk bob (bodyChild local position) ─────────────────────────

    void ApplyBreatheAndBob()
    {
        if (bodyChild == null) return;

        float speed = spider != null ? spider.Speed : 0f;

        breathTimer += Time.deltaTime * breatheSpeed;
        float breathOffset = Mathf.Sin(breathTimer)
                           * breatheAmount
                           * Mathf.Clamp01(1f - speed * 0.4f);

        if (speed > 0.05f) bobTimer += Time.deltaTime * walkBobSpeed;
        float bobOffset = Mathf.Sin(bobTimer)
                        * walkBobAmount
                        * Mathf.Clamp01(speed);

        // Move only the visual child vertically — not the physics root
        Vector3 localPos = bodyChild.localPosition;
        localPos.y = breathOffset + bobOffset;   // relative to root, not world
        bodyChild.localPosition = localPos;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (legs == null) return;
        Gizmos.color = UnityEngine.Color.cyan;
        foreach (var leg in legs)
            if (leg != null)
                Gizmos.DrawWireSphere(leg.FootPosition, 0.05f);

        // Draw target ride height
        if (legs.Length > 0)
        {
            float avgY = 0f;
            foreach (var leg in legs) avgY += leg.FootPosition.y;
            avgY /= legs.Length;
            Gizmos.color = UnityEngine.Color.yellow;
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, avgY + rideHeight, transform.position.z),
                new Vector3(0.5f, 0.02f, 0.5f));
        }
    }
#endif
}