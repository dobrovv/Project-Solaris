using UnityEngine;
using UnityEngine.InputSystem;

public class MouseController : MonoBehaviour
{
    [Header("Circle Settings")]
    public Transform circleCenter; // Assign your circle's transform (or leave empty to use this object's position)
    public float maxClickRadius = 1.0f; // How close the click needs to be

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
        if (circleCenter == null)
        {
            circleCenter = transform;
        }
    }

    void Update()
    {
        // Check if the left mouse button was pressed this frame
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            // Read mouse screen position and convert it to 2D world space
            Vector2 mouseScreenPos = mouse.position.ReadValue();
            Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
            Vector2 mouseWorld2D = new Vector2(worldMousePos.x, worldMousePos.y);

            Debug.Log(mouseWorld2D); // Log the mouse world position for debugging
            foreach (Graph.VertexT v in GameManager.Instance.graph.GetVertices()) {
                // Calculate the distance from the click to the circle center
                Vector2 circlePos2D = new Vector2(v.position.x, v.position.y);
                float distance = Vector2.Distance(mouseWorld2D, circlePos2D);

                // Check if the click is within the allowed radius
                if (distance <= maxClickRadius)
                {
                    Debug.Log($"Clicked close to the circle! Distance: {distance:F2}");
                    OnCircleClicked(v.keep);
                    return;
                }
            }

            OnCircleClicked(null);
        }
    }

    private void OnCircleClicked(StarComp clickedOrNull)
    {
        if (clickedOrNull == null)
        {
            GameManager.Instance.SetSelectedStar(null);
            return;
        }

        GameManager.Instance.SetSelectedStar(clickedOrNull);
        // Put your custom logic here (e.g., select unit, trigger event, change color)
    }
}