using UnityEngine;

public class MemberMovement1 : MonoBehaviour
{
    [HideInInspector]
    public GameObject core;           // Assigned by your spawner

    private ClusterVisual1 cluster;   // Holds maxRange (and spacing, if you add it)
    private Vector3 offset;           // Initial grid offset from core

    public float distance;

    void Start()
    {
        // Cache the core’s ClusterVisual1 for range (and spacing) data
        cluster = core.GetComponent<ClusterVisual1>();
    }

    void Update()
    {

    }

    public void Move(Vector3 targetPosition)
    {
        transform.position = targetPosition;
    }
}
