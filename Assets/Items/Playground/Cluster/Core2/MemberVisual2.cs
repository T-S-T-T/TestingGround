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

        float dist = Vector3.Distance(transform.position, core.transform.position);

        float min = clusterVisual2.minDistance;
        float max = clusterVisual2.maxDistance;
        float spacing = clusterVisual2.spacing;

        // Shrink the range by 10% on both sides
        float innerMin = min + spacing;
        float innerMax = max - spacing;

        // Scale factor: 1 at innerMin, 0 at innerMax
        float t = 1f - Mathf.InverseLerp(innerMin, innerMax, dist);

        transform.localScale = Vector3.one * t;
    }

}
