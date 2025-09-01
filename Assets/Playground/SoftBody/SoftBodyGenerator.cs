using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class SoftBodyGenerator : MonoBehaviour
{
    [Header("Soft-Body Settings")]
    public int nodeCount = 100;
    public float radius = 0.5f;
    public float nodeMass = 0.1f;
    public float spring = 200f;
    public float damper = 5f;
    public GameObject nodePrefab;  // simple empty GameObject or tiny mesh without Rigidbody/Collider

    private Rigidbody centralNode;
    private List<Rigidbody> nodes = new List<Rigidbody>();
    private HashSet<(int, int)> connectedPair = new HashSet<(int, int)>();

    void Start()
    {
        GenerateCentralNode();
        GenerateSurfaceNodes();
        ConnectCentralSprings();
        ConnectNearestNeighborSprings();
        Debug.Log($"Total SpringJoints: {GetComponentsInChildren<SpringJoint>().Length}");
    }

    // 1) Create a rigidbody at center; applying force here moves the whole ball
    void GenerateCentralNode()
    {
        var centralGO = new GameObject("CentralNode");
        centralGO.transform.SetParent(transform);
        centralGO.transform.localPosition = Vector3.zero;

        centralNode = centralGO.AddComponent<Rigidbody>();
        centralNode.mass = nodeMass * nodeCount;  // heavier so surface nodes drag it
        centralNode.drag = 0.5f;
    }

    // 2) Distribute nodes on sphere via golden‐ratio method
    void GenerateSurfaceNodes()
    {
        float offset = 2f / nodeCount;
        float increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < nodeCount; i++)
        {
            float y = ((i * offset) - 1f) + (offset / 2f);
            float r = Mathf.Sqrt(1f - y * y);
            float phi = i * increment;

            float x = Mathf.Cos(phi) * r;
            float z = Mathf.Sin(phi) * r;
            Vector3 worldPos = transform.position + new Vector3(x, y, z) * radius;

            var nodeGO = Instantiate(nodePrefab, worldPos, Quaternion.identity, transform);
            var rb = nodeGO.AddComponent<Rigidbody>();
            rb.mass = nodeMass;
            rb.drag = 0.5f;
            nodes.Add(rb);

            // small collider to hit the ground
            var col = nodeGO.AddComponent<SphereCollider>();
            col.radius = 0.08f;
        }
    }

    // 3) For each surface node, attach one spring to the central node
    void ConnectCentralSprings()
    {
        foreach (var rb in nodes)
        {
            float restLen = Vector3.Distance(rb.position, centralNode.position);
            var sj = rb.gameObject.AddComponent<SpringJoint>();
            sj.connectedBody = centralNode;
            sj.spring = spring;
            sj.damper = damper;
            sj.minDistance = restLen * 0.9f;
            sj.maxDistance = restLen * 1.1f;

            // track that (central, rb) is “connected” if needed later
            connectedPair.Add((-1, nodes.IndexOf(rb)));
        }
    }

    // 4) For each node A, find its closest neighbor B; if not already connected, attach spring
    void ConnectNearestNeighborSprings()
    {
        int N = nodes.Count;
        for (int i = 0; i < N; i++)
        {
            float minDist = float.MaxValue;
            int bestJ = -1;

            // find nearest neighbor not yet paired with i
            for (int j = 0; j < N; j++)
            {
                if (i == j) continue;
                var pair = (Math.Min(i, j), Math.Max(i, j));
                if (connectedPair.Contains(pair)) continue;

                float d = Vector3.Distance(nodes[i].position, nodes[j].position);
                if (d < minDist)
                {
                    minDist = d;
                    bestJ = j;
                }
            }

            if (bestJ >= 0)
            {
                // create spring A→B
                var sj = nodes[i].gameObject.AddComponent<SpringJoint>();
                sj.connectedBody = nodes[bestJ];
                sj.spring = spring;
                sj.damper = damper;
                //sj.minDistance = minDist * 0.9f;
                //sj.maxDistance = minDist * 1.1f;

                // mark pair so we never double-connect
                connectedPair.Add((Math.Min(i, bestJ), Math.Max(i, bestJ)));
            }
        }
    }
}
