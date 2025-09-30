using UnityEngine;

public class MemberVisual2 : MonoBehaviour
{

    [HideInInspector] public GameObject core;
    [HideInInspector] public ClusterVisual2 clusterVisual2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        clusterVisual2 = core.GetComponent<ClusterVisual2>();
    }

    // Update is called once per frame
    void Update()
    {
        if (core == null || clusterVisual2 == null) return;

        // Distance from this member to the core
        float dist = Vector3.Distance(transform.position, core.transform.position);

        // Get latest min/max from the cluster
        float min = clusterVisual2.minDistance;
        float max = clusterVisual2.maxDistance;

        // Scale factor: 1 at min, 0 at max
        float t = 1f - Mathf.InverseLerp(min, max, dist);

        // Apply scale uniformly
        transform.localScale = Vector3.one * t;
    }
}
