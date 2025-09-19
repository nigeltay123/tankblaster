using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TankController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotateSpeed = 180f;

    [Tooltip("Set TRUE if the sprite points UP in the image. Set FALSE if it points RIGHT.")]
    public bool spriteFacesUp = true;

    private Rigidbody2D rb;
    private float moveInput;
    private float turnInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");

        if (Mathf.Approximately(v, 0f))
            v = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
              + (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);
        if (Mathf.Approximately(h, 0f))
            h = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f)
              + (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);

        moveInput = Mathf.Clamp(v, -1f, 1f);
        turnInput = Mathf.Clamp(h, -1f, 1f);
    }

    void FixedUpdate()
    {
        // Rotate
        if (!Mathf.Approximately(turnInput, 0f))
        {
            float newRot = rb.rotation - turnInput * rotateSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(newRot);
        }
        else
        {
            rb.angularVelocity = 0f;
        }

        // Move along the sprite's forward direction
        if (!Mathf.Approximately(moveInput, 0f))
        {
            Vector2 forward = spriteFacesUp ? (Vector2)transform.up : (Vector2)transform.right;
            Vector2 newPos = rb.position + forward * (moveInput * moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }
    }
}