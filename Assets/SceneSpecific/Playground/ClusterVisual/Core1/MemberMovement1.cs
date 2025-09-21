using UnityEngine;

public class MemberMovement1 : MonoBehaviour
{
    [HideInInspector]
    public GameObject core;          // Initial grid offset from core

    public float distance;

    void Start()
    {

    }

    void Update()
    {

    }

    public void Move(Vector3 targetPosition)
    {
        transform.position = targetPosition;
    }
}
