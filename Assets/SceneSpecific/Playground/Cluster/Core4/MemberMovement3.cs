using UnityEngine;

public class MemberMovement3 : MonoBehaviour
{
    public float speed = 5f; // movement speed
    public Vector3 Direction { get; private set; }
    private Transform meshTransform;

    void Start()
    {
        // Pick a random direction in 3D space
        Direction = Random.onUnitSphere;
    }

    void Update()
    {
        // Move the boid along its direction
        transform.position += Direction * speed * Time.deltaTime;
    }
}
