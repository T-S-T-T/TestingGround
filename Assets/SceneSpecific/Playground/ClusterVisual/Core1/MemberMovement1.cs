using UnityEngine;

public class MemberMovement1 : MonoBehaviour
{
    [HideInInspector]
    public GameObject core;           // Assigned by your spawner

    private ClusterVisual1 cluster;   // Holds maxRange (and spacing, if you add it)
    private Vector3 offset;           // Initial grid offset from core

    private bool moveReady = true;

    void Start()
    {
        // Cache the core’s ClusterVisual1 for range (and spacing) data
        cluster = core.GetComponent<ClusterVisual1>();
    }

    void Update()
    {
        // Fast check using squared magnitudes
        float r = cluster.maxRange;
        float s = cluster.spacing;
        if ((transform.position - core.transform.position).sqrMagnitude > r * r)
        {
            if (moveReady)
            {
                Vector3 center = RoundPosition(core.transform.position, s);
                // 2) Compute offset from that snapped center
                Vector3 offset = transform.position - center;
                Vector3 mirroredOffset = -offset;
                transform.position = center + mirroredOffset;

                moveReady = false;
            }

        }
        else moveReady = true;
    }

    public static Vector3 RoundPosition(Vector3 currentPos, float spacing)
    {
        if (spacing == 0f)
            return currentPos;

        // Precompute reciprocal for a tiny performance gain
        float inv = 1f / spacing;

        float x = Mathf.Round(currentPos.x * inv) / inv;
        float y = Mathf.Round(currentPos.y * inv) / inv;
        float z = Mathf.Round(currentPos.z * inv) / inv;

        return new Vector3(x, y, z);
    }
}
