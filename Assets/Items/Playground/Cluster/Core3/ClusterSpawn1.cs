using System.Collections.Generic;
using UnityEngine;

public class ClusterSpawn1 : MonoBehaviour
{
    [SerializeField] private GameObject memberPrefab; // Prefab to spawn
    private int numberToSpawn = 100;   // How many to spawn
    private float spawnRadius = 5f;  // Optional: spread them out

    public List<GameObject> members = new List<GameObject>();
    public int MemberCount => members.Count;

    void Start()
    {
        SpawnMembers();
    }

    private void SpawnMembers()
    {
        for (int i = 0; i < numberToSpawn; i++)
        {
            // Random position around the core
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            //spawnPos.y = transform.position.y; // keep them on same height if needed

            // Instantiate prefab
            GameObject newMember = Instantiate(memberPrefab, spawnPos, Quaternion.identity);

            // Add to list
            members.Add(newMember);

            // Assign core to MemberMovement2 script
            MemberMovement2 movementScript = newMember.GetComponent<MemberMovement2>();
            if (movementScript != null)
            {
                movementScript.core = this.gameObject;
            }
        }
    }
}
