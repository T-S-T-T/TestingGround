using System.Collections.Generic;
using UnityEngine;

public class ClusterVisual1 : MonoBehaviour
{
    [Header("Cluster Settings")]
    public GameObject memberPrefab;   // Prefab with MemberMovement1 attached
    public float spacing = 1f;        // Grid spacing
    public float maxDistance = 5f;    // Radius around core

    private List<MemberMovement1> members = new List<MemberMovement1>();
    private HashSet<Vector3> occupied = new HashSet<Vector3>();

    void Start()
    {
        // Pre-create members to cover the maximum possible grid
        int steps = Mathf.CeilToInt(maxDistance / spacing);
        int count = 0;
        for (int i = -steps; i <= steps; i++)
        {
            for (int j = -steps; j <= steps; j++)
            {
                for (int k = -steps; k <= steps; k++)
                {
                    count++;
                    Vector3 point = new Vector3(i * spacing, j * spacing, k * spacing);

                    // Only spawn if inside the spherical radius
                    if (point.sqrMagnitude <= maxDistance * maxDistance)
                    {
                        // Spawn at the grid position relative to the core
                        Vector3 spawnPos = transform.position + point;

                        GameObject obj = Instantiate(memberPrefab, spawnPos, Quaternion.identity);

                        var mover = obj.GetComponent<MemberMovement1>();
                        if (mover != null)
                        {
                            members.Add(mover);
                        }
                    }
                }
            }
        }
        Debug.Log(count);
    }

    void Update()
    {
        RunVisualFunction();
    }

    void RunVisualFunction()
    {
        Vector3 core = SnapToGrid(transform.position);

        // Lists for recycling
        List<MemberMovement1> waitList = new List<MemberMovement1>();
        List<Vector3> fillList = new List<Vector3>();

        occupied.Clear();

        // Step 1: classify members
        foreach (var m in members)
        {
            float dist = Vector3.Distance(m.transform.position, core);
            if (dist > maxDistance)
            {
                waitList.Add(m);
            }
            else
            {
                occupied.Add(m.transform.position);
            }
        }

        // Step 2: find grid positions that should be filled
        int steps = Mathf.CeilToInt(maxDistance / spacing);
        for (int x = -steps; x <= steps; x++)
        {
            for (int y = -steps; y <= steps; y++)
            {
                for (int z = -steps; z <= steps; z++)
                {
                    Vector3 offset = new Vector3(x, y, z) * spacing;
                    Vector3 pos = core + offset;

                    if (offset.magnitude <= maxDistance)
                    {
                        if (!occupied.Contains(pos))
                        {
                            fillList.Add(pos);
                        }
                    }
                }
            }
        }

        // (Optional) sort lists
        waitList.Sort((a, b) =>
            Vector3.Distance(b.transform.position, core).CompareTo(Vector3.Distance(a.transform.position, core)));
        fillList.Sort((a, b) =>
            Vector3.Distance(a, core).CompareTo(Vector3.Distance(b, core)));

        // Step 3: move members from wait → fill
        int count = Mathf.Min(waitList.Count, fillList.Count);
        for (int i = 0; i < count; i++)
        {
            waitList[i].Move(fillList[i]);
        }
    }

    Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / spacing) * spacing,
            Mathf.Round(pos.y / spacing) * spacing,
            Mathf.Round(pos.z / spacing) * spacing
        );
    }
}
