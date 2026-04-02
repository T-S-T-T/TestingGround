using UnityEngine;

/// <summary>
/// SpiderCamera — smooth third-person follow camera.
/// Attach to the Main Camera. Assign the spider root as the target.
/// </summary>
public class SpiderCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;           // the Spider root GameObject

    [Header("Offset")]
    public Vector3 offset = new Vector3(0f, 2.5f, -5f);  // local-space offset
    public float followSpeed = 8f;
    public float rotateSpeed = 6f;

    [Header("Look")]
    public float lookAheadDistance = 1.5f;   // look slightly ahead of spider

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position: offset rotated by target's Y rotation
        Quaternion flatRot = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        Vector3 desiredPos  = target.position + flatRot * offset;

        // Smooth position and rotation
        transform.position = Vector3.Lerp(
            transform.position, desiredPos,
            Time.deltaTime * followSpeed);

        // Look at a point slightly ahead of the spider
        Vector3 lookTarget = target.position + target.forward * lookAheadDistance;
        Quaternion desiredRot = Quaternion.LookRotation(
            lookTarget - transform.position, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot,
            Time.deltaTime * rotateSpeed);
    }
}
