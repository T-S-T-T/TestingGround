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
        if (node != Vector3.zero)
        {
            float distance = Vector3.Distance(transform.position, node);

            if (distance > 0.1f) // tolerance zone
            {
                Vector3 direction = (node - transform.position).normalized;
                rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
            }
            else
            {
                // Snap to node and stop moving
                rb.MovePosition(node);
            }
        }
    }

    //public void MoveToNode()
    //{
    //    transform.position = node;
    //}
}
