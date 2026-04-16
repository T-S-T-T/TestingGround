using UnityEngine;

/// <summary>
/// First-person camera controller with wall-run tilt.
/// Attach to the Camera. The parent GameObject should be the player.
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] float sensitivityX  = 2.5f;
    [SerializeField] float sensitivityY  = 2.5f;

    [Header("Clamping")]
    [SerializeField] float minPitch = -85f;
    [SerializeField] float maxPitch =  85f;

    [Header("Wall Run Tilt")]
    [SerializeField] float wallTiltAngle = 18f;   // degrees of camera roll
    [SerializeField] float tiltSpeed     = 8f;     // lerp speed for tilt

    [Header("FOV")]
    [SerializeField] float defaultFOV    = 90f;
    [SerializeField] float sprintFOV     = 98f;
    [SerializeField] float wallRunFOV    = 102f;
    [SerializeField] float fovChangeSpeed= 6f;

    // ─── Internal ─────────────────────────────────────────────────────────────

    PlayerMovement _player;
    Camera         _camera;

    float _pitch;       // X rotation (up/down)
    float _yaw;         // Y rotation (left/right) — applied to player body
    float _currentRoll; // Z rotation for wall tilt

    void Awake()
    {
        _player = GetComponentInParent<PlayerMovement>();
        _camera = GetComponent<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _camera.fieldOfView = defaultFOV;
    }

    void Update()
    {
        HandleMouseLook();
        HandleCameraRoll();
        HandleFOV();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivityX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY;

        _yaw   += mouseX;
        _pitch -= mouseY;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // Rotate the player body left/right
        _player.transform.rotation = Quaternion.Euler(0, _yaw, 0);

        // Rotate the camera up/down (no roll — that's applied separately)
        transform.localRotation = Quaternion.Euler(_pitch, 0, _currentRoll);
    }

    void HandleCameraRoll()
    {
        float targetRoll = 0f;

        if (_player.CurrentWall == PlayerMovement.WallState.Running)
        {
            // Tilt toward the wall
            float dot = Vector3.Dot(_player.transform.right, _player.WallNormal);
            targetRoll = -wallTiltAngle * Mathf.Sign(dot);
        }

        _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, tiltSpeed * Time.deltaTime);
    }

    void HandleFOV()
    {
        float targetFOV = defaultFOV;

        if (_player.CurrentWall == PlayerMovement.WallState.Running)
            targetFOV = wallRunFOV;
        else if (_player.Speed > 10f)
            targetFOV = Mathf.Lerp(defaultFOV, sprintFOV, (_player.Speed - 10f) / 6f);

        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
    }
}
