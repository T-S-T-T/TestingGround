using UnityEngine;

/// <summary>
/// Titanfall 2-style player movement controller.
/// Requires: CharacterController component, PlayerCamera script,
///           Layer "Ground" and Layer "Wall" set up in project.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ─── Inspector Sections ───────────────────────────────────────────────────

    [Header("Ground Movement")]
    [SerializeField] float walkSpeed       = 7f;
    [SerializeField] float sprintSpeed     = 12f;
    [SerializeField] float groundAccel     = 60f;   // how quickly we reach target speed
    [SerializeField] float groundFriction  = 18f;   // how quickly we decelerate on ground
    [SerializeField] float airAccel        = 12f;   // reduced air control
    [SerializeField] float airFriction     = 2f;

    [Header("Jumping")]
    [SerializeField] float jumpHeight      = 2.2f;
    [SerializeField] float doubleJumpHeight= 1.8f;
    [SerializeField] float gravity         = -28f;
    [SerializeField] float fallMultiplier  = 1.6f;  // faster fall arc (less floaty)
    [SerializeField] float coyoteTime      = 0.12f; // grace period after walking off ledge
    [SerializeField] float jumpBufferTime  = 0.15f; // jump input buffer before landing

    [Header("Wall Running")]
    [SerializeField] float wallRunSpeed    = 14f;
    [SerializeField] float wallRunDuration = 1.8f;  // max time before sliding off
    [SerializeField] float wallRunGravity  = -4f;   // slight downward pull while wall running
    [SerializeField] float wallJumpForce   = 14f;   // lateral force away from wall
    [SerializeField] float wallJumpUp      = 8f;
    [SerializeField] float wallCheckDist   = 0.65f; // raycast distance for wall detection
    [SerializeField] float wallMinSpeed    = 5f;    // minimum speed to initiate wall run
    [SerializeField] float wallCooldown    = 0.4f;  // prevent re-grabbing same wall

    [Header("Wall Climbing")]
    [SerializeField] float climbSpeed      = 6f;
    [SerializeField] float climbDuration   = 1.2f;  // max climb time

    [Header("Sliding")]
    [SerializeField] float slideSpeed      = 16f;
    [SerializeField] float slideDuration   = 0.9f;
    [SerializeField] float slideHeightMult = 0.5f;  // crouch height factor
    [SerializeField] float slopeSlideMult  = 1.4f;  // extra speed on slopes

    [Header("Layer Masks")]
    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask wallMask;

    // ─── Internal State ───────────────────────────────────────────────────────

    CharacterController _cc;
    Transform           _cam;

    // Velocity & physics
    Vector3 _velocity;
    Vector3 _horizontalVel;
    float   _verticalVel;

    // Ground state
    bool  _isGrounded;
    bool  _wasGrounded;
    float _coyoteTimer;

    // Jump state
    bool  _jumpBuffered;
    float _jumpBufferTimer;
    int   _jumpsUsed;        // 0=none, 1=first, 2=double
    bool  _canDoubleJump;

    // Wall state
    enum WallState { None, Running, Climbing }
    WallState _wallState = WallState.None;
    Vector3   _wallNormal;
    Vector3   _wallForward;
    float     _wallTimer;
    float     _wallCooldownTimer;
    int       _lastWallID = -1; // collider instance ID for same-wall cooldown
    bool      _wallJumped;

    // Slide state
    bool  _isSliding;
    float _slideTimer;
    float _defaultHeight;
    float _slideHeight;
    Vector3 _slideDir;

    // Input cache
    Vector2 _moveInput;
    bool    _sprintHeld;
    bool    _crouchHeld;
    bool    _jumpPressed;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        _cc            = GetComponent<CharacterController>();
        _cam           = Camera.main.transform;
        _defaultHeight = _cc.height;
        _slideHeight   = _defaultHeight * slideHeightMult;
    }

    void Update()
    {
        GatherInput();
        UpdateGroundState();
        UpdateWallDetection();
        HandleJump();
        HandleSlide();
        ApplyMovement();
        ApplyGravity();
        MoveCharacter();
        HandleCooldowns();
    }

    // ─── Input ────────────────────────────────────────────────────────────────

    void GatherInput()
    {
        _moveInput   = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _sprintHeld  = Input.GetKey(KeyCode.LeftShift);
        _crouchHeld  = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _jumpPressed      = true;
            _jumpBufferTimer  = jumpBufferTime;
        }
    }

    // ─── Ground Detection ─────────────────────────────────────────────────────

    void UpdateGroundState()
    {
        _wasGrounded = _isGrounded;

        // Sphere cast slightly below the character center
        float radius = _cc.radius * 0.9f;
        Vector3 sphereOrigin = transform.position + Vector3.up * (radius - 0.1f);
        _isGrounded = Physics.CheckSphere(sphereOrigin, radius, groundMask);

        if (_isGrounded)
        {
            _coyoteTimer  = coyoteTime;
            _jumpsUsed    = 0;
            _canDoubleJump= true;
            _wallJumped   = false;

            // Landing resets wall state
            if (_wallState != WallState.None)
                ExitWallState();

            // Kill downward velocity on landing
            if (_verticalVel < 0)
                _verticalVel = -2f; // small negative keeps grounded check stable
        }
        else
        {
            // Coyote time counts down once we leave ground
            if (_wasGrounded && _verticalVel <= 0)
                _coyoteTimer -= Time.deltaTime;
            else
                _coyoteTimer -= Time.deltaTime;
        }
    }

    // ─── Wall Detection ───────────────────────────────────────────────────────

    void UpdateWallDetection()
    {
        // Don't grab walls while grounded or during cooldown
        if (_isGrounded || _wallCooldownTimer > 0)
        {
            if (_wallState != WallState.None && _isGrounded)
                ExitWallState();
            return;
        }

        // Need some horizontal speed to initiate a wall run
        float hSpeed = new Vector3(_velocity.x, 0, _velocity.z).magnitude;

        Vector3[] directions = { transform.right, -transform.right };

        foreach (var dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDist, wallMask))
            {
                // Avoid re-grabbing the same wall immediately
                int hitID = hit.colliderInstanceID;
                if (hitID == _lastWallID && _wallCooldownTimer > 0)
                    continue;

                Vector3 normal  = hit.normal;
                Vector3 forward = Vector3.Cross(normal, Vector3.up).normalized;

                // Align forward direction with current movement direction
                if (Vector3.Dot(forward, transform.forward) < 0)
                    forward = -forward;

                // Check if player is also looking/moving INTO the wall upward (climbing)
                bool movingUp = _moveInput.y > 0.3f && _verticalVel >= -1f;
                bool movingAlong = Mathf.Abs(Vector3.Dot(transform.forward, forward)) > 0.4f;

                if (_wallState == WallState.None)
                {
                    if (movingUp && hSpeed < wallMinSpeed * 0.5f)
                        EnterWallClimb(normal);
                    else if (movingAlong && hSpeed >= wallMinSpeed)
                        EnterWallRun(normal, forward, hitID);
                }
                else
                {
                    // Stay on wall
                    _wallNormal  = normal;
                    _wallForward = forward;
                    _lastWallID  = hitID;
                }

                return;
            }
        }

        // No wall found — exit wall state
        if (_wallState != WallState.None)
            ExitWallState();
    }

    void EnterWallRun(Vector3 normal, Vector3 forward, int wallID)
    {
        _wallState   = WallState.Running;
        _wallNormal  = normal;
        _wallForward = forward;
        _wallTimer   = wallRunDuration;
        _lastWallID  = wallID;
        _jumpsUsed   = 0; // reset double jump while wall running
        _verticalVel = 0;
    }

    void EnterWallClimb(Vector3 normal)
    {
        _wallState   = WallState.Climbing;
        _wallNormal  = normal;
        _wallTimer   = climbDuration;
        _verticalVel = 0;
        _jumpsUsed   = 0;
    }

    void ExitWallState()
    {
        if (_wallState != WallState.None)
        {
            _wallCooldownTimer = wallCooldown;
        }
        _wallState = WallState.None;
        _wallTimer = 0;
    }

    // ─── Jumping ──────────────────────────────────────────────────────────────

    void HandleJump()
    {
        // Tick down jump buffer
        if (_jumpBufferTimer > 0)
            _jumpBufferTimer -= Time.deltaTime;
        else
            _jumpPressed = false;

        if (!_jumpPressed) return;

        // ── Wall Jump ──────────────────────────────────────────────────────────
        if (_wallState != WallState.None)
        {
            PerformWallJump();
            _jumpPressed = false;
            return;
        }

        // ── Normal / Coyote Jump ───────────────────────────────────────────────
        bool canJump = _isGrounded || _coyoteTimer > 0;
        if (canJump && _jumpsUsed == 0)
        {
            _verticalVel = Mathf.Sqrt(-2f * gravity * jumpHeight);
            _jumpsUsed   = 1;
            _coyoteTimer = 0;
            _jumpPressed = false;
            return;
        }

        // ── Double Jump ────────────────────────────────────────────────────────
        if (!_isGrounded && _canDoubleJump && _jumpsUsed < 2)
        {
            _verticalVel  = Mathf.Sqrt(-2f * gravity * doubleJumpHeight);
            _jumpsUsed    = 2;
            _canDoubleJump= false;
            _jumpPressed  = false;
        }
    }

    void PerformWallJump()
    {
        ExitWallState();
        _wallJumped  = true;
        _jumpsUsed   = 1;

        // Push away from wall + upward
        Vector3 jumpDir = (_wallNormal * wallJumpForce + Vector3.up * wallJumpUp);
        _verticalVel    = jumpDir.y;

        // Override horizontal velocity with wall-jump impulse
        _horizontalVel  = _wallNormal * wallJumpForce;

        _wallCooldownTimer = wallCooldown;
    }

    // ─── Sliding ──────────────────────────────────────────────────────────────

    void HandleSlide()
    {
        bool canSlide = _isGrounded && _sprintHeld && _crouchHeld && _moveInput.magnitude > 0.1f;

        if (canSlide && !_isSliding)
        {
            // Only initiate if already sprinting (moving fast)
            float hSpeed = new Vector3(_velocity.x, 0, _velocity.z).magnitude;
            if (hSpeed > walkSpeed)
                StartSlide();
        }

        if (_isSliding)
        {
            _slideTimer -= Time.deltaTime;

            // Exit slide conditions
            if (_slideTimer <= 0 || !_isGrounded || _moveInput.magnitude < 0.05f)
            {
                EndSlide();
                return;
            }

            // Accelerate on downward slope
            Vector3 slopeDir = GetSlopeDirection();
            float   slope    = Vector3.Dot(slopeDir, -Vector3.up);

            float speed = slideSpeed + (slope > 0 ? slope * slopeSlideMult * 10f : 0);
            _horizontalVel = _slideDir * speed;
        }
    }

    void StartSlide()
    {
        _isSliding  = true;
        _slideTimer = slideDuration;
        _slideDir   = new Vector3(_velocity.x, 0, _velocity.z).normalized;

        // Crouch the character controller
        _cc.height  = _slideHeight;
        _cc.center  = new Vector3(0, _slideHeight * 0.5f, 0);
    }

    void EndSlide()
    {
        _isSliding = false;

        // Restore height (check for ceiling before standing)
        _cc.height = _defaultHeight;
        _cc.center = new Vector3(0, _defaultHeight * 0.5f, 0);
    }

    Vector3 GetSlopeDirection()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f, groundMask))
            return Vector3.ProjectOnPlane(Vector3.forward, hit.normal).normalized;
        return Vector3.zero;
    }

    // ─── Horizontal Movement ──────────────────────────────────────────────────

    void ApplyMovement()
    {
        if (_isSliding) return; // sliding controls its own velocity

        // Target speed
        float targetSpeed = _sprintHeld ? sprintSpeed : walkSpeed;

        // Input direction relative to camera (project onto XZ)
        Vector3 camFwd   = Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(_cam.right,   Vector3.up).normalized;
        Vector3 inputDir = (camFwd * _moveInput.y + camRight * _moveInput.x).normalized;

        // ── Wall Run overrides movement ────────────────────────────────────────
        if (_wallState == WallState.Running)
        {
            _wallTimer -= Time.deltaTime;
            if (_wallTimer <= 0) { ExitWallState(); return; }

            _horizontalVel = _wallForward * wallRunSpeed;
            return;
        }

        // ── Wall Climb overrides movement ─────────────────────────────────────
        if (_wallState == WallState.Climbing)
        {
            _wallTimer -= Time.deltaTime;
            if (_wallTimer <= 0) { ExitWallState(); return; }

            _horizontalVel = Vector3.zero;
            _verticalVel   = climbSpeed;
            return;
        }

        // ── Ground / Air movement ─────────────────────────────────────────────
        float accel    = _isGrounded ? groundAccel   : airAccel;
        float friction = _isGrounded ? groundFriction : airFriction;

        if (inputDir.magnitude > 0.05f)
        {
            Vector3 target = inputDir * targetSpeed;
            _horizontalVel = Vector3.MoveTowards(_horizontalVel, target, accel * Time.deltaTime);
        }
        else
        {
            // Decelerate
            _horizontalVel = Vector3.MoveTowards(_horizontalVel, Vector3.zero, friction * Time.deltaTime);
        }

        // Cap speed (allow slight overshoot from wall jumps / slides)
        float maxSpeed = _isGrounded ? targetSpeed : Mathf.Max(targetSpeed * 1.3f, _horizontalVel.magnitude);
        if (_horizontalVel.magnitude > maxSpeed && _isGrounded)
            _horizontalVel = _horizontalVel.normalized * maxSpeed;
    }

    // ─── Gravity ──────────────────────────────────────────────────────────────

    void ApplyGravity()
    {
        if (_isGrounded && _verticalVel < 0)
        {
            _verticalVel = -2f;
            return;
        }

        if (_wallState == WallState.Running)
        {
            _verticalVel = Mathf.MoveTowards(_verticalVel, wallRunGravity, 20f * Time.deltaTime);
            return;
        }

        if (_wallState == WallState.Climbing)
        {
            // Climbing sets verticalVel in ApplyMovement
            return;
        }

        // Apply extra downward force when falling (snappier arc)
        float gScale = (_verticalVel < 0) ? fallMultiplier : 1f;
        _verticalVel += gravity * gScale * Time.deltaTime;
    }

    // ─── Final Move ───────────────────────────────────────────────────────────

    void MoveCharacter()
    {
        _velocity = _horizontalVel + Vector3.up * _verticalVel;
        _cc.Move(_velocity * Time.deltaTime);
    }

    void HandleCooldowns()
    {
        if (_wallCooldownTimer > 0)
            _wallCooldownTimer -= Time.deltaTime;
    }

    // ─── Public Accessors (used by camera tilt, UI, etc.) ────────────────────

    public bool        IsGrounded    => _isGrounded;
    public bool        IsSliding     => _isSliding;
    public WallState   CurrentWall   => _wallState;
    public Vector3     WallNormal    => _wallNormal;
    public float       Speed         => _velocity.magnitude;
    public Vector3     HorizontalVel => _horizontalVel;
}
