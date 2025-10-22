using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DropPlaceScript : MonoBehaviour, IDropHandler
{
    [Header("Matching rules")]
    [Tooltip("Must match the car's Tag.")]
    public string requiredTag = "CarA";

    [Tooltip("Require rotation to be within tolerance?")]
    public bool requireRotation = true;

    [Tooltip("Allowed angle difference (degrees).")]
    public float rotationToleranceDeg = 20f;

    [Tooltip("Require scale magnitude to be within tolerance? (mirror signs ignored)")]
    public bool requireScale = true;

    [Range(0f, 0.5f)]
    [Tooltip("Per-axis magnitude tolerance (0.20 = ±20%).")]
    public float sizeTolerancePercent = 0.20f;

    [Tooltip("Only snap when within this distance (pixels/world). 0 = ignore.")]
    public float snapDistance = 300f;

    [Header("What happens on correct match")]
    public bool snapOnCorrect = true;
    public bool lockOnCorrect = true;

    [Header("Snap target (optional)")]
    [Tooltip("If set, snap to this transform (usually the WHITE silhouette). If null, uses this RectTransform.")]
    public RectTransform snapAnchor;

    [Header("Debug")]
    public bool debugLogs = true;

    RectTransform _slot;
    bool _filled;

    void Awake()
    {
        _slot = GetComponent<RectTransform>();
        if (!snapAnchor) snapAnchor = _slot;

        var img = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        img.raycastTarget = true;
    }

    // EventSystem fallback
    public void OnDrop(PointerEventData e)
    {
        if (e.pointerDrag) TryAccept(e.pointerDrag);
    }

    /// <summary>Main entry used by DragAndDropScript. Returns true if accepted.</summary>
    public bool TryAccept(GameObject dragged)
    {
        if (_filled) { if (debugLogs) Debug.Log("[DropPlace] already filled; ignore.", this); return false; }
        if (!dragged) return false;

        var carRT = dragged.GetComponent<RectTransform>();
        if (!carRT) { if (debugLogs) Debug.Log("[DropPlace] dragged object has no RectTransform.", this); return false; }

        // 1) Tag check
        if (!string.IsNullOrEmpty(requiredTag) && dragged.tag != requiredTag)
        {
            if (debugLogs) Debug.Log($"[DropPlace] tag mismatch → need '{requiredTag}', got '{dragged.tag}'.", this);
            return false;
        }

        // 2) Proximity check (world space = robust)
        if (snapDistance > 0f)
        {
            float d = Vector2.Distance(carRT.position, snapAnchor.position);
            if (d > snapDistance)
            {
                if (debugLogs) Debug.Log($"[DropPlace] too far → dist {d:F1} > {snapDistance}.", this);
                return false;
            }
        }

        // 3) Rotation check (world Z)
        if (requireRotation)
        {
            float diffZ = Mathf.Abs(Mathf.DeltaAngle(carRT.eulerAngles.z, snapAnchor.eulerAngles.z));
            if (diffZ > rotationToleranceDeg)
            {
                if (debugLogs) Debug.Log($"[DropPlace] rotation diff {diffZ:F1}° > tol {rotationToleranceDeg}°.", this);
                return false;
            }
        }

        // 4) Scale check (lossyScale magnitudes; ignore mirror sign)
        if (requireScale)
        {
            Vector3 c = carRT.lossyScale;
            Vector3 s = snapAnchor.lossyScale;
            if (!WithinTol(Mathf.Abs(c.x), Mathf.Abs(s.x), sizeTolerancePercent))
            {
                if (debugLogs) Debug.Log($"[DropPlace] scale X out of tol → car|slot {Mathf.Abs(c.x):F2}|{Mathf.Abs(s.x):F2} ±{sizeTolerancePercent*100f:F0}%.", this);
                return false;
            }
            if (!WithinTol(Mathf.Abs(c.y), Mathf.Abs(s.y), sizeTolerancePercent))
            {
                if (debugLogs) Debug.Log($"[DropPlace] scale Y out of tol → car|slot {Mathf.Abs(c.y):F2}|{Mathf.Abs(s.y):F2} ±{sizeTolerancePercent*100f:F0}%.", this);
                return false;
            }
        }

        // ====== ✅ Correct placement ======
        if (snapOnCorrect) SnapToAnchor(carRT);

        if (lockOnCorrect)
        {
            var drag = carRT.GetComponent<DragAndDropScript>();
            if (drag) drag.Lock($"DropPlace:{name}");
        }

        _filled = true;
        if (debugLogs) Debug.Log($"[DropPlace] ✅ Correct! Count +1 on '{name}'.", this);
        ProgressCounter.Instance?.AddOne();

        return true;
    }

    void SnapToAnchor(RectTransform carRT)
    {
        carRT.position = snapAnchor.position;
        carRT.rotation = snapAnchor.rotation;

        float sx = Mathf.Sign(carRT.localScale.x == 0 ? 1f : carRT.localScale.x);
        float sy = Mathf.Sign(carRT.localScale.y == 0 ? 1f : carRT.localScale.y);
        Vector3 s = snapAnchor.localScale;
        carRT.localScale = new Vector3(sx * Mathf.Abs(s.x), sy * Mathf.Abs(s.y), carRT.localScale.z);
    }

    static bool WithinTol(float a, float b, float pct)
    {
        float min = b * (1f - pct);
        float max = b * (1f + pct);
        return a >= min && a <= max;
    }
}
