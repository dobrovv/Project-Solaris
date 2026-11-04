using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Touch camera controller for an orthographic 2D camera (new Input System).
///
/// - One finger drag      : pan (the world point under your finger stays under your finger)
/// - Two finger pinch     : zoom around the pinch center (two-finger drag also pans)
/// - Optional             : inertia after a flick, world bounds, ignore touches that start on UI
///
/// Setup: add to your orthographic Camera, and set
/// Project Settings > Player > Active Input Handling to "Input System Package" (or "Both").
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraControllerMobile : MonoBehaviour
{
    [Header("Zoom (orthographic size)")]
    [SerializeField, Min(0.01f)] private float minSize = 2f;
    [SerializeField, Min(0.01f)] private float maxSize = 12f;

    [Header("Pan")]
    [Tooltip("Finger must move this many pixels before the camera starts panning, so taps don't nudge the camera.")]
    [SerializeField, Min(0f)] private float dragThresholdPixels = 8f;
    [SerializeField] private bool useInertia = true;
    [Tooltip("Higher = the camera stops sooner after a flick.")]
    [SerializeField, Min(0f)] private float inertiaDamping = 5f;

    [Header("World bounds (optional)")]
    [SerializeField] private bool useBounds = false;
    [Tooltip("The camera view is kept inside this rectangle. Max zoom is limited so the view never exceeds it.")]
    [SerializeField] private Rect worldBounds = new Rect(-20f, -20f, 40f, 40f);

    [Header("Input")]
    [Tooltip("Ignore gestures that start on a UI element (requires an EventSystem in the scene).")]
    [SerializeField] private bool ignoreInputOverUI = true;

    private enum Gesture { None, Pan, Pinch }

    private Camera _cam;
    private Gesture _gesture = Gesture.None;

    // Pan state
    private int _panId;
    private Vector2 _panOrigin;
    private Vector2 _lastPanPos;
    private bool _panMoving;
    private Vector2 _velocity; // world units / second (used for inertia)

    // Pinch state
    private int _pinchIdA, _pinchIdB;
    private Vector2 _lastMid;
    private float _lastDist;

    // Misc
    private readonly List<Touch> _validTouches = new List<Touch>(10);
    private readonly HashSet<int> _uiTouchIds = new HashSet<int>();

    // ------------------------------------------------------------------ Unity

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam.orthographic)
            Debug.LogWarning($"{nameof(CameraControllerMobile)} expects an orthographic camera.", this);
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable(); // reference counted, safe if something else enables it too
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        _gesture = Gesture.None;
        _velocity = Vector2.zero;
        _uiTouchIds.Clear();
    }

    private void OnValidate()
    {
        minSize = Mathf.Max(0.01f, minSize);
        maxSize = Mathf.Max(minSize, maxSize);
    }

    // LateUpdate so that UI (EventSystem) has already processed this frame's touches
    // and so we move the camera after any gameplay code that moves things this frame.
    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;

        HandleTouch(dt);

        if (_gesture == Gesture.None) ApplyInertia(dt);

        ConstrainToLimits();
    }

    // ------------------------------------------------------------------ Touch input

    private void HandleTouch(float dt)
    {
        var touches = Touch.activeTouches;

        if (touches.Count == 0)
        {
            _uiTouchIds.Clear();
            EndGesture();
            return;
        }

        // Collect touches that are still down and didn't start on UI.
        _validTouches.Clear();
        for (int i = 0; i < touches.Count; i++)
        {
            Touch t = touches[i];

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                _uiTouchIds.Remove(t.touchId);
                continue;
            }

            if (t.phase == TouchPhase.Began && ignoreInputOverUI && IsOverUI(t.touchId))
                _uiTouchIds.Add(t.touchId);

            if (_uiTouchIds.Contains(t.touchId)) continue;

            _validTouches.Add(t);
        }

        switch (_validTouches.Count)
        {
            case 0:
                EndGesture();
                break;
            case 1:
                HandlePan(_validTouches[0].touchId, _validTouches[0].screenPosition, dt);
                break;
            default:
                HandlePinch(_validTouches[0], _validTouches[1]);
                break;
        }
    }

    private void HandlePan(int id, Vector2 pos, float dt)
    {
        // New pan (first touch, or the finger changed, e.g. after a pinch).
        if (_gesture != Gesture.Pan || _panId != id)
        {
            _gesture = Gesture.Pan;
            _panId = id;
            _panOrigin = _lastPanPos = pos;
            _panMoving = false;
            _velocity = Vector2.zero; // touching the screen stops any inertia
            return;
        }

        if (!_panMoving)
        {
            if ((pos - _panOrigin).sqrMagnitude < dragThresholdPixels * dragThresholdPixels) return;
            _panMoving = true; // _lastPanPos is still the origin, so no distance is lost
        }

        Vector2 shift = ApplyTransform(_lastPanPos, pos, 1f);
        _lastPanPos = pos;

        if (dt > 0f)
            _velocity = Vector2.Lerp(_velocity, shift / dt, 0.5f);
    }

    private void HandlePinch(Touch a, Touch b)
    {
        Vector2 pa = a.screenPosition;
        Vector2 pb = b.screenPosition;
        Vector2 mid = (pa + pb) * 0.5f;
        float dist = Vector2.Distance(pa, pb);

        // New pinch (or the pair of fingers changed): just record the start values.
        if (_gesture != Gesture.Pinch || _pinchIdA != a.touchId || _pinchIdB != b.touchId)
        {
            _gesture = Gesture.Pinch;
            _pinchIdA = a.touchId;
            _pinchIdB = b.touchId;
            _lastMid = mid;
            _lastDist = dist;
            _velocity = Vector2.zero;
            return;
        }

        // Fingers moving apart (dist grows) => zoom in => orthographic size shrinks.
        float zoomFactor = (dist > 1f && _lastDist > 1f) ? _lastDist / dist : 1f;

        ApplyTransform(_lastMid, mid, zoomFactor);

        _lastMid = mid;
        _lastDist = dist;
    }

    // ------------------------------------------------------------------ Core

    /// <summary>
    /// Moves/zooms the camera so the world point that was under <paramref name="fromScreen"/>
    /// ends up under <paramref name="toScreen"/>, after scaling the ortho size by
    /// <paramref name="zoomFactor"/> (&lt;1 zooms in, &gt;1 zooms out).
    /// One formula covers pan, pinch-zoom-at-center and wheel-zoom-at-cursor.
    /// Returns the world-space camera shift.
    /// </summary>
    private Vector2 ApplyTransform(Vector2 fromScreen, Vector2 toScreen, float zoomFactor)
    {
        Vector3 before = _cam.ScreenToWorldPoint(fromScreen);

        if (!Mathf.Approximately(zoomFactor, 1f))
            _cam.orthographicSize = ClampSize(_cam.orthographicSize * zoomFactor);

        Vector3 after = _cam.ScreenToWorldPoint(toScreen);

        Vector3 shift = before - after;
        shift.z = 0f;
        transform.position += shift;
        return shift;
    }

    private void EndGesture()
    {
        if (_gesture == Gesture.None) return;

        // Keep the velocity only if a real drag just ended and inertia is on.
        if (_gesture != Gesture.Pan || !_panMoving || !useInertia)
            _velocity = Vector2.zero;

        _gesture = Gesture.None;
    }

    private void ApplyInertia(float dt)
    {
        if (_velocity == Vector2.zero) return;

        Vector3 p = transform.position + (Vector3)(_velocity * dt);
        Vector3 clamped = ClampToBounds(p);

        // Hitting a bounds edge kills the velocity on that axis.
        if (!Mathf.Approximately(clamped.x, p.x)) _velocity.x = 0f;
        if (!Mathf.Approximately(clamped.y, p.y)) _velocity.y = 0f;

        transform.position = clamped;

        _velocity *= Mathf.Exp(-inertiaDamping * dt);

        float stopSpeed = 0.01f * _cam.orthographicSize;
        if (_velocity.sqrMagnitude < stopSpeed * stopSpeed)
            _velocity = Vector2.zero;
    }

    // ------------------------------------------------------------------ Limits

    private void ConstrainToLimits()
    {
        float size = ClampSize(_cam.orthographicSize);
        if (!Mathf.Approximately(size, _cam.orthographicSize))
            _cam.orthographicSize = size;

        if (useBounds)
            transform.position = ClampToBounds(transform.position);
    }

    private float ClampSize(float size)
    {
        float max = maxSize;

        if (useBounds)
        {
            // Don't zoom out further than what the bounds can contain.
            float fitHeight = worldBounds.height * 0.5f;
            float fitWidth = worldBounds.width * 0.5f / _cam.aspect;
            max = Mathf.Min(max, fitHeight, fitWidth);
        }

        return Mathf.Clamp(size, minSize, Mathf.Max(max, minSize));
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        if (!useBounds) return pos;

        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        pos.x = ClampAxis(pos.x, worldBounds.xMin + halfW, worldBounds.xMax - halfW, worldBounds.center.x);
        pos.y = ClampAxis(pos.y, worldBounds.yMin + halfH, worldBounds.yMax - halfH, worldBounds.center.y);
        return pos;
    }

    // If the view is larger than the bounds on an axis (min > max), center on that axis.
    private static float ClampAxis(float value, float min, float max, float center)
        => min > max ? center : Mathf.Clamp(value, min, max);

    // ------------------------------------------------------------------ Helpers

    private static bool IsOverUI(int pointerId)
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);

    /// <summary>Change the bounds at runtime (e.g. when loading a level).</summary>
    public void SetBounds(Rect bounds)
    {
        worldBounds = bounds;
        useBounds = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    }
}