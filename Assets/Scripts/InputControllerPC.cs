using UnityEngine;
using UnityEngine.InputSystem;

public class InputControllerPC : MonoBehaviour
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
        // Check if the left mouse button was pressed this frame
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            // Read mouse screen position and convert it to 2D world space
            Vector2 mouseScreenPos = mouse.position.ReadValue();
            Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
            Vector2 mouseWorld2D = new Vector2(worldMousePos.x, worldMousePos.y);

            Debug.Log(mouseWorld2D); // Log the mouse world position for debugging
            foreach (Graph.VertexT v in GameManager.Instance.graph.GetVertices())
            {
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


        if (Touchscreen.current == null || Touchscreen.current.touches.Count < 1) { 
            return;
        }

        if (!Touchscreen.current.primaryTouch.press.wasPressedThisFrame) {
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
                OnCircleClicked(v.keep);
                return;
            }
        }

        OnCircleClicked(null);
    }

    public static void OnCircleClicked(StarComp keepCompOrNull)
    {

        // Clicked on something that is not a keep, deselect the current keep
        if (keepCompOrNull == null)
        {
            GameManager.Instance.SetSelectedStar(null);
            return;
        }

        StarComp clickedKeepComp = keepCompOrNull;

        // Clicked again on the same keep , deselect it and clear the autoPointerRoad
        if (GameManager.Instance.selectedStar == clickedKeepComp)
        {
            //selectedKeep.autoPointerRoad = null;
            GameManager.Instance.SetSelectedStar(null);
            return;

        }

        if (GameManager.Instance.selectedStar != null)
        {

            LaneComp connectingRoadOrNull = GetRoadBetweenNeighborsOrNull(GameManager.Instance.selectedStar, clickedKeepComp);

            if (connectingRoadOrNull != null)
            {
                GameManager.Instance.CreateUnit(GameManager.Instance.selectedStar, connectingRoadOrNull);
            }

            GameManager.Instance.SetSelectedStar(null);
        }
        else
        {
            // Prevent player from clicking on a keep that does not belong to them and selecting it
            if (clickedKeepComp.Faction != Factions.Player)
            {
                // Clicked on a keep that does not belong to the player, do nothing
                GameManager.Instance.SetSelectedStar(null);
                return;
            }
            GameManager.Instance.SetSelectedStar(clickedKeepComp);
        }
    }


    public static LaneComp GetRoadBetweenNeighborsOrNull(StarComp keepA, StarComp keepB)
    {
        return GameManager.Instance.graph.GetEdgeBetweenVertices(keepA.Vertex, keepB.Vertex)?.road;
    }
}