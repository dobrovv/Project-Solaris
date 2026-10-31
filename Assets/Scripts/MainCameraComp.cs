using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class OrthographicCameraController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float movementSmoothing = 10f;
    [SerializeField] private float dragSensitivity = 1f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSensitivity = 2f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float zoomSmoothing = 8f;

    private Camera cam;
    private Vector2 moveInput;
    private float scrollInput;
    private Vector3 targetPosition;
    private float targetZoom;

    // Input Actions
    private InputAction moveAction;
    private InputAction zoomAction;
    private InputAction dragHoldAction;
    private InputAction mouseDeltaAction;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        targetPosition = transform.position;
        targetZoom = cam.orthographicSize;

        SetupInputs();
    }

    private void SetupInputs()
    {
        // 1. Keyboard / Gamepad Move Action
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");

        // 2. Zoom Action (Mouse Scroll / Gamepad Shoulders)
        zoomAction = new InputAction("Zoom", binding: "<Mouse>/scroll/y");
        zoomAction.AddBinding("<Gamepad>/rightShoulder").WithProcessor("Scale(factor=120)");
        zoomAction.AddBinding("<Gamepad>/leftShoulder").WithProcessor("Scale(factor=-120)");

        // 3. Middle Click Drag Actions
        dragHoldAction = new InputAction("DragHold", binding: "<Mouse>/middleButton");
        mouseDeltaAction = new InputAction("MouseDelta", binding: "<Mouse>/delta");

        // Enable all actions
        moveAction.Enable();
        zoomAction.Enable();
        dragHoldAction.Enable();
        mouseDeltaAction.Enable();
    }

    private void Update()
    {
        HandleInput();
        PerformMovement();
        PerformZoom();
    }

    private void HandleInput()
    {
        // Read keyboard/gamepad vector
        moveInput = moveAction.ReadValue<Vector2>();

        // Read scroll delta
        scrollInput = zoomAction.ReadValue<float>();// * 0.01f;

        // Process mouse dragging if middle button is pressed
        if (dragHoldAction.IsPressed())
        {
            Vector2 mouseDelta = mouseDeltaAction.ReadValue<Vector2>();

            if (mouseDelta.sqrMagnitude > 0.01f)
            {
                // Convert screen delta into world-relative scale based on the camera's height view
                float screenToWorldFactor = (cam.orthographicSize * 2f) / Screen.height;

                // Invert the delta because moving the mouse right should pull the camera left
                Vector3 dragVelocity = new Vector3(-mouseDelta.x, -mouseDelta.y, 0) * screenToWorldFactor * dragSensitivity;

                // Snap target position directly forward when dragging for instant tracking response
                targetPosition += dragVelocity;
            }
        }
    }

    private void PerformMovement()
    {
        // Add keyboard input over time to target position
        Vector3 moveDirection = new Vector3(moveInput.x, moveInput.y, 0);
        targetPosition += moveDirection * moveSpeed * Time.deltaTime;

        // Smoothly interpolate to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, movementSmoothing * Time.deltaTime);
    }

    private void PerformZoom()
    {
        if (Mathf.Abs(scrollInput) > 0.01f)
        {

            targetZoom -= scrollInput * zoomSensitivity;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSmoothing * Time.deltaTime);
    }

    private void OnDisable()
    {
        moveAction.Disable();
        zoomAction.Disable();
        dragHoldAction.Disable();
        mouseDeltaAction.Disable();
    }
}
