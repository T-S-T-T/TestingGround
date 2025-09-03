using UnityEngine;

/// <summary>
/// Attach this to the central Rigidbody node.
/// Reads WASD/arrow input and applies force in camera-relative directions.
/// </summary>
public class CentralNodeController : MonoBehaviour
{
    [Tooltip("Force applied per frame based on input")]
    public float moveForce = 10f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // read standard axes
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f))
            return;

        // project the main camera’s forward/right onto the ground plane
        var cam = Camera.main.transform;
        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // compute and apply force
        Vector3 dir = forward * v + right * h;
        rb.AddForce(dir * moveForce, ForceMode.Force);
    }
}
