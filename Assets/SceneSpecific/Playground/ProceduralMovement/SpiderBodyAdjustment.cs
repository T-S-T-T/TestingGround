using UnityEngine;

/// <summary>
/// SpiderBodyAdjustment — attach to the Spider root (same object as SpiderController).
/// Reads all 8 foot positions each frame and adjusts the visual body's
/// height (ride height), terrain tilt, and idle breathing bob.
///
/// MUST run in LateUpdate so all LegStepControllers have already moved.
/// </summary>
public class SpiderBodyAdjustment : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Leg Data — assign all 8 LegStepControllers")]
    public LegStepController[] legs;   // order: FL, FR, ML, MR, RL, RR, BL, BR
                                       // (Front-Left, Front-Right, Mid-Left, etc.)

    [Header("Ride Height")]
    [Tooltip("How high the body centre sits above the average foot level.")]
    public float rideHeight = 0.55f;

    [Tooltip("How quickly the body tracks the target height. " +
             "Lower = more floaty, higher = snappier.")]
    public float heightSmoothSpeed = 6f;

    [Header("Tilt")]
    [Tooltip("How quickly the body tilts to match terrain slope.")]
    public float tiltSmoothSpeed = 4f;

    [Header("Idle Breathing")]
    [Tooltip("Subtle up/down oscillation when nearly stationary.")]
    public float breatheAmount = 0.018f;
    public float breatheSpeed  = 1.2f;

    [Header("Movement Bob")]
    [Tooltip("Extra oscillation that kicks in while walking.")]
    public float walkBobAmount = 0.025f;
    public float walkBobSpeed  = 9f;

    // ── Private ──────────────────────────────────────────────────────────────

    private SpiderController spider;
    private float breathTimer;
    private float bobTimer;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        spider = GetComponent<SpiderController>();
    }

    void LateUpdate()
    {
        if (legs == null || legs.Length == 0) return;

        ApplyHeightAndTilt();
        ApplyBreatheAndBob();
    }

    // ── Height & Tilt ─────────────────────────────────────────────────────────

    void ApplyHeightAndTilt()
    {
        // ── Average foot height ──
        float avgY = 0f;
        foreach (var leg in legs)
            avgY += leg.FootPosition.y;
        avgY /= legs.Length;

        float targetY = avgY + rideHeight;

        // Smooth the body toward the target height
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * heightSmoothSpeed);
        transform.position = pos;

        // ── Ground normal from leg cross-product ──
        // We need at least 4 legs for a meaningful normal.
        // Use the average front pair, rear pair, left side, right side.
        if (legs.Length >= 8)
        {
            // Front pair: legs[0] (FL) and legs[1] (FR)
            // Rear  pair: legs[6] (BL) and legs[7] (BR)
            // Left  side: legs[0,2,4,6]
            // Right side: legs[1,3,5,7]

            Vector3 frontAvg = (legs[0].FootPosition + legs[1].FootPosition) * 0.5f;
            Vector3 rearAvg  = (legs[6].FootPosition + legs[7].FootPosition) * 0.5f;
            Vector3 leftAvg  = (legs[0].FootPosition + legs[2].FootPosition
                              + legs[4].FootPosition + legs[6].FootPosition) * 0.25f;
            Vector3 rightAvg = (legs[1].FootPosition + legs[3].FootPosition
                              + legs[5].FootPosition + legs[7].FootPosition) * 0.25f;

            // Two spanning vectors across the foot polygon
            Vector3 fwdVec   = (frontAvg - rearAvg).normalized;
            Vector3 rightVec = (rightAvg - leftAvg).normalized;

            // Their cross product is the approximate ground normal
            Vector3 groundNormal = Vector3.Cross(fwdVec, rightVec).normalized;

            if (groundNormal != Vector3.zero)
            {
                // Rotate body so its "up" aligns with the ground normal,
                // while keeping its "forward" as close to the original as possible.
                Quaternion targetRot = Quaternion.FromToRotation(transform.up, groundNormal)
                                       * transform.rotation;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot,
                    Time.deltaTime * tiltSmoothSpeed);
            }
        }
    }

    // ── Breathing & Walking Bob ───────────────────────────────────────────────

    void ApplyBreatheAndBob()
    {
        float speed = spider != null ? spider.GetSpeed() : 0f;

        // Idle breathe — always running, fades out at speed
        breathTimer += Time.deltaTime * breatheSpeed;
        float breatheOffset = Mathf.Sin(breathTimer)
                            * breatheAmount
                            * Mathf.Clamp01(1f - speed * 0.5f);

        // Walk bob — only when moving
        if (speed > 0.05f)
            bobTimer += Time.deltaTime * walkBobSpeed;
        float walkBobOffset = Mathf.Sin(bobTimer)
                            * walkBobAmount
                            * Mathf.Clamp01(speed);

        transform.position += transform.up * (breatheOffset + walkBobOffset);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (legs == null) return;
        Gizmos.color = Color.cyan;
        foreach (var leg in legs)
        {
            if (leg != null)
                Gizmos.DrawWireSphere(leg.FootPosition, 0.05f);
        }
    }
#endif
}
