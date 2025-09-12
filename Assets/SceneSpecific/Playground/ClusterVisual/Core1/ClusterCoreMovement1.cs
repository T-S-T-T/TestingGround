using UnityEngine;

public class ClusterCoreMovement1 : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;                     // Movement speed in units/sec

    [Header("Axis Bounds")]
    public float XMin = -10f;
    public float XMax = 10f;
    public float YMin = 0f;
    public float YMax = 5f;
    public float ZMin = -8f;
    public float ZMax = 8f;

    // Internal movement direction
    private Vector3 direction;

    void Start()
    {
        // Random values on each axis, then normalize to unit length
        Vector3 raw = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        );

        // If raw is ever (0,0,0), pick a default
        direction = raw != Vector3.zero ? raw.normalized : Vector3.forward;
    }

    void Update()
    {
        // Move the object
        transform.position += direction * speed * Time.deltaTime;

        Vector3 pos = transform.position;

        // Bounce on X
        if (pos.x < XMin || pos.x > XMax)
        {
            direction.x = -direction.x;
            pos.x = Mathf.Clamp(pos.x, XMin, XMax);
        }

        // Bounce on Y
        if (pos.y < YMin || pos.y > YMax)
        {
            direction.y = -direction.y;
            pos.y = Mathf.Clamp(pos.y, YMin, YMax);
        }

        // Bounce on Z
        if (pos.z < ZMin || pos.z > ZMax)
        {
            direction.z = -direction.z;
            pos.z = Mathf.Clamp(pos.z, ZMin, ZMax);
        }

        transform.position = pos;
    }

    // Visualize bounds and current direction in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw the boundary box
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(
            (XMin + XMax) * 0.5f,
            (YMin + YMax) * 0.5f,
            (ZMin + ZMax) * 0.5f
        );
        Vector3 size = new Vector3(
            Mathf.Abs(XMax - XMin),
            Mathf.Abs(YMax - YMin),
            Mathf.Abs(ZMax - ZMin)
        );
        Gizmos.DrawWireCube(center, size);

        // Draw movement direction arrow
        Gizmos.color = Color.red;
        // Ensure we have a direction (in Edit mode, Start() may not run)
        Vector3 dirNorm = direction == Vector3.zero
            ? Vector3.forward
            : direction.normalized;
        Vector3 start = transform.position;
        float arrowLength = Mathf.Min(size.x, size.y, size.z) * 0.4f;
        Vector3 end = start + dirNorm * arrowLength;

        // Main arrow line
        Gizmos.DrawLine(start, end);

        // Arrowhead (two simple lines)
        Vector3 right = Quaternion.LookRotation(dirNorm) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(dirNorm) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        Gizmos.DrawLine(end, end + right * (arrowLength * 0.2f));
        Gizmos.DrawLine(end, end + left * (arrowLength * 0.2f));
    }
}
