using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class SoftBodyGenerator : MonoBehaviour
{
    [Header("Soft-Body Settings")]
    private readonly int nodeCount = 50;
    private readonly float radius = 0.5f;
    private readonly float nodeMass = 0.001f;
    private readonly float spring = 100f;
    private readonly float springSurface = 1000f;
    private readonly float damper = 5f;

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

        centralNode.linearDamping = 0f;
    }

    // 2) Distribute nodes on sphere via golden?ratio method
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
            rb.freezeRotation = true;

            rb.linearDamping = 0f;
            nodes.Add(rb);

            // small collider to hit the ground
            var col = nodeGO.AddComponent<SphereCollider>();
            col.radius = 0.04f;
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
            sj.minDistance = radius;
            sj.maxDistance = radius;
            sj.tolerance = 0.005f;
            sj.autoConfigureConnectedAnchor = false;

            // track that (central, rb) is “connected” if needed later
            connectedPair.Add((-1, nodes.IndexOf(rb)));
        }
    }

    // 4) For each node A, find its closest neighbor B; if not already connected, attach spring
    void ConnectNearestNeighborSprings()
    {
        int N = nodes.Count;
        int k = 5;  // number of nearest neighbors to connect

        for (int i = 0; i < N; i++)
        {
            // build list of distances to all other nodes
            List<(float dist, int idx)> distances = new List<(float, int)>(N - 1);
            for (int j = 0; j < N; j++)
            {
                if (i == j) continue;
                distances.Add((Vector3.Distance(nodes[i].position, nodes[j].position), j));
            }

            // sort ascending by distance
            distances.Sort((a, b) => a.dist.CompareTo(b.dist));

            // take up to k closest neighbors
            int neighbors = Mathf.Min(k, distances.Count);
            for (int n = 0; n < neighbors; n++)
            {
                int j = distances[n].idx;
                var pair = (Math.Min(i, j), Math.Max(i, j));
                if (connectedPair.Contains(pair))
                    continue;

                float restDist = distances[n].dist;
                var sj = nodes[i].gameObject.AddComponent<SpringJoint>();
                sj.connectedBody = nodes[j];
                sj.spring = springSurface;
                sj.damper = damper;
                sj.minDistance = restDist * 0.95f;
                sj.maxDistance = restDist * 1.05f;
                sj.autoConfigureConnectedAnchor = false;

                // mark pair so we never double-connect
                connectedPair.Add(pair);
            }
        }
    }

    // Draw every SpringJoint as a line between its two bodies
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var sj in GetComponentsInChildren<SpringJoint>())
        {
            if (sj.connectedBody != null)
            {
                Gizmos.DrawLine(sj.transform.position, sj.connectedBody.transform.position);
            }
        }
    }
}
