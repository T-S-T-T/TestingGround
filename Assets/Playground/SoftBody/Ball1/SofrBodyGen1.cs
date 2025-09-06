using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SoftBodyController : MonoBehaviour
{
    [Header("Soft-Body Settings")]
    public int nodeCount = 200;
    public float radius = 1f;
    public float nodeMass = 0.1f;
    public float springCentral = 500f;
    public float springSurface = 100f;
    public float damper = 5f;

    [Header("Connectivity")]
    public int kNearest = 7;
    //public int kFurthest = 3;

    [Header("Prefabs")]
    public GameObject nodePrefab;

    private Rigidbody centralRb;
    private List<Rigidbody> nodes = new List<Rigidbody>();
    private HashSet<(int, int)> connectedPairs = new HashSet<(int, int)>();

    void Awake()
    {
        centralRb = GetComponent<Rigidbody>();
        centralRb.mass = nodeMass * nodeCount;
        centralRb.freezeRotation = true;
        centralRb.linearDamping = 0f;
    }

    void Start()
    {
        GenerateSurfaceNodes();
        ConnectToCentral();
        ConnectNeighborSprings();
        Debug.Log($"Total SpringJoints: {GetComponentsInChildren<SpringJoint>().Length}");
    }

    void GenerateSurfaceNodes()
    {
        float offset = 2f / nodeCount;
        float increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < nodeCount; i++)
        {
            float y = ((i * offset) - 1f) + (offset / 2f);
            float r = Mathf.Sqrt(1f - y * y);
            float phi = i * increment;
            Vector3 pos = transform.position
                        + new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r)
                        * radius;

            var go = Instantiate(nodePrefab, pos, Quaternion.identity, transform);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = nodeMass;
            rb.freezeRotation = true;
            rb.linearDamping = 0f;
            nodes.Add(rb);

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.04f;
        }
    }

    void ConnectToCentral()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var sj = nodes[i].gameObject.AddComponent<SpringJoint>();
            sj.connectedBody = centralRb;
            sj.spring = springCentral;
            sj.damper = damper;
            sj.minDistance = radius;
            sj.maxDistance = radius;
            sj.tolerance = 0.005f;
            sj.autoConfigureConnectedAnchor = false;
            connectedPairs.Add((-1, i));
        }
    }

    void ConnectNeighborSprings()
    {
        int N = nodes.Count;
        for (int i = 0; i < N; i++)
        {
            var list = new List<(float dist, int idx)>(N - 1);
            for (int j = 0; j < N; j++)
            {
                if (i == j) continue;
                float d = Vector3.Distance(nodes[i].position, nodes[j].position);
                list.Add((d, j));
            }
            list.Sort((a, b) => a.dist.CompareTo(b.dist));

            int near = Mathf.Min(kNearest, list.Count);
            for (int n = 0; n < near; n++)
                AddSpring(i, list[n].idx, list[n].dist, springSurface);

            //int far = Mathf.Min(kFurthest, list.Count);
            //for (int n = 0; n < far; n++)
            //{
            //    int idx = list.Count - 1 - n;
            //    AddSpring(i, list[idx].idx, list[idx].dist, springSurface);
            //}
        }
    }

    void AddSpring(int a, int b, float restLen, float stiffness)
    {
        var key = (Math.Min(a, b), Math.Max(a, b));
        if (connectedPairs.Contains(key)) return;

        var sj = nodes[a].gameObject.AddComponent<SpringJoint>();
        sj.connectedBody = nodes[b];
        sj.spring = stiffness;
        sj.damper = damper;
        sj.minDistance = restLen * 0.95f;
        sj.maxDistance = restLen * 1.05f;
        sj.autoConfigureConnectedAnchor = false;
        connectedPairs.Add(key);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var sj in GetComponentsInChildren<SpringJoint>())
        {
            if (sj.connectedBody != null)
                Gizmos.DrawLine(sj.transform.position, sj.connectedBody.transform.position);
        }
    }
}
