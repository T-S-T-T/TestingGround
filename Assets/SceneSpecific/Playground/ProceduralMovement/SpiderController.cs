using UnityEngine;

/// <summary>
/// SpiderController — attach to the Spider root GameObject.
/// Handles player input, moves the body, and coordinates all subsystems.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SpiderController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotateSpeed = 120f;
    public float groundCheckDistance = 0.4f;
    public LayerMask groundLayer;

    [Header("References — assign in Inspector")]
    public Transform bodyTransform;          // the visual spider body child
    public LegStepController[] legs;         // all 8 LegStepController components
    public SpiderBodyAdjustment bodyAdjust;  // the body bob/tilt component

    // Internals
    private Rigidbody rb;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;            // we handle rotation ourselves
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        CheckGround();
        HandleInput();
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(
            transform.position, -transform.up,
            groundCheckDistance + 0.1f, groundLayer);
    }

    void HandleInput()
    {
        float h = Input.GetAxis("Horizontal");   // A/D or Left/Right
        float v = Input.GetAxis("Vertical");     // W/S or Up/Down

        // Rotate the spider around its local up axis
        transform.Rotate(transform.up, h * rotateSpeed * Time.deltaTime);

        // Move forward/backward along local forward
        Vector3 move = transform.forward * v * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    // ── Public helpers used by LegStepController ───────────────────────────

    /// <summary>Returns this spider's current velocity magnitude.</summary>
    public float GetSpeed() => rb.linearVelocity.magnitude;

    /// <summary>Returns whether any leg at the given index is currently stepping.</summary>
    public bool IsLegStepping(int index)
    {
        if (index < 0 || index >= legs.Length) return false;
        return legs[index].IsStepping;
    }
}
