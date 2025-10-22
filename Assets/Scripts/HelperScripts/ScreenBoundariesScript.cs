using UnityEngine;

public class ScreenBoundriesScript : MonoBehaviour
{
    [Header("Use a UI Panel (RectTransform) as the bounds (recommended)")]
    public RectTransform playArea;

    [Header("Or use Camera screen with padding (when playArea is not assigned)")]
    [Range(0f, 0.45f)] public float paddingPercent = 0.02f;

    [Header("Computed (World Units)")]
    public float minX, maxX, minY, maxY;

    // Legacy fields for drag code compatibility
    [HideInInspector] public Vector3 screenPoint;
    [HideInInspector] public Vector3 offset;

    void Awake() => Recalculate();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        Recalculate();
    }
#endif

    public void Recalculate()
    {
        if (playArea)
        {
            var corners = new Vector3[4];
            playArea.GetWorldCorners(corners); // 0 BL, 2 TR
            Vector3 bl = corners[0];
            Vector3 tr = corners[2];
            minX = bl.x; minY = bl.y; maxX = tr.x; maxY = tr.y;
        }
        else
        {
            Camera cam = Camera.main;
            if (!cam) { Debug.LogWarning("[ScreenBoundries] No Camera.main"); return; }

            float depth = Mathf.Abs(cam.transform.position.z);
            Vector3 bl = cam.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 tr = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, depth));

            float padX = (tr.x - bl.x) * paddingPercent;
            float padY = (tr.y - bl.y) * paddingPercent;

            minX = bl.x + padX; maxX = tr.x - padX;
            minY = bl.y + padY; maxY = tr.y - padY;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size   = new(Mathf.Abs(maxX - minX), Mathf.Abs(maxY - minY), 0f);
        Gizmos.DrawWireCube(center, size);
    }

    /// <summary>Clamp a world position to the current bounds, return as Vector2.</summary>
    public Vector2 GetClampedPosition(Vector3 worldPos)
    {
        float x = Mathf.Clamp(worldPos.x, minX, maxX);
        float y = Mathf.Clamp(worldPos.y, minY, maxY);
        return new Vector2(x, y);
    }

    /// <summary>Clamp a world position to the current bounds, preserving Z.</summary>
    public Vector3 ClampWorldPosition(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.Clamp(worldPos.x, minX, maxX),
            Mathf.Clamp(worldPos.y, minY, maxY),
            worldPos.z
        );
    }
}
