using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class CameraScript : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 150f;
    public float maxZoom = 300f;
    public float zoomStep = 10f;              // Desktop scroll
    public float touchZoomSpeed = 0.5f;       // Mobile pinch
    public float doubleTapResetDuration = 0.25f;

    [Header("Pan Settings")]
    public float panSpeed = 6f;               // Desktop
    public float touchPanSpeed = 0.5f;        // Mobile

    [Header("World Boundaries")]
    public Vector2 worldMin = new Vector2(-500, -500);
    public Vector2 worldMax = new Vector2(500, 500);
    public ScreenBoundriesScript screenBoundries; // Optional

    [Header("Debug / Editor")]
    [SerializeField] private bool simulateMobileInEditor = false;

    private Camera cam;
    private Vector3 lastPanPosition;
    private int panFingerId = -1;
    private bool isTouchPanning = false;

    // Double-tap detection
    private float lastTapTime = 0f;
    public float doubleTapMaxDelay = 0.4f;
    public float doubleTapMaxDistance = 100f;
    private Vector2 lastTouchPos;
    private float startZoom;

    void Awake()
    {
        cam = GetComponent<Camera>();
        startZoom = cam.orthographicSize;

        if (screenBoundries == null)
            screenBoundries = FindFirstObjectByType<ScreenBoundriesScript>();
    }

    void Start()
    {
        if (screenBoundries != null)
        {
            screenBoundries.RecalculateBounds();
            transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (simulateMobileInEditor)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMousePan();
            HandleMouseZoom();
        }
#elif UNITY_STANDALONE
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
        if (Input.GetMouseButton(2)) // Middle mouse drag
        {
            float x = -Input.GetAxis("Mouse X") * panSpeed;
            float y = -Input.GetAxis("Mouse Y") * panSpeed;
            transform.Translate(x, y, 0, Space.World);
        }
    }

    void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            cam.orthographicSize -= scroll * zoomStep * 10f;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    // --- 🤏 Mobile Controls ---
    void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (IsTouchingUIButton(t.position)) return;

            if (t.phase == TouchPhase.Began)
            {
                // Double-tap zoom reset
                float dt = Time.time - lastTapTime;
                if (dt <= doubleTapMaxDelay && Vector2.Distance(t.position, lastTouchPos) <= doubleTapMaxDistance)
                {
                    StartCoroutine(ResetZoomSmooth());
                    lastTapTime = 0f;
                }
                else
                {
                    lastTapTime = Time.time;
                }

                lastTouchPos = t.position;
                lastPanPosition = t.position;
                panFingerId = t.fingerId;
                isTouchPanning = true;
            }
            else if (t.phase == TouchPhase.Moved && isTouchPanning && t.fingerId == panFingerId)
            {
                Vector2 delta = t.position - (Vector2)lastPanPosition;
                transform.Translate(ScreenDeltaToWorldDelta(delta) * touchPanSpeed * Time.deltaTime * 60f, Space.World);
                lastPanPosition = t.position;
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                isTouchPanning = false;
                panFingerId = -1;
            }
        }
        else if (Input.touchCount == 2)
        {
            HandlePinch();
        }
    }

    void HandlePinch()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        Vector2 prevPos0 = t0.position - t0.deltaPosition;
        Vector2 prevPos1 = t1.position - t1.deltaPosition;

        float prevDist = (prevPos0 - prevPos1).magnitude;
        float currDist = (t0.position - t1.position).magnitude;

        // Adjusted for consistent zoom across devices
        float delta = (currDist - prevDist) * 0.01f;
        cam.orthographicSize -= delta * touchZoomSpeed;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    bool IsTouchingUIButton(Vector2 touchPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = touchPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                return true;
        }

        return false;
    }

    Vector3 ScreenDeltaToWorldDelta(Vector2 delta)
    {
        float worldPerPixel = (cam.orthographicSize * 2f) / Screen.height;
        return new Vector3(-delta.x * worldPerPixel, -delta.y * worldPerPixel, 0f);
    }

    // --- 🔄 Smooth Zoom Reset (Double Tap) ---
    IEnumerator ResetZoomSmooth()
    {
        float duration = doubleTapResetDuration;
        float elapsed = 0f;
        float initialZoom = cam.orthographicSize;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(initialZoom, startZoom, elapsed / duration);
            if (screenBoundries != null)
            {
                screenBoundries.RecalculateBounds();
                transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
            }
            yield return null;
        }

        cam.orthographicSize = startZoom;
    }

    // --- 🧱 Clamp Camera within World Bounds ---
    void ClampCameraPosition()
    {
        if (screenBoundries != null)
        {
            screenBoundries.RecalculateBounds();
            transform.position = screenBoundries.GetClampedCameraPosition(transform.position);
            return;
        }

        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, worldMin.x + horzExtent, worldMax.x - horzExtent);
        pos.y = Mathf.Clamp(pos.y, worldMin.y + vertExtent, worldMax.y - vertExtent);
        transform.position = pos;
    }
}
