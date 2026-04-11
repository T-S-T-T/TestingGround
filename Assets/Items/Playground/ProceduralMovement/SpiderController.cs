using UnityEngine;

/// <summary>
/// SpiderController (fixed)
///
/// Root cause of the flip: the old version rotated around transform.up,
/// but SpiderBodyAdjustment was also modifying transform.rotation for tilt.
/// The two systems fought, accumulated rotational drift, and eventually flipped.
///
/// Fix: the ROOT object rotates ONLY around world Vector3.up (steering).
/// All tilt is applied to the visual Body CHILD only, never the root.
/// The Rigidbody has FreezeRotation so physics can never rotate the root either.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SpiderController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotateSpeed = 120f;

    [Header("Ground check")]
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;

    [Header("References — assign in Inspector")]
    public Transform bodyTransform;          // visual Body child (receives tilt)
    public LegStepController[] legs;         // all 8 LegStepControllers
    public SpiderBodyAdjustment bodyAdjust;

    // ── Public ────────────────────────────────────────────────────────────────
    public bool IsGrounded { get; private set; }
    public float Speed => rb.linearVelocity.magnitude;

    // ── Private ───────────────────────────────────────────────────────────────
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Freeze ALL rotation — physics must never tilt the root.
        // Tilt lives on the Body child, not here.
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        CheckGround();
        HandleInput();
    }

    void CheckGround()
    {
        IsGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.05f,
            Vector3.down,
            groundCheckDistance,
            groundLayer);
    }

    void HandleInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Rotate around WORLD Y only — never transform.up.
        // If we used transform.up it could be tilted (if the root ever drifts),
        // compounding the error every frame.
        if (Mathf.Abs(h) > 0.01f)
            transform.Rotate(Vector3.up, h * rotateSpeed * Time.deltaTime, Space.World);

        // Move along the root's flat forward direction
        Vector3 move = transform.forward * v * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    public bool IsLegStepping(int index)
    {
        if (index < 0 || index >= legs.Length) return false;
        return legs[index].IsStepping;
    }
}