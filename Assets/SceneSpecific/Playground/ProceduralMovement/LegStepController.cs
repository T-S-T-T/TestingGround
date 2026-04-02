using System.Collections;
using UnityEngine;

/// <summary>
/// LegStepController — attach one to each of the 8 leg root GameObjects.
/// Decides when this leg should step, raycasts the landing point, and
/// animates the foot along an arc to the new target.
/// </summary>
public class LegStepController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Rest Position")]
    [Tooltip("Empty GameObject parented to the spider body. " +
             "Represents the ideal resting position for this foot.")]
    public Transform restPosition;

    [Header("Step Settings")]
    [Tooltip("How far the foot can drift from its rest position before stepping.")]
    public float stepThreshold = 0.35f;

    [Tooltip("How long a single step takes, in seconds.")]
    public float stepDuration = 0.12f;

    [Tooltip("Peak height of the foot arc during a step.")]
    public float stepHeight = 0.12f;

    [Tooltip("Extra distance to cast the new step target forward, " +
             "so the spider plants its foot in front of where the rest " +
             "position currently sits (gives a more natural gait).")]
    public float stepOvershoot = 0.1f;

    [Header("Raycast")]
    public float raycastOriginHeight = 1f;   // how far above restPos to start the ray
    public float raycastDistance = 2.5f;
    public LayerMask groundLayer;

    [Header("Partner Leg")]
    [Tooltip("Diagonal partner. This leg won't step if its partner is stepping.")]
    public LegStepController partnerLeg;

    // ── Public state (read by SpiderBodyAdjustment) ──────────────────────────

    /// <summary>Current world-space foot position.</summary>
    public Vector3 FootPosition { get; private set; }

    /// <summary>True while this leg is mid-step.</summary>
    public bool IsStepping { get; private set; }

    // ── Private ──────────────────────────────────────────────────────────────

    private SpiderController spider;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        spider = GetComponentInParent<SpiderController>();

        // Initialise foot to wherever the rest position currently is
        FootPosition = restPosition != null ? restPosition.position : transform.position;
    }

    void Update()
    {
        if (restPosition == null) return;

        float distFromRest = Vector3.Distance(FootPosition, restPosition.position);

        bool partnerStepping = partnerLeg != null && partnerLeg.IsStepping;

        if (!IsStepping && !partnerStepping && distFromRest > stepThreshold)
        {
            StartCoroutine(TakeStep());
        }
    }

    // ── Step coroutine ───────────────────────────────────────────────────────

    IEnumerator TakeStep()
    {
        IsStepping = true;

        Vector3 startPos = FootPosition;

        // Project a landing point: raycast from above the rest position,
        // slightly ahead in the spider's travel direction.
        Vector3 rayOrigin = restPosition.position
                          + Vector3.up * raycastOriginHeight
                          + spider.transform.forward * stepOvershoot;

        Vector3 targetPos;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                            raycastDistance, groundLayer))
        {
            targetPos = hit.point;
        }
        else
        {
            // No ground found — just step to rest position projected flat
            targetPos = restPosition.position;
            targetPos.y = startPos.y;
        }

        // Animate the arc
        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stepDuration);

            // Smooth the horizontal travel with an ease-in-out curve
            float tSmooth = Mathf.SmoothStep(0f, 1f, t);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, tSmooth);

            // Add the arc: sin curve peaks in the middle of the step
            pos.y += Mathf.Sin(t * Mathf.PI) * stepHeight;

            FootPosition = pos;
            yield return null;
        }

        FootPosition = targetPos;
        IsStepping = false;
    }
}
