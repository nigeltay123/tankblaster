using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TankController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;       // How fast the tank moves
    public float rotationSpeed = 200f; // How fast the tank rotates

    private Rigidbody2D rb;
    private float moveInput;
    private float rotationInput;

    void Start()
    {
        // Get Rigidbody2D from the tank
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Get WASD or Arrow key input
        moveInput = Input.GetAxis("Vertical");   // W/S or Up/Down
        rotationInput = -Input.GetAxis("Horizontal"); // A/D or Left/Right
    }

    void FixedUpdate()
    {
        // Move forward/backward
        rb.velocity = transform.up * moveInput * moveSpeed;

        // Rotate
        rb.MoveRotation(rb.rotation + rotationInput * rotationSpeed * Time.fixedDeltaTime);
    }
}
