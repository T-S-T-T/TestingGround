using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class SoftBodyGenerator : MonoBehaviour
{
    [Header("Soft-Body Settings")]
    //public int nodeCount = 100;
    //public float radius = 0.5f;
    //public float nodeMass = 0.1f;
    //public float spring = 200f;
    //public float damper = 5f;
    private int nodeCount = 100;
    private float radius = 0.5f;
    private float nodeMass = 0.1f;
    private float spring = 200f;
    private float damper = 5f;
    public GameObject nodePrefab;  // an empty GameObject or tiny mesh without Rigidbody/Collider

    private List<Rigidbody> nodes = new List<Rigidbody>();

    void Start()
    {
        GenerateNodes();
        ConnectSprings();
        Debug.Log($"Created {GetComponentsInChildren<SpringJoint>().Length} springs");
    }

    void GenerateNodes()
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
            Vector3 pos = transform.position + new Vector3(x, y, z) * radius;

            GameObject node = Instantiate(nodePrefab, pos, Quaternion.identity, transform);
            Rigidbody rb = node.AddComponent<Rigidbody>();
            rb.mass = nodeMass;
            rb.linearDamping = 0.5f;
            nodes.Add(rb);

            SphereCollider col = node.AddComponent<SphereCollider>();
            col.radius = 0.08f;  // increase if nodes tunnel through ground
        }
    }

    void ConnectSprings()
    {
        int N = nodes.Count;
        bool[] connected = new bool[N];

        for (int i = 0; i < N; i++)
        {
            // if this node already has a spring, skip it
            if (connected[i]) continue;

            float minDist = float.MaxValue;
            int closest = -1;

            // find the nearest neighbor that isn't yet connected
            for (int j = 0; j < N; j++)
            {
                if (j == i || connected[j]) continue;
                float d = Vector3.Distance(nodes[i].position, nodes[j].position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = j;
                }
            }

            // if we found a valid neighbor, link them
            if (closest != -1)
            {
                SpringJoint sj = nodes[i].gameObject.AddComponent<SpringJoint>();
                sj.connectedBody = nodes[closest];
                sj.spring = spring;
                sj.damper = damper;
                sj.minDistance = minDist * 0.9f;
                sj.maxDistance = minDist * 1.1f;

                connected[i] = true;
                connected[closest] = true;
            }
        }
    }
}
