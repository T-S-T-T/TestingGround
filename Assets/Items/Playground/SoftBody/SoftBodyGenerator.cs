using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class SoftBodyGenerator : MonoBehaviour
{
    [Header("Soft-Body Settings")]
    public int nodeCount = 200;
    public float radius = 1f;
    public float nodeMass = 0.1f;
    public float springCentral = 100f;   // spring strength back to the center
    public float springSurface = 1000f;  // spring strength between neighbors
    public float damper = 5f;

    [Header("Connectivity")]
    [Tooltip("Number of nearest neighbors to connect")]
    public int kNearest = 7;
    [Tooltip("Number of furthest neighbors to connect")]
    public int kFurthest = 3;

    [Header("Prefabs")]
    public GameObject nodePrefab;  // must have NO Rigidbody/Collider

    private Rigidbody centralNode;
    private List<Rigidbody> nodes = new List<Rigidbody>();
    private HashSet<(int, int)> connectedPair = new HashSet<(int, int)>();

    void Start()
    {
        CreateCentralNodeWithCamera();
        GenerateSurfaceNodes();
        ConnectCentralSprings();      // ← back to center
        ConnectNeighborSprings();     // ← nearest & furthest
        Debug.Log($"Total SpringJoints: {GetComponentsInChildren<SpringJoint>().Length}");
    }

    // 1) Central Rigidbody + Cinemachine setup
    void CreateCentralNodeWithCamera()
    {
        // ensure CinemachineBrain on main camera
        var mainCam = Camera.main;
        if (mainCam && mainCam.GetComponent<CinemachineBrain>() == null)
            mainCam.gameObject.AddComponent<CinemachineBrain>();

        // central physics body
        var go = new GameObject("CentralNode");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        centralNode = go.AddComponent<Rigidbody>();
        centralNode.mass = nodeMass * nodeCount;
        centralNode.linearDamping = 0f;
        centralNode.freezeRotation = true;

        // movement controller
        var controller = go.AddComponent<CentralNodeController>();
        controller.moveForce = 100f;

        // Cinemachine virtual camera (new API)
        var vcamGO = new GameObject("CM vcam");
        vcamGO.transform.SetParent(go.transform);
        vcamGO.transform.localPosition = new Vector3(0f, 2f, -4f);
        vcamGO.transform.localRotation = Quaternion.identity;
    }

    // 2) Spawn all nodes on a sphere surface
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

            var nodeGO = Instantiate(nodePrefab, pos, Quaternion.identity, transform);

            // add physics
            var rb = nodeGO.AddComponent<Rigidbody>();
            rb.mass = nodeMass;
            rb.freezeRotation = true;
            rb.linearDamping = 0f;
            nodes.Add(rb);

            // collider so it bumps the world
            var col = nodeGO.AddComponent<SphereCollider>();
            col.radius = 0.04f;
        }
    }

    // 3) Connect each node back to the central node
    void ConnectCentralSprings()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var rb = nodes[i];
            var sj = rb.gameObject.AddComponent<SpringJoint>();
            sj.connectedBody = centralNode;
            sj.spring = springCentral;
            sj.damper = damper;
            sj.minDistance = radius;
            sj.maxDistance = radius;
            sj.tolerance = 0.005f;
            sj.autoConfigureConnectedAnchor = false;

            connectedPair.Add((-1, i));
        }
    }

    // 4) Connect k nearest and k furthest neighbor springs
    void ConnectNeighborSprings()
    {
        int N = nodes.Count;
        for (int i = 0; i < N; i++)
        {
            // build distance list to all other nodes
            var list = new List<(float dist, int idx)>(N - 1);
            for (int j = 0; j < N; j++)
            {
                if (i == j) continue;
                float d = Vector3.Distance(nodes[i].position, nodes[j].position);
                list.Add((d, j));
            }

            list.Sort((a, b) => a.dist.CompareTo(b.dist));

            // k nearest (surface tension)
            int nearCount = Mathf.Min(kNearest, list.Count);
            for (int n = 0; n < nearCount; n++)
                AddSpringBetween(i, list[n].idx, list[n].dist, springSurface);

            // k furthest (volume retention)
            int farCount = Mathf.Min(kFurthest, list.Count);
            for (int n = 0; n < farCount; n++)
            {
                int idx = list.Count - 1 - n;
                AddSpringBetween(i, list[idx].idx, list[idx].dist, springSurface);
            }
        }
    }

    // Helper: create a spring if that pair isn’t already connected
    void AddSpringBetween(int a, int b, float restLength, float stiffness)
    {
        var pair = (Math.Min(a, b), Math.Max(a, b));
        if (connectedPair.Contains(pair)) return;

        var sj = nodes[a].gameObject.AddComponent<SpringJoint>();
        sj.connectedBody = nodes[b];
        sj.spring = stiffness;
        sj.damper = damper;
        sj.minDistance = restLength * 0.95f;
        sj.maxDistance = restLength * 1.05f;
        sj.autoConfigureConnectedAnchor = false;

        connectedPair.Add(pair);
    }

    // debug: draw all springs in the editor
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
