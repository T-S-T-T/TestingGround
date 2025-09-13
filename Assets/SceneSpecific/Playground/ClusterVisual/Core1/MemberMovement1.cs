using UnityEngine;

public class MemberMovement1 : MonoBehaviour
{
    [HideInInspector]
    public GameObject core;           // Assigned by your spawner

    private ClusterVisual1 cluster;   // Holds maxRange (and spacing, if you add it)
    private Vector3 offset;           // Initial grid offset from core

    void Start()
    {
        // Cache the core’s ClusterVisual1 for range (and spacing) data
        cluster = core.GetComponent<ClusterVisual1>();

        // Compute and store the member’s original offset on the grid
        offset = transform.position - core.transform.position;
    }

    void Update()
    {
        // Fast check using squared magnitudes
        float r = cluster.maxRange;
        if ((transform.position - core.transform.position).sqrMagnitude > r * r)
        {
            // Invert the stored offset to wrap to the opposite side
            offset = -offset;

            // Teleport to the new wrapped-around position
            transform.position = core.transform.position + offset;
        }

        // You can still use maxRange elsewhere in this script if needed
    }
}
