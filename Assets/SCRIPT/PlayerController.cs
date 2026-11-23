using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("CS:GO Movement Speeds")]
    public float runSpeed = 5.4f;        // Authentic CS:GO M4A4 run speed (~225 u/s)
    public float shiftWalkSpeed = 2.8f;  // Hold Left Shift (~52% speed, silent & accurate)
    public float crouchSpeed = 1.9f;     // Hold Left Ctrl
    public float groundAccel = 14f;      // Snappy Source-engine ramp-up
    public float groundFriction = 8f;    // Crisp stop + counter-strafe feel

    [Header("CS:GO Jump & Gravity")]
    public float jumpHeight = 1.05f;
    public float gravity = -28f;

    [Header("Camera Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 85f;

    private CharacterController controller;
    private Vector3 currentHorizontalVel;
    private float verticalVelocity;
    private float cameraPitch = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        // GetAxisRaw removes Unity's sluggish mouse smoothing for crisp 1:1 CS flicks
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 wishDir = (transform.right * moveX + transform.forward * moveZ).normalized;

        // Speed selection: Ctrl Crouch > Shift Walk > Normal Run
        float targetSpeed = runSpeed;
        if (Input.GetKey(KeyCode.LeftControl))
            targetSpeed = crouchSpeed;
        else if (Input.GetKey(KeyCode.LeftShift))
            targetSpeed = shiftWalkSpeed;

        Vector3 targetVel = wishDir * targetSpeed;

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            // Extra snappy stop when counter-strafing (pressing opposite direction)
            float isCounterStrafing = Vector3.Dot(currentHorizontalVel, targetVel) < -0.1f ? 1.8f : 1f;
            float accelRate = (wishDir.sqrMagnitude > 0.01f) ? groundAccel * isCounterStrafing : groundFriction;

            currentHorizontalVel = Vector3.MoveTowards(
                currentHorizontalVel,
                targetVel,
                accelRate * targetSpeed * Time.deltaTime
            );

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            // Light air control so you can curve jumps like CS:GO without floating
            currentHorizontalVel = Vector3.MoveTowards(
                currentHorizontalVel,
                targetVel,
                (groundAccel * 0.25f) * targetSpeed * Time.deltaTime
            );
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = currentHorizontalVel + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }
}