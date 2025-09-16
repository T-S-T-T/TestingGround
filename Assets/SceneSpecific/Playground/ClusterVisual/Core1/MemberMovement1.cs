using UnityEngine;

public class MemberMovement1 : MonoBehaviour
{
    [HideInInspector]
    public GameObject core;           // Assigned by your spawner

    private ClusterVisual1 cluster;   // Holds maxRange (and spacing, if you add it)
    private Vector3 offset;           // Initial grid offset from core

    private bool moveReady = true;

    public float distance;

    void Start()
    {
        // Cache the core’s ClusterVisual1 for range (and spacing) data
        cluster = core.GetComponent<ClusterVisual1>();
    }

    void Update()
    {

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
