using UnityEngine;

public class MemberMovement2 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject core;

    public Vector3 node;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MoveToNode()
    {
        transform.position = node;
    }
}
