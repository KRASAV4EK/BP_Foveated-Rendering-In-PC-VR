using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFirstPersonController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;

    public Transform cameraTransform;

    public float snapTurnAngle = 30f; // Degrees per snap turn
    public float snapTurnThreshold = 0.8f; // Sensitivity threshold
    private bool canSnapTurn = true; // Lock between snap turns

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleSnapTurn();
    }

    // Handles mouse-based look
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    // Handles WASD movement and gravity
    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Handles snap turning via right stick
    void HandleSnapTurn()
    {
        // Get the horizontal axis of the right thumbstick
        float rightStickX = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;

        if (canSnapTurn && Mathf.Abs(rightStickX) > snapTurnThreshold)
        {
            float angle = rightStickX > 0 ? snapTurnAngle : -snapTurnAngle;
            transform.Rotate(Vector3.up * angle);
            canSnapTurn = false; // Prevent continuous turning while held
        }
        else if (Mathf.Abs(rightStickX) < 0.2f)
        {
            canSnapTurn = true; // Unlock turning after stick returns to center
        }
    }
}
