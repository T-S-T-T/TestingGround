using UnityEngine;

public class MemberMovement3 : MonoBehaviour
{
    public float speed = 5f; // movement speed
    private Vector3 direction;
    private Transform meshTransform;

    void Start()
    {
        // Pick a random direction in 3D space
        direction = Random.onUnitSphere;

        // Cache the child called "Mesh"
        meshTransform = transform.Find("Mesh");
        if (meshTransform == null)
        {
            Debug.LogError("Child 'Mesh' not found! Make sure the GameObject has a child named 'Mesh'.");
        }
    }

    void Update()
    {
        // Move the boid along its direction
        transform.position += direction * speed * Time.deltaTime;

        // Rotate the Mesh child to face the direction
        if (meshTransform != null)
        {
            meshTransform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
