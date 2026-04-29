using UnityEngine;

/// <summary>
/// Represents a single Boid agent. Applies the three classic Boid rules:
///   1. Separation  – steer away from nearby neighbours
///   2. Alignment   – steer toward the average heading of neighbours
///   3. Cohesion    – steer toward the average position of neighbours
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Boid : MonoBehaviour
{
    // ── Inspector-visible settings (overridden by BoidManager on spawn) ──────
    [Header("Movement")]
    public float minSpeed   = 3f;
    public float maxSpeed   = 7f;
    public float maxForce   = 5f;

    [Header("Neighbour Detection")]
    public float perceptionRadius = 5f;
    [Range(0f, 360f)]
    public float fieldOfView = 270f;   // degrees of forward vision

    [Header("Rule Weights")]
    public float separationWeight = 1.5f;
    public float alignmentWeight  = 1.0f;
    public float cohesionWeight   = 1.0f;

    [Header("Boundary")]
    public float boundaryRadius  = 25f;   // distance from origin before turning back
    public float boundaryForce   = 6f;

    // ── Private state ─────────────────────────────────────────────────────────
    private Rigidbody   rb;
    private BoidManager manager;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.linearDamping        = 0f;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // we rotate via transform
    }

    void Start()
    {
        manager = BoidManager.Instance;

        // Random initial velocity
        rb.linearVelocity = Random.insideUnitSphere.normalized * ((minSpeed + maxSpeed) * 0.5f);
    }

    void FixedUpdate()
    {
        Vector3 steering = CalculateSteering();

        // Apply steering force
        rb.AddForce(steering, ForceMode.Force);

        // Clamp speed between min/max
        float speed = rb.linearVelocity.magnitude;
        if (speed < minSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * minSpeed;
        else if (speed > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // Face the direction of travel
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, rb.linearVelocity.normalized, Time.fixedDeltaTime * 10f);
    }

    // ── Steering calculation ──────────────────────────────────────────────────

    private Vector3 CalculateSteering()
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment  = Vector3.zero;
        Vector3 cohesion   = Vector3.zero;

        int count = 0; // neighbours within perception radius

        foreach (Boid other in manager.AllBoids)
        {
            if (other == this) continue;

            Vector3 offset   = other.transform.position - transform.position;
            float   distance = offset.magnitude;

            if (distance > perceptionRadius) continue;
            if (!InFieldOfView(offset))      continue;

            // ── Rule 1 – Separation ──────────────────────────────────────────
            // Steer away from each neighbour, weighted by inverse distance
            if (distance > 0.001f)
                separation -= offset.normalized / distance;

            // ── Rule 2 – Alignment ───────────────────────────────────────────
            // Accumulate neighbours' velocities
            alignment += other.rb.linearVelocity;

            // ── Rule 3 – Cohesion ────────────────────────────────────────────
            // Accumulate neighbours' positions
            cohesion += other.transform.position;

            count++;
        }

        Vector3 steer = Vector3.zero;

        if (count > 0)
        {
            // Alignment: steer toward average heading
            alignment = (alignment / count).normalized * maxSpeed;
            steer += Limit(alignment - rb.linearVelocity, maxForce) * alignmentWeight;

            // Cohesion: steer toward average position
            cohesion = (cohesion / count - transform.position).normalized * maxSpeed;
            steer += Limit(cohesion - rb.linearVelocity, maxForce) * cohesionWeight;
        }

        // Separation is accumulated independently (no division needed)
        steer += Limit(separation.normalized * maxSpeed - rb.linearVelocity, maxForce) * separationWeight;

        // Boundary avoidance – soft push back toward origin
        steer += BoundaryForce();

        return steer;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns true if the direction to a neighbour is within our FOV.</summary>
    private bool InFieldOfView(Vector3 offsetToNeighbour)
    {
        if (fieldOfView >= 360f) return true;
        float angle = Vector3.Angle(transform.forward, offsetToNeighbour);
        return angle <= fieldOfView * 0.5f;
    }

    /// <summary>Clamps a vector's magnitude to maxMag.</summary>
    private static Vector3 Limit(Vector3 v, float maxMag)
    {
        if (v.sqrMagnitude > maxMag * maxMag)
            return v.normalized * maxMag;
        return v;
    }

    /// <summary>Returns a force that steers the boid back inside the boundary sphere.</summary>
    private Vector3 BoundaryForce()
    {
        float dist = transform.position.magnitude;
        if (dist < boundaryRadius) return Vector3.zero;

        // Proportional push back toward origin
        Vector3 desired = -transform.position.normalized * maxSpeed;
        return Limit(desired - rb.linearVelocity, boundaryForce) * ((dist - boundaryRadius) / boundaryRadius);
    }

    // ── Debug gizmos ──────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(Vector3.zero, boundaryRadius);
    }
}
