using UnityEngine;

public class MemberMeshRotation1 : MonoBehaviour
{
    private MemberMovement3 movementScript;
    private Transform meshTransform;

    void Start()
    {
        // Get the movement script from the same GameObject
        movementScript = GetComponent<MemberMovement3>();
        if (movementScript == null)
        {
            Debug.LogError("MemberMovement3 script not found on this GameObject!");
        }

        // Cache the child called "Mesh"
        meshTransform = transform.Find("Mesh");
        if (meshTransform == null)
        {
            Debug.LogError("Child 'Mesh' not found! Make sure the GameObject has a child named 'Mesh'.");
        }
    }

    void Update()
    {
        if (movementScript != null && meshTransform != null)
        {
            // Rotate the Mesh child to face the current direction
            meshTransform.rotation = Quaternion.LookRotation(movementScript.Direction);
        }
    }
}