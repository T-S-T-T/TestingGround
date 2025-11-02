using UnityEngine;

public class MemberMovement2 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject core;
    public Vector3 node;
    public float speed = 5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Ensure gravity is disabled
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Only move if node is set and not already at the target
        if (node != Vector3.zero && Vector3.Distance(transform.position, node) > 0.1f)
        {
            Vector3 direction = (node - transform.position).normalized;
            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        }
    }

    //public void MoveToNode()
    //{
    //    transform.position = node;
    //}
}
