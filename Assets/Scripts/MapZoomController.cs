// Assets/Scripts/UI/MapZoomController.cs
// Zooms a UI RectTransform (your Map) via buttons, wheel, and pinch.
// Works great with your drag/drop canvas-based map.
//
// How to use:
// 1) Add to a scene object (e.g., GameSystems).
// 2) Assign Target to your Map (RectTransform).
// 3) Assign zoomInButton / zoomOutButton (optional – script can also find by name).
// 4) Press play; use buttons, mouse wheel, or pinch.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapZoomController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("RectTransform you want to scale (your Map root).")]
    public RectTransform target;         // e.g., Canvas/Map
    [Tooltip("Optional: parent viewport to keep the map from drifting out of view.")]
    public RectTransform viewport;       // e.g., the same Canvas or a masked panel; leave null to skip clamp

    [Header("Buttons (optional)")]
    public Button zoomInButton;
    public Button zoomOutButton;

    [Header("Zoom Settings")]
    [Tooltip("Minimum allowed uniform scale.")]
    public float minScale = 0.75f;
    [Tooltip("Maximum allowed uniform scale.")]
    public float maxScale = 2.0f;
    [Tooltip("Scale step per click or wheel tick.")]
    public float step = 0.15f;
    [Tooltip("Seconds to animate scale changes.")]
    public float tweenSeconds = 0.15f;

    [Header("Input")]
    [Tooltip("Enable mouse wheel zoom on desktop.")]
    public bool enableMouseWheel = true;
    [Tooltip("Enable two-finger pinch on touch devices.")]
    public bool enablePinch = true;
    [Tooltip("Mouse wheel sensitivity (positive numbers).")]
    public float wheelSensitivity = 1.0f;
    [Tooltip("Pinch sensitivity (larger = faster).")]
    public float pinchSensitivity = 0.005f;

    Coroutine _tween;
    float _targetScale = 1f;

    void Awake()
    {
        // Auto-find target if not set
        if (!target)
        {
            var t = GameObject.Find("Map");
            if (t) target = t.GetComponent<RectTransform>();
        }

        if (!viewport && target) viewport = target.parent as RectTransform;

        if (!target)
        {
            Debug.LogWarning("[MapZoomController] No target assigned and 'Map' not found.");
            enabled = false;
            return;
        }

        // Make sure we scale uniformly around center
        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot    = new Vector2(0.5f, 0.5f);

        _targetScale = target.localScale.x;

        // Hook buttons if provided
        if (zoomInButton)  zoomInButton.onClick.AddListener(ZoomIn);
        if (zoomOutButton) zoomOutButton.onClick.AddListener(ZoomOut);
    }

    void Update()
    {
        // Mouse wheel (desktop)
        if (enableMouseWheel)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                float delta = Mathf.Sign(scroll) * step * wheelSensitivity;
                SetScaleAnimated(_targetScale + delta);
            }
        }

        // Pinch (mobile)
        if (enablePinch && Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            float prevMag = (t0Prev - t1Prev).magnitude;
            float currMag = (t0.position - t1.position).magnitude;
            float diff = currMag - prevMag;

            if (Mathf.Abs(diff) > 0.01f)
            {
                float delta = diff * pinchSensitivity;
                SetScaleAnimated(_targetScale + delta);
            }
        }
    }

    // Button hooks
    public void ZoomIn()  => SetScaleAnimated(_targetScale + step);
    public void ZoomOut() => SetScaleAnimated(_targetScale - step);

    // Instantly set scale (no animation)
    public void SetScaleInstant(float s)
    {
        _targetScale = Mathf.Clamp(s, minScale, maxScale);
        target.localScale = new Vector3(_targetScale, _targetScale, 1f);
        ClampInsideViewport();
    }

    // Smoothly set scale
    public void SetScaleAnimated(float s)
    {
        _targetScale = Mathf.Clamp(s, minScale, maxScale);
        if (_tween != null) StopCoroutine(_tween);
        _tween = StartCoroutine(TweenScale(_targetScale, tweenSeconds));
    }

    IEnumerator TweenScale(float toScale, float seconds)
    {
        float from = target.localScale.x;
        if (Mathf.Approximately(from, toScale))
        {
            ClampInsideViewport();
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; // zoom feels responsive even if paused
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            float s = Mathf.Lerp(from, toScale, k);
            target.localScale = new Vector3(s, s, 1f);
            ClampInsideViewport();
            yield return null;
        }

        target.localScale = new Vector3(toScale, toScale, 1f);
        ClampInsideViewport();
        _tween = null;
    }

    // Keep the target from drifting outside the viewport when scaling.
    // Works best if viewport is the parent RectTransform.
    void ClampInsideViewport()
    {
        if (!viewport) return;

        // Get world corners
        Vector3[] vCorners = new Vector3[4];
        Vector3[] tCorners = new Vector3[4];
        viewport.GetWorldCorners(vCorners);
        target.GetWorldCorners(tCorners);

        // Convert to viewport local space so we can clamp anchoredPosition
        Vector2 vMin = WorldToLocal(viewport, vCorners[0]);
        Vector2 vMax = WorldToLocal(viewport, vCorners[2]);

        Vector2 tMin = WorldToLocal(viewport, tCorners[0]);
        Vector2 tMax = WorldToLocal(viewport, tCorners[2]);

        Vector2 offset = Vector2.zero;

        // Only clamp if target is larger than viewport on an axis; otherwise keep centered
        float allowX = Mathf.Max(0f, (tMax.x - tMin.x) - (vMax.x - vMin.x)) * 0.5f;
        float allowY = Mathf.Max(0f, (tMax.y - tMin.y) - (vMax.y - vMin.y)) * 0.5f;

        // Compute how far the target center wandered from viewport center and clamp it
        Vector2 viewCenter = (vMin + vMax) * 0.5f;
        Vector2 targCenter = (tMin + tMax) * 0.5f;

        Vector2 delta = targCenter - viewCenter;

        float clampedX = Mathf.Clamp(delta.x, -allowX, allowX);
        float clampedY = Mathf.Clamp(delta.y, -allowY, allowY);
        offset = new Vector2(clampedX, clampedY) - delta;

        if (offset.sqrMagnitude > 0.00001f)
        {
            // Move the target back by adjusting its anchoredPosition in viewport space
            Vector2 anchored = WorldToLocal(viewport, target.position);
            anchored += offset;
            target.position = LocalToWorld(viewport, anchored);
        }
    }

    static Vector2 WorldToLocal(RectTransform space, Vector3 world)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(space, RectTransformUtility.WorldToScreenPoint(null, world), null, out local);
        return local;
    }

    static Vector3 LocalToWorld(RectTransform space, Vector2 local)
    {
        return space.TransformPoint(local);
    }
}
