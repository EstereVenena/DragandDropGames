using UnityEngine;

[ExecuteAlways]
public class ScreenBoundriesScript : MonoBehaviour
{
    [Header("Optional UI Area Bounds (RectTransform)")]
    public RectTransform playArea;

    [Header("World Bounds (used when playArea is null)")]
    public Rect worldBounds = new Rect(-960, -540, 1920, 1080);
    [Range(0f, 0.5f)] public float padding = 0.02f;

    [Header("Camera Reference")]
    public Camera targetCamera;

    [Header("Calculated Bounds (Read-Only)")]
    public float minCamX;
    public float maxCamX;
    public float minCamY;
    public float maxCamY;

    private float lastOrthoSize;
    private float lastAspect;
    private Vector3 lastCamPos;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        RecalculateBounds();
    }

    void Update()
    {
        if (targetCamera == null)
            return;

        bool changed = false;

        if (targetCamera.orthographic)
        {
            if (!Mathf.Approximately(targetCamera.orthographicSize, lastOrthoSize))
                changed = true;
        }

        if (!Mathf.Approximately(targetCamera.aspect, lastAspect))
            changed = true;

        if (targetCamera.transform.position != lastCamPos)
            changed = true;

        if (changed)
            RecalculateBounds();
    }

    public void RecalculateBounds()
    {
        if (targetCamera == null)
            return;

        if (playArea != null)
        {
            // Use UI RectTransform bounds
            var corners = new Vector3[4];
            playArea.GetWorldCorners(corners);
            Vector3 bl = corners[0];
            Vector3 tr = corners[2];
            worldBounds = new Rect(bl.x, bl.y, tr.x - bl.x, tr.y - bl.y);
        }

        float wbMinX = worldBounds.xMin;
        float wbMaxX = worldBounds.xMax;
        float wbMinY = worldBounds.yMin;
        float wbMaxY = worldBounds.yMax;

        if (targetCamera.orthographic)
        {
            float halfH = targetCamera.orthographicSize;
            float halfW = halfH * targetCamera.aspect;

            if (halfW * 2f >= (wbMaxX - wbMinX))
                minCamX = maxCamX = (wbMinX + wbMaxX) * 0.5f;
            else
            {
                minCamX = wbMinX + halfW;
                maxCamX = wbMaxX - halfW;
            }

            if (halfH * 2f >= (wbMaxY - wbMinY))
                minCamY = maxCamY = (wbMinY + wbMaxY) * 0.5f;
            else
            {
                minCamY = wbMinY + halfH;
                maxCamY = wbMaxY - halfH;
            }
        }

        lastOrthoSize = targetCamera.orthographicSize;
        lastAspect = targetCamera.aspect;
        lastCamPos = targetCamera.transform.position;
    }

    public Vector2 GetClampedPosition(Vector3 curPosition)
    {
        float shrinkW = worldBounds.width * padding;
        float shrinkH = worldBounds.height * padding;

        float wbMinX = worldBounds.xMin + shrinkW;
        float wbMaxX = worldBounds.xMax - shrinkW;
        float wbMinY = worldBounds.yMin + shrinkH;
        float wbMaxY = worldBounds.yMax - shrinkH;

        float cx = Mathf.Clamp(curPosition.x, wbMinX, wbMaxX);
        float cy = Mathf.Clamp(curPosition.y, wbMinY, wbMaxY);
        return new Vector2(cx, cy);
    }

    public Vector3 GetClampedCameraPosition(Vector3 desiredCamCenter)
    {
        float cx = Mathf.Clamp(desiredCamCenter.x, minCamX, maxCamX);
        float cy = Mathf.Clamp(desiredCamCenter.y, minCamY, maxCamY);
        return new Vector3(cx, cy, desiredCamCenter.z);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new((worldBounds.xMin + worldBounds.xMax) * 0.5f, (worldBounds.yMin + worldBounds.yMax) * 0.5f, 0f);
        Vector3 size = new(Mathf.Abs(worldBounds.width), Mathf.Abs(worldBounds.height), 0f);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.cyan;
        Vector3 camCenter = new((minCamX + maxCamX) * 0.5f, (minCamY + maxCamY) * 0.5f, 0f);
        Vector3 camSize = new(Mathf.Abs(maxCamX - minCamX), Mathf.Abs(maxCamY - minCamY), 0f);
        Gizmos.DrawWireCube(camCenter, camSize);
    }
#endif
}
