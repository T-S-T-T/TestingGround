using UnityEngine;

public class MemberConstraint1 : MonoBehaviour
{
    [Header("Boundary Settings")]
    private Vector3 minBounds = new Vector3(-50f, 5f, -50f);
    private Vector3 maxBounds = new Vector3(50f, 55f, 50f);

    void Update()
    {
        Vector3 pos = transform.position;

        // Wrap X
        if (pos.x > maxBounds.x)
            pos.x = minBounds.x;
        else if (pos.x < minBounds.x)
            pos.x = maxBounds.x;

        // Wrap Y
        if (pos.y > maxBounds.y)
            pos.y = minBounds.y;
        else if (pos.y < minBounds.y)
            pos.y = maxBounds.y;

        // Wrap Z
        if (pos.z > maxBounds.z)
            pos.z = minBounds.z;
        else if (pos.z < minBounds.z)
            pos.z = maxBounds.z;

        transform.position = pos;
    }
}
