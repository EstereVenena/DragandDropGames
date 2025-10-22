using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class DragAndDropScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ----------------------------- References -----------------------------
    [Header("References")]
    public RectTransform playArea;                  // Assign Map (RectTransform). If null we try to find by tag/name.
    public string playAreaTag = "PlayArea";         // Optional tag to auto-find playArea.
    public string noDropZoneTag = "NoDropZone";     // Optional tag to auto-collect no-drop zones.
    public List<RectTransform> noDropZones = new List<RectTransform>();
    public TMP_Text hintText;                       // Optional hint overlay.

    // ------------------------ Movement & Controls -------------------------
    [Header("Movement & Controls")]
    public float rotateSpeedDeg = 90f;
    public float scaleSpeedX = 1.0f;
    public float scaleSpeedY = 1.0f;
    public float shiftMultiplier = 0.35f;
    public Vector2 minScale = new Vector2(0.3f, 0.3f);
    public Vector2 maxScale = new Vector2(3.0f, 3.0f);
    public bool preserveAspectWhileScaling = false;

    // ------------------- Mirroring / Reset / Persistence ------------------
    [Header("Mirror / Reset / Persistence")]
    public bool allowMirroring = true;
    public bool allowReset = true;
    public Vector2 defaultScale = new Vector2(1f, 1f);
    public bool saveTransformState = true;          // Persist pos/rot/scale between runs
    public string saveKeyPrefix = "DragCar_";

    // --------------------------- Visuals / UX -----------------------------
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color forbiddenColor = new Color(1f, 0.35f, 0.35f, 1f);
    public float forbiddenEdgePadding = 8f;
    public bool showGhostPreview = true;

    // --------------------------- Drop behavior ----------------------------
    [Header("Drop Behavior")]
    [Tooltip("If ON: when you drop ON a slot and it rejects, return to where the drag started. If OFF: leave the item where you released it.")]
    public bool restoreOnReject = false;

    // ----------------------------- Internals ------------------------------
    RectTransform _rt;
    RectTransform _dragSpace;
    Canvas _canvas;
    Camera _uiCam;
    Image _img;
    CanvasGroup _cg;
    bool _locked, _isDragging;
    Vector2 _offset, _lastValidAnchored;
    int _origSibling;

    // restore if drop rejected
    Transform _startParent;
    Vector3 _startPos, _startScale;
    Quaternion _startRot;

    GameObject _ghost;
    float _lastPinchDist = -1f;
    float _lastTwistAngle = 0f;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _img = GetComponent<Image>();

        _dragSpace = _rt.parent as RectTransform ?? _rt;

        _canvas = GetComponentInParent<Canvas>();
        _uiCam = (_canvas && _canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;

        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _img.raycastTarget = true;

        if (!playArea) playArea = SafeFindPlayArea(playAreaTag);
        if (noDropZones.Count == 0) SafeCollectNoDropZones(noDropZoneTag, noDropZones);

        if (hintText) hintText.text = "Q/E mirror | Z/X rotate | Arrows scale | R reset";

        if (saveTransformState) LoadSavedTransform();

        _lastValidAnchored = _rt.anchoredPosition;

        CreateGhost(); // harmless if disabled by flag later
    }

    void Update()
    {
        if (_locked) return;
        if (_isDragging)
        {
            HandleKeyboardRotate();
            HandleKeyboardScale();
            HandleKeyboardMirrorAndReset();
            HandleTouchControls();
        }
    }

    // ----------------------------- Drag begin -----------------------------
    public void OnBeginDrag(PointerEventData e)
    {
        if (_locked) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragSpace, e.position, _uiCam, out var p))
            return;

        _offset = _rt.anchoredPosition - p;
        _isDragging = true;

        // Tell the world we're dragging (for bombs, etc.)
        DragState.Begin(_rt);

        _origSibling = _rt.GetSiblingIndex();
        _rt.SetAsLastSibling();

        _lastValidAnchored = _rt.anchoredPosition;

        // Save start transform for possible restore
        _startParent = _rt.parent;
        _startPos    = _rt.position;
        _startRot    = _rt.rotation;
        _startScale  = _rt.localScale;

        // Let raycasts pass through this item so the slot receives OnDrop
        _cg.blocksRaycasts = false;
        _img.raycastTarget = false;

        // Safety: any decorative children shouldn't block
        DisableDecorativeRaycasts(gameObject);

        if (_ghost) _ghost.SetActive(true);
    }

    // ------------------------------ Drag move -----------------------------
    public void OnDrag(PointerEventData e)
    {
        if (_locked) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragSpace, e.position, _uiCam, out var p))
            return;

        Vector2 wanted = p + _offset;

        // Clamp to playArea
        if (playArea)
        {
            Vector3 world = _dragSpace.TransformPoint(new Vector3(wanted.x, wanted.y, 0f));
            var r = playArea.rect;
            Vector3 lp = playArea.InverseTransformPoint(world);
            lp.x = Mathf.Clamp(lp.x, r.xMin, r.xMax);
            lp.y = Mathf.Clamp(lp.y, r.yMin, r.yMax);
            world = playArea.TransformPoint(lp);
            wanted = _dragSpace.InverseTransformPoint(world);
        }

        _rt.anchoredPosition = wanted;

        bool inForbidden = IsInsideAnyNoDropZone(_rt.anchoredPosition);
        _img.color = inForbidden ? forbiddenColor : normalColor;

        if (!inForbidden) _lastValidAnchored = _rt.anchoredPosition;

        if (showGhostPreview) UpdateGhost();
    }

    // ------------------------------ Drag end ------------------------------
    public void OnEndDrag(PointerEventData e)
    {
        if (_locked) return;

        // Drag finished
        _isDragging = false;
        DragState.End();

        // Put our own raycasts back (AFTER we've resolved the drop)
        _cg.blocksRaycasts = true;
        _img.raycastTarget = true;

        _img.color = normalColor;
        if (_ghost) _ghost.SetActive(false);

        // If we ended inside any no-drop zone, push out a bit
        if (IsInsideAnyNoDropZone(_rt.anchoredPosition))
        {
            Vector2 fixedPos = PushOutsideNoDropZones(_rt.anchoredPosition, forbiddenEdgePadding);
            _rt.anchoredPosition = fixedPos;
        }

        // Resolve whatever we hit to the actual slot (parent with DropPlaceScript)
        var hitGO = e.pointerCurrentRaycast.gameObject;
        var slot  = hitGO ? hitGO.GetComponentInParent<DropPlaceScript>() : null;

        bool accepted = false;
        bool explicitlyRejected = false;

        if (slot)
        {
            accepted = slot.TryAccept(gameObject);
            explicitlyRejected = !accepted; // a slot was targeted but it said "nope"
        }

        // Only snap back if a slot rejected AND the toggle is ON
        if (explicitlyRejected && restoreOnReject)
        {
            _rt.SetParent(_startParent, worldPositionStays: true);
            _rt.position   = _startPos;
            _rt.rotation   = _startRot;
            _rt.localScale = _startScale;
        }

        // Final clamp so nothing ends off-screen
        if (playArea)
        {
            Vector3[] c = new Vector3[4];
            playArea.GetWorldCorners(c);
            Vector3 p = _rt.position;
            p.x = Mathf.Clamp(p.x, c[0].x, c[2].x);
            p.y = Mathf.Clamp(p.y, c[0].y, c[2].y);
            _rt.position = p;
        }

        _lastValidAnchored = _rt.anchoredPosition;

        if (saveTransformState) SaveTransformState();
    }

    // --------------------------- Public: Lock -----------------------------
    /// <summary>Called by DropPlaceScript on correct placement.</summary>
    public void Lock(string reason = "")
    {
        Debug.Log($"[Drag] LOCK '{name}' reason={reason}", this);
        _locked = true;
        _isDragging = false;

        if (_ghost) _ghost.SetActive(false);
        _img.raycastTarget = false;
        if (_cg) _cg.blocksRaycasts = false;

        // Ensure drag state ends if locked mid-drag
        if (DragState.IsDragging && DragState.Current == _rt)
            DragState.End();
    }

    // --------------------------- Keyboard input ---------------------------
    void HandleKeyboardRotate()
    {
        float mult = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? shiftMultiplier : 1f;

        float rot = 0f;
        if (Input.GetKey(KeyCode.Z)) rot += rotateSpeedDeg * mult * Time.deltaTime; // CCW
        if (Input.GetKey(KeyCode.X)) rot -= rotateSpeedDeg * mult * Time.deltaTime; // CW

        if (Mathf.Abs(rot) > 0.0001f)
            _rt.localRotation = Quaternion.Euler(0, 0, _rt.localEulerAngles.z + rot);
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

        if (preserveAspectWhileScaling && (Mathf.Abs(dx) > 0.001f || Mathf.Abs(dy) > 0.001f))
        {
            float delta = Mathf.Abs(dx) > Mathf.Abs(dy) ? dx : dy;
            float ax = Mathf.Clamp(Mathf.Abs(s.x) + delta, minScale.x, maxScale.x);
            float ay = Mathf.Clamp(Mathf.Abs(s.y) + delta, minScale.y, maxScale.y);
            float sx = Mathf.Sign(s.x == 0 ? 1f : s.x);
            float sy = Mathf.Sign(s.y == 0 ? 1f : s.y);
            _rt.localScale = new Vector3(sx * ax, sy * ay, s.z);
        }
        else
        {
            if (Mathf.Abs(dx) > 0.001f)
                s.x = Mathf.Sign(s.x) * Mathf.Clamp(Mathf.Abs(s.x) + dx, minScale.x, maxScale.x);
            if (Mathf.Abs(dy) > 0.001f)
                s.y = Mathf.Sign(s.y) * Mathf.Clamp(Mathf.Abs(s.y) + dy, minScale.y, maxScale.y);
            _rt.localScale = s;
        }
    }

    void HandleKeyboardMirrorAndReset()
    {
        if (!allowMirroring) return;
        Vector3 s = _rt.localScale;

        if (Input.GetKeyDown(KeyCode.Q)) { s.x *= -1f; _rt.localScale = s; } // mirror X
        if (Input.GetKeyDown(KeyCode.E)) { s.y *= -1f; _rt.localScale = s; } // mirror Y

        if (allowReset && Input.GetKeyDown(KeyCode.R))
        {
            float sx = Mathf.Sign(_rt.localScale.x);
            float sy = Mathf.Sign(_rt.localScale.y);
            _rt.localScale = new Vector3(sx * defaultScale.x, sy * defaultScale.y, 1f);
            _rt.localRotation = Quaternion.identity;
        }
    }

    // --------------------------- Touch input ------------------------------
    void HandleTouchControls()
    {
        if (Input.touchCount < 2) { _lastPinchDist = -1f; return; }

        Touch t1 = Input.GetTouch(0), t2 = Input.GetTouch(1);
        float curDist  = Vector2.Distance(t1.position, t2.position);
        float curAngle = Mathf.Atan2(t2.position.y - t1.position.y, t2.position.x - t1.position.x) * Mathf.Rad2Deg;

        if (_lastPinchDist > 0f)
        {
            float diff = (curDist - _lastPinchDist) * 0.005f;
            float ax = Mathf.Clamp(Mathf.Abs(_rt.localScale.x) + diff, minScale.x, maxScale.x);
            float ay = Mathf.Clamp(Mathf.Abs(_rt.localScale.y) + diff, minScale.y, maxScale.y);
            _rt.localScale = new Vector3(Mathf.Sign(_rt.localScale.x) * ax, Mathf.Sign(_rt.localScale.y) * ay, 1f);
        }

        float deltaRot = curAngle - _lastTwistAngle;
        _rt.localRotation = Quaternion.Euler(0, 0, _rt.localEulerAngles.z + deltaRot);

        _lastPinchDist = curDist;
        _lastTwistAngle = curAngle;
    }

    // --------------------------- Ghost preview ----------------------------
    void CreateGhost()
    {
        _ghost = new GameObject(name + "_Ghost");
        _ghost.transform.SetParent(_rt.parent, worldPositionStays: false);

        var img = _ghost.AddComponent<Image>();
        img.sprite = _img.sprite;
        img.preserveAspect = true;
        img.color = new Color(1, 1, 1, 0.25f);
        img.raycastTarget = false; // never block pointer

        var rt = _ghost.GetComponent<RectTransform>();
        rt.sizeDelta = _rt.sizeDelta;
        rt.localScale = _rt.localScale;
        rt.localRotation = _rt.localRotation;
        rt.anchoredPosition = _rt.anchoredPosition;

        _ghost.SetActive(false);
    }

    DropPlaceScript FindMatchingSlot()
    {
        var meTag = gameObject.tag;

        // Modern API: include inactive, no sorting (fast)
        var allSlots = Object.FindObjectsByType<DropPlaceScript>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var s in allSlots)
        {
            if (!s) continue;
            if (string.IsNullOrEmpty(s.requiredTag) || s.requiredTag != meTag) continue;
            return s;
        }
        return null;
    }

    void UpdateGhost()
    {
        if (!_ghost) return;

        var slot = FindMatchingSlot();
        if (!slot)
        {
            if (_ghost.activeSelf) _ghost.SetActive(false);
            return;
        }

        var anchor = slot.snapAnchor ? slot.snapAnchor : slot.GetComponent<RectTransform>();
        if (!_ghost.activeSelf) _ghost.SetActive(true);

        var grt = _ghost.GetComponent<RectTransform>();

        // parent next to anchor to avoid local-space offsets
        if (grt.parent != anchor.parent)
            grt.SetParent(anchor.parent, worldPositionStays: true);

        // copy world xform for pixel-perfect overlay
        grt.position = anchor.position;
        grt.rotation = anchor.rotation;

        // match world scale robustly
        Vector3 aLossy = anchor.lossyScale;
        Vector3 gLossy = grt.lossyScale;
        if (gLossy.x != 0 && gLossy.y != 0 && gLossy.z != 0)
        {
            grt.localScale = new Vector3(
                grt.localScale.x * (aLossy.x / gLossy.x),
                grt.localScale.y * (aLossy.y / gLossy.y),
                grt.localScale.z * (aLossy.z / gLossy.z)
            );
        }
    }

    // ---------------------- No-Drop-zone utilities ------------------------
    bool IsInsideAnyNoDropZone(Vector2 playLocal)
    {
        if (!playArea) return false;
        foreach (var zone in noDropZones)
        {
            if (!zone) continue;
            if (GetZoneRect(zone, out Rect zr) && zr.Contains(playLocal))
                return true;
        }
        return false;
    }

    Vector2 PushOutsideNoDropZones(Vector2 pos, float pad)
    {
        foreach (var zone in noDropZones)
        {
            if (!zone) continue;
            if (GetZoneRect(zone, out Rect zr) && zr.Contains(pos))
            {
                float dL = Mathf.Abs(pos.x - zr.xMin), dR = Mathf.Abs(zr.xMax - pos.x);
                float dB = Mathf.Abs(pos.y - zr.yMin), dT = Mathf.Abs(zr.yMax - pos.y);
                float minD = Mathf.Min(Mathf.Min(dL, dR), Mathf.Min(dB, dT));
                if (minD == dL) pos.x = zr.xMin - pad;
                else if (minD == dR) pos.x = zr.xMax + pad;
                else if (minD == dB) pos.y = zr.yMin - pad;
                else pos.y = zr.yMax + pad;
            }
        }
        return pos;
    }

    bool GetZoneRect(RectTransform zone, out Rect r)
    {
        r = default;
        if (!playArea) return false;
        Vector3[] wc = new Vector3[4];
        zone.GetWorldCorners(wc);
        for (int i = 0; i < 4; i++) wc[i] = playArea.InverseTransformPoint(wc[i]);
        float minX = Mathf.Min(wc[0].x, wc[2].x), maxX = Mathf.Max(wc[0].x, wc[2].x);
        float minY = Mathf.Min(wc[0].y, wc[2].y), maxY = Mathf.Max(wc[0].y, wc[2].y);
        r = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    // ----------------------------- Persistence ----------------------------
    void SaveTransformState()
    {
        string key = saveKeyPrefix + name;
        PlayerPrefs.SetFloat(key + "_px", _rt.anchoredPosition.x);
        PlayerPrefs.SetFloat(key + "_py", _rt.anchoredPosition.y);
        PlayerPrefs.SetFloat(key + "_rz", _rt.localEulerAngles.z);
        PlayerPrefs.SetFloat(key + "_sx", _rt.localScale.x);
        PlayerPrefs.SetFloat(key + "_sy", _rt.localScale.y);
    }

    void LoadSavedTransform()
    {
        string key = saveKeyPrefix + name;
        if (!PlayerPrefs.HasKey(key + "_px")) return;
        _rt.anchoredPosition = new Vector2(PlayerPrefs.GetFloat(key + "_px"), PlayerPrefs.GetFloat(key + "_py"));
        _rt.localRotation    = Quaternion.Euler(0, 0, PlayerPrefs.GetFloat(key + "_rz"));
        _rt.localScale       = new Vector3(PlayerPrefs.GetFloat(key + "_sx"), PlayerPrefs.GetFloat(key + "_sy"), 1);
    }

    // ---------------------------- Safe finders ----------------------------
    RectTransform SafeFindPlayArea(string tag)
    {
        try
        {
            var goByTag = GameObject.FindGameObjectWithTag(tag);
            if (goByTag) return goByTag.GetComponent<RectTransform>();
        }
        catch { /* tag not defined */ }

        var byName = GameObject.Find("Map") ?? GameObject.Find("PlayArea");
        return byName ? byName.GetComponent<RectTransform>() : null;
    }

    void SafeCollectNoDropZones(string tag, List<RectTransform> outList)
    {
        if (string.IsNullOrEmpty(tag)) return;
        try
        {
            var gos = GameObject.FindGameObjectsWithTag(tag);
            foreach (var go in gos)
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt) outList.Add(rt);
            }
        }
        catch { /* tag not defined */ }
    }

    // ------------------------ Raycast safety helper -----------------------
    void DisableDecorativeRaycasts(GameObject root)
    {
        var graphics = root.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            string n = g.gameObject.name.ToLowerInvariant();
            if (n.Contains("ghost") || n.Contains("outline") || n.Contains("hover"))
            {
                g.raycastTarget = false;
                var cg = g.GetComponent<CanvasGroup>() ?? g.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                g.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
        }
    }
}
