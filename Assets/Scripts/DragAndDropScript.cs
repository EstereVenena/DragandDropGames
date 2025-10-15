// Assets/Scripts/DragAndDropScript.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class DragAndDropScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Optional: clamp movement")]
    public RectTransform playArea; // assign Map (RectTransform) or leave null

    [Header("Keyboard Controls (only while dragging)")]
    public float rotateSpeedDeg = 90f;      // Z=CCW, X=CW
    public float scaleSpeedX   = 1.0f;      // Left/Right
    public float scaleSpeedY   = 1.0f;      // Up/Down
    public float shiftMultiplier = 0.35f;   // hold Shift for finer control
    public Vector2 minScale = new Vector2(0.3f, 0.3f);
    public Vector2 maxScale = new Vector2(3.0f, 3.0f);
    public bool preserveAspectWhileScaling = false;

    RectTransform _rt;
    RectTransform _dragSpace; // parent RectTransform if available; falls back to self
    Canvas _canvas; Camera _uiCam;
    Vector2 _offset;
    bool _locked, _isDragging;
    int _origSibling;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (!_rt) { Debug.LogError($"[DragAndDrop] '{name}' needs RectTransform."); enabled = false; return; }

        _dragSpace = _rt.parent as RectTransform;
        if (!_dragSpace)
        {
            _dragSpace = _rt;
            Debug.LogWarning($"[DragAndDrop] '{name}' parent has no RectTransform, using self as drag space.");
        }

        _canvas = GetComponentInParent<Canvas>();
        if (!_canvas) { Debug.LogError($"[DragAndDrop] '{name}' not under a Canvas."); enabled = false; return; }
        _uiCam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;

        GetComponent<Image>().raycastTarget = true;

        // sane clamps
        if (maxScale.x < minScale.x) maxScale.x = minScale.x;
        if (maxScale.y < minScale.y) maxScale.y = minScale.y;
    }

    void Update()
    {
        if (_locked || !_isDragging) return;
        HandleKeyboardRotate();
        HandleKeyboardScale();
    }

    // ---------------- Drag ----------------
    public void OnBeginDrag(PointerEventData e)
    {
        if (_locked) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragSpace, e.position, _uiCam, out var p))
            return;

        _offset = _rt.anchoredPosition - p;
        _isDragging = true;

        // bring to front while dragging
        _origSibling = _rt.GetSiblingIndex();
        _rt.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        if (_locked) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragSpace, e.position, _uiCam, out var p))
            return;

        Vector2 wanted = p + _offset;

        if (playArea)
        {
            // dragSpace local -> world
            Vector3 world = _dragSpace.TransformPoint(new Vector3(wanted.x, wanted.y, 0f));
            // clamp in playArea local
            var r = playArea.rect;
            Vector3 lp = playArea.InverseTransformPoint(world);
            lp.x = Mathf.Clamp(lp.x, r.xMin, r.xMax);
            lp.y = Mathf.Clamp(lp.y, r.yMin, r.yMax);
            // back to dragSpace local
            world = playArea.TransformPoint(lp);
            Vector3 finalLocal = _dragSpace.InverseTransformPoint(world);
            wanted = new Vector2(finalLocal.x, finalLocal.y);
        }

        _rt.anchoredPosition = wanted;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_locked) return;
        _isDragging = false;

        // If you want to restore original sibling order after drop, uncomment:
        // _rt.SetSiblingIndex(_origSibling);
        // (I recommend leaving it on top so placed cars stay above silhouettes.)
    }

    public void Lock()
    {
        _locked = true;
        _isDragging = false;
        var img = GetComponent<Image>();
        if (img) img.raycastTarget = false;
    }

    // ---------------- Keyboard while dragging ----------------
    void HandleKeyboardRotate()
    {
        float mult = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? shiftMultiplier : 1f;

        float rot = 0f;
        if (Input.GetKey(KeyCode.Z)) rot += rotateSpeedDeg * mult * Time.deltaTime; // CCW
        if (Input.GetKey(KeyCode.X)) rot -= rotateSpeedDeg * mult * Time.deltaTime; // CW

        if (Mathf.Abs(rot) > 0.0001f)
            _rt.localRotation = Quaternion.Euler(0f, 0f, _rt.localEulerAngles.z + rot);
    }

    void HandleKeyboardScale()
    {
        Vector3 s = _rt.localScale;

        float mult = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? shiftMultiplier : 1f;
        float dx = 0f, dy = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) dx += scaleSpeedX * mult * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow))  dx -= scaleSpeedX * mult * Time.deltaTime;
        if (Input.GetKey(KeyCode.UpArrow))    dy += scaleSpeedY * mult * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow))  dy -= scaleSpeedY * mult * Time.deltaTime;

        if (preserveAspectWhileScaling && (Mathf.Abs(dx) > 0.0001f || Mathf.Abs(dy) > 0.0001f))
        {
            float delta = Mathf.Abs(dx) > Mathf.Abs(dy) ? dx : dy;
            float nx = Mathf.Clamp(s.x + delta, minScale.x, maxScale.x);
            float ny = Mathf.Clamp(s.y + delta, minScale.y, maxScale.y);
            _rt.localScale = new Vector3(nx, ny, s.z);
        }
        else
        {
            bool changed = false;
            if (Mathf.Abs(dx) > 0.0001f) { s.x = Mathf.Clamp(s.x + dx, minScale.x, maxScale.x); changed = true; }
            if (Mathf.Abs(dy) > 0.0001f) { s.y = Mathf.Clamp(s.y + dy, minScale.y, maxScale.y); changed = true; }
            if (changed) _rt.localScale = s;
        }
    }
}
