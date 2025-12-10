using UnityEngine;
using System.Collections.Generic;

public class MemberMovement3 : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;                // movement speed

    [Header("Boid Settings")]
    public float viewRadius = 5f;           // how far the boid can "see"
    public float viewAngle = 120f;          // field of view in degrees (front only)

    [Header("Rule Strengths")]
    public float separationStrength = 1f;   // how strongly to steer away
    public float alignmentStrength = 1f;    // how strongly to align with neighbors
    public float cohesionStrength = 1f;     // how strongly to move toward center

    public Vector3 Direction { get; private set; }

    void Start()
    {
        // Pick a random direction in 3D space
        Direction = Random.onUnitSphere;
    }

    void Update()
    {
        // Apply boid rules
        ApplyBoidRules();

        // Move the boid along its direction
        transform.position += Direction * speed * Time.deltaTime;
    }

    void ApplyBoidRules()
    {
        Vector3 separationForce = Vector3.zero;
        Vector3 alignmentForce = Vector3.zero;
        Vector3 cohesionForce = Vector3.zero;

        int neighborCount = 0;
        Vector3 averageHeading = Vector3.zero;
        Vector3 averagePosition = Vector3.zero;

        // Find all neighbors within viewRadius
        Collider[] neighbors = Physics.OverlapSphere(transform.position, viewRadius);

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject == gameObject) continue; // skip self

            Vector3 toNeighbor = neighbor.transform.position - transform.position;
            float distance = toNeighbor.magnitude;
            Vector3 toNeighborDir = toNeighbor.normalized;

            // Check if neighbor is in front (within viewAngle)
            if (Vector3.Angle(Direction, toNeighborDir) < viewAngle * 0.5f)
            {
                Debug.Log("Neighbor in view!");
                neighborCount++;

                // Separation: steer away
                separationForce -= toNeighborDir / distance;

                // Alignment: add neighbor's heading (if it has a MemberMovement3)
                MemberMovement3 other = neighbor.GetComponent<MemberMovement3>();
                if (other != null)
                {
                    Debug.Log("Average heading!");
                    averageHeading += other.Direction;
                }

                // Cohesion: add neighbor's position
                averagePosition += neighbor.transform.position;
            }
        }

        if (neighborCount > 0)
        {
            // Alignment force: average heading
            alignmentForce = (averageHeading / neighborCount).normalized;

            // Cohesion force: toward center of mass
            Vector3 centerOfMass = averagePosition / neighborCount;
            cohesionForce = (centerOfMass - transform.position).normalized;
        }

        // Combine forces with weights
        Vector3 totalForce =
            separationForce * separationStrength +
            alignmentForce * alignmentStrength +
            cohesionForce * cohesionStrength;

        if (totalForce != Vector3.zero)
        {
            // Adjust direction smoothly
            Direction = (Direction + totalForce).normalized;
        }
    }

    void OnDrawGizmos()
    {
        if (Direction == Vector3.zero) return;

        // Draw view radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Draw view angle boundaries
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
