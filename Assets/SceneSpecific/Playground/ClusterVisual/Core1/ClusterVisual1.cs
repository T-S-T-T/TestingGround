using UnityEngine;

public class ClusterVisual1 : MonoBehaviour
{
    [Header("Member Settings")]
    public GameObject memberPrefab;     // Assign your Member prefab here
    public float spacing = 1f;          // Distance between members along X, Y, Z
    public float maxRange = 5f;         // Radius around core to spawn within

    void Start()
    {
        SpawnMembers();
    }

    void SpawnMembers()
    {
        Vector3 rawCenter = transform.position;
        Vector3 center = new Vector3(
            Mathf.Round(rawCenter.x),
            Mathf.Round(rawCenter.y),
            Mathf.Round(rawCenter.z)
        );

        int steps = Mathf.CeilToInt(maxRange / spacing);

        for (int x = -steps; x <= steps; x++)
        {
            for (int y = -steps; y <= steps; y++)
            {
                for (int z = -steps; z <= steps; z++)
                {
                    Vector3 offset = new Vector3(x, y, z) * spacing;

                    if (offset.magnitude <= maxRange)
                    {
                        Vector3 spawnPos = center + offset;
                        GameObject member = Instantiate(memberPrefab, spawnPos, Quaternion.identity);

                        // 1) Grab the MemberMovement1 component
                        MemberMovement1 mover = member.GetComponent<MemberMovement1>();

                        // 2) If it exists, assign this core’s Transform (or GameObject)
                        if (mover != null)
                        {
                            mover.core = gameObject;
                        }

                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }
}