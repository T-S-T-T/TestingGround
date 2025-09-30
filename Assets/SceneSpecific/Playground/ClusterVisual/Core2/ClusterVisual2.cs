using System.Collections.Generic;
using UnityEngine;

public class ClusterVisual2 : MonoBehaviour
{
    [Header("Cluster Settings")]
    public GameObject memberPrefab;
    public int memberCount = 10;
    public float minDistance = 1f;
    public float maxDistance = 5f;
    public float spacing = 1f;

    private Vector3 lastSnappedPos;
    private List<GameObject> members = new List<GameObject>();

    void Start()
    {
        lastSnappedPos = SnapToGrid(transform.position);

        SpawnMembers();
    }

    void Update()
    {
        Vector3 snappedPos = SnapToGrid(transform.position);

        if (snappedPos != lastSnappedPos)
        {
            MoveMembers(snappedPos - lastSnappedPos);
            lastSnappedPos = snappedPos;
        }
    }

    // --- Spawn members around the core ---
    void SpawnMembers()
    {
        // Build a list of all valid grid points
        List<Vector3> validOffsets = new List<Vector3>();
        int maxSteps = Mathf.CeilToInt(maxDistance / spacing);

        for (int x = -maxSteps; x <= maxSteps; x++)
        {
            for (int y = -maxSteps; y <= maxSteps; y++)
            {
                for (int z = -maxSteps; z <= maxSteps; z++)
                {
                    Vector3 offset = new Vector3(x * spacing, y * spacing, z * spacing);
                    float dist = offset.magnitude;

                    if (dist >= minDistance && dist <= maxDistance)
                    {
                        validOffsets.Add(offset);
                    }
                }
            }
        }

        Debug.Log("Valid postion in the cluster: "+ validOffsets.Count);

        // Shuffle the list so we get random unique positions
        for (int i = 0; i < validOffsets.Count; i++)
        {
            Vector3 temp = validOffsets[i];
            int randIndex = Random.Range(i, validOffsets.Count);
            validOffsets[i] = validOffsets[randIndex];
            validOffsets[randIndex] = temp;
        }

        // Spawn up to memberCount members without overlap
        int spawnCount = Mathf.Min(memberCount, validOffsets.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 snappedOffset = validOffsets[i];
            GameObject member = Instantiate(memberPrefab, lastSnappedPos + snappedOffset, Quaternion.identity);
            member.GetComponent<MemberVisual2>().core = gameObject;

            members.Add(member);
        }
    }

    // --- Move members relative to new snapped core position ---
    void MoveMembers(Vector3 coreDelta)
    {
        foreach (GameObject member in members)
        {
            member.transform.position += coreDelta;
        }
    }

    // --- Snap helper ---
    Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / spacing) * spacing,
            Mathf.Round(pos.y / spacing) * spacing,
            Mathf.Round(pos.z / spacing) * spacing
        );
    }

    void OnDrawGizmos()
    {
        // Draw minDistance sphere in red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        // Draw maxDistance sphere in green
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }

}
