using UnityEngine;
using System.Collections.Generic;

public class MemberMovement3 : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;                // movement speed

    [Header("Boid Separation Settings")]
    public float viewRadius = 5f;           // how far the boid can "see"
    public float viewAngle = 120f;          // field of view in degrees (front only)
    public float separationStrength = 1f;   // how strongly to steer away

    public Vector3 Direction { get; private set; }

    void Start()
    {
        // Pick a random direction in 3D space
        Direction = Random.onUnitSphere;
    }

    void Update()
    {
        // Apply separation rule
        ApplySeparation();

        // Move the boid along its direction
        transform.position += Direction * speed * Time.deltaTime;
    }

    void ApplySeparation()
    {
        Vector3 separationForce = Vector3.zero;

        // Find all neighbors within viewRadius
        Collider[] neighbors = Physics.OverlapSphere(transform.position, viewRadius);

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject == gameObject) continue; // skip self

            Vector3 toNeighbor = neighbor.transform.position - transform.position;
            float distance = toNeighbor.magnitude;

            // Normalize for angle check
            Vector3 toNeighborDir = toNeighbor.normalized;

            // Check if neighbor is in front (within viewAngle)
            Debug.Log(Vector3.Angle(Direction, toNeighborDir));
            if (Vector3.Angle(Direction, toNeighborDir) < viewAngle * 0.5f)
            {
                Debug.Log("Entity in view!");
                // Steer away: add opposite direction, weighted by distance
                separationForce -= toNeighborDir / distance;
            }
        }

        if (separationForce != Vector3.zero)
        {
            // Adjust direction smoothly
            Direction = (Direction + separationForce * separationStrength).normalized;
        }
    }

    void OnDrawGizmos()
    {
        if (Direction == Vector3.zero) return;

        // Draw view radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Draw view angle boundaries (in local forward plane)
        Gizmos.color = Color.cyan;
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * Direction;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle * 0.5f, 0) * Direction;

        Gizmos.DrawRay(transform.position, leftBoundary.normalized * viewRadius);
        Gizmos.DrawRay(transform.position, rightBoundary.normalized * viewRadius);

        // Draw forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Direction.normalized * viewRadius);
    }
}
