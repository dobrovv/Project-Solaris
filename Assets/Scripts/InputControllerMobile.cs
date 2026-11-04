using UnityEngine;
using UnityEngine.InputSystem;

public class InputControllerMobile : MonoBehaviour
{
    [Header("Circle Settings")]
    public float maxClickRadius = 1.0f; // How close the click needs to be

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Touchscreen.current == null || Touchscreen.current.touches.Count < 1)
        {
            return;
        }

        if (!Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return;
        }

        // Read touch screen position and convert it to 2D world space
        Vector2 touchScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        Vector3 worldTouchPos = mainCamera.ScreenToWorldPoint(new Vector3(touchScreenPos.x, touchScreenPos.y, mainCamera.nearClipPlane));
        Vector2 touchWorld2D = new Vector2(worldTouchPos.x, worldTouchPos.y);

        Debug.Log(touchWorld2D); // Log the mouse world position for debugging
        foreach (Graph.VertexT v in GameManager.Instance.graph.GetVertices())
        {
            // Calculate the distance from the click to the circle center
            Vector2 circlePos2D = new Vector2(v.position.x, v.position.y);
            float distance = Vector2.Distance(touchWorld2D, circlePos2D);

            // Check if the click is within the allowed radius
            if (distance <= maxClickRadius)
            {
                Debug.Log($"Touched close to the circle! Distance: {distance:F2}");
                InputControllerPC.OnCircleClicked(v.keep);
                return;
            }
        }

        InputControllerPC.OnCircleClicked(null);
    }
}