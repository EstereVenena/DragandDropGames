using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 150f;
    public float maxZoom = 300f;
    public float zoomStep = 10f; // For desktop scroll
    public float touchZoomSpeed = 0.5f; // For mobile pinch

    [Header("Pan Settings")]
    public float panSpeed = 6f;
    public float touchPanSpeed = 0.5f;

    [Header("World Boundaries")]
    public Vector2 worldMin = new Vector2(-500, -500);
    public Vector2 worldMax = new Vector2(500, 500);

    private Camera cam;
    private Vector3 lastPanPosition;
    private int panFingerId; // Touch ID for mobile panning

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            Debug.LogError("CameraScript requires a Camera component.");
    }

    void Update()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        HandleMousePan();
        HandleMouseZoom();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
#endif
        ClampCameraPosition();
    }

    // --- 🖱️ Desktop Controls ---
    void HandleMousePan()
    {
        if (Input.GetMouseButton(2)) // Middle mouse button drag
        {
            float x = -Input.GetAxis("Mouse X") * panSpeed;
            float y = -Input.GetAxis("Mouse Y") * panSpeed;
            transform.Translate(x, y, 0);
        }
    }

    void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0 && cam.orthographicSize > minZoom)
            cam.orthographicSize -= zoomStep;
        else if (scroll < 0 && cam.orthographicSize < maxZoom)
            cam.orthographicSize += zoomStep;
    }

    // --- 🤏 Mobile Controls ---
    void HandleTouchInput()
    {
        if (Input.touchCount == 1) // Single finger drag
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastPanPosition = touch.position;
                panFingerId = touch.fingerId;
            }
            else if (touch.fingerId == panFingerId && touch.phase == TouchPhase.Moved)
            {
                Vector3 delta = touch.deltaPosition * touchPanSpeed * Time.deltaTime;
                transform.Translate(-delta.x, -delta.y, 0);
            }
        }
        else if (Input.touchCount == 2) // Pinch zoom
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 prevTouch0 = touch0.position - touch0.deltaPosition;
            Vector2 prevTouch1 = touch1.position - touch1.deltaPosition;

            float prevDistance = (prevTouch0 - prevTouch1).magnitude;
            float currentDistance = (touch0.position - touch1.position).magnitude;
            float delta = currentDistance - prevDistance;

            cam.orthographicSize -= delta * touchZoomSpeed * Time.deltaTime;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    // --- 🧱 Clamp Camera within World Bounds ---
    void ClampCameraPosition()
    {
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, worldMin.x + horzExtent, worldMax.x - horzExtent);
        pos.y = Mathf.Clamp(pos.y, worldMin.y + vertExtent, worldMax.y - vertExtent);
        transform.position = pos;
    }
}
