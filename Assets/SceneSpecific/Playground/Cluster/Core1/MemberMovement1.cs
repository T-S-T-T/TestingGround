using System.Collections;
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

    public void Move(Vector3 targetPosition, float delay)
    {
        StartCoroutine(MoveAfterDelay(targetPosition, delay));
    }

    private IEnumerator MoveAfterDelay(Vector3 targetPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        transform.position = targetPosition;
    }
}
