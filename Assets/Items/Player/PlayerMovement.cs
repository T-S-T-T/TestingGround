using UnityEngine;

public class Player_movement : MonoBehaviour
{

    [Header("Speeds")]
    public float walkSpeed = 1f;
    public float sprintMultiplier = 2f;
    public float verticalSpeed = 5f;

    CharacterController controller;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        // Read input axes
        float moveX = Input.GetAxis("Horizontal"); // A/D, Left/Right
        float moveZ = Input.GetAxis("Vertical");   // W/S, Up/Down

        // Base horizontal movement vector
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Sprint multiplier
        float currentSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed *= sprintMultiplier;

        // Vertical (fly) movement
        float v = 0f;
        if (Input.GetKey(KeyCode.Space))
            v = verticalSpeed;
        else if (Input.GetKey(KeyCode.LeftControl))
            v = -verticalSpeed;

        // Combine horizontal + vertical, then move
        move = move.normalized * currentSpeed;
        move.y = v;

        controller.Move(move * Time.deltaTime);
    }
}
