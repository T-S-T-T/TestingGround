using UnityEngine;

public class MemberMovement1 : MonoBehaviour
{
    [HideInInspector]
    public GameObject core;
    public float maxRange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ClusterVisual1 clusterVisual1 = core.GetComponent<ClusterVisual1>();
        maxRange = clusterVisual1.maxRange;
    }
}
