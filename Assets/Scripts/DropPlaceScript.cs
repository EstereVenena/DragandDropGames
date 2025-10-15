// Assets/Scripts/DropPlaceScript.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DropPlaceScript : MonoBehaviour, IDropHandler
{
    [Header("Tolerances")]
    [Tooltip("Max allowed angle difference in degrees.")]
    public float rotationToleranceDeg = 10f;

    [Tooltip("Allowed relative size error per axis (0.10 = 10%).")]
    [Range(0f, 0.5f)] public float sizeTolerancePercent = 0.10f;

    static readonly Vector3[] _corners = new Vector3[4];

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (!dragged) return;

        // Require tag match (e.g., both “FireTruck”)
        if (!dragged.CompareTag(tag))
        {
            Debug.Log($"[DropPlace] Tag mismatch: dragged '{dragged.tag}' vs slot '{tag}'");
            return;
        }

        var carRT  = dragged.GetComponent<RectTransform>();
        var slotRT = GetComponent<RectTransform>();
        if (!carRT || !slotRT) return;

        // --- Rotation check ---
        float carZ  = carRT.eulerAngles.z;
        float slotZ = slotRT.eulerAngles.z;
        float rotDiff     = Mathf.Abs(Mathf.DeltaAngle(carZ, slotZ));
        bool  rotationOK  = rotDiff <= rotationToleranceDeg;

        // --- Size check (world space) ---
        Vector2 carSizeWS  = GetWorldSize(carRT);
        Vector2 slotSizeWS = GetWorldSize(slotRT);
        float wErr = Mathf.Abs(carSizeWS.x - slotSizeWS.x) / Mathf.Max(1f, slotSizeWS.x);
        float hErr = Mathf.Abs(carSizeWS.y - slotSizeWS.y) / Mathf.Max(1f, slotSizeWS.y);
        bool  sizeOK = (wErr <= sizeTolerancePercent) && (hErr <= sizeTolerancePercent);

        if (!rotationOK || !sizeOK)
        {
            Debug.Log($"[DropPlace] Failed: rotOK={rotationOK}({rotDiff:F1}°) sizeOK={sizeOK} (wErr={wErr:P0} hErr={hErr:P0})");
            return;
        }

        // --- Snap center-to-center in the car's parent space ---
        Vector3 worldCenter = slotRT.TransformPoint(slotRT.rect.center);
        Vector3 localCenter = carRT.parent.InverseTransformPoint(worldCenter);
        carRT.anchoredPosition = new Vector2(localCenter.x, localCenter.y);

        // --- Match rotation ---
        carRT.localRotation = Quaternion.Euler(0, 0, slotRT.localEulerAngles.z);

        // --- Match visual size by scaling (keeps current parent) ---
        Vector2 newCarSizeWS = GetWorldSize(carRT); // current world size after rotation
        float kx = newCarSizeWS.x > 0.001f ? slotSizeWS.x / newCarSizeWS.x : 1f;
        float ky = newCarSizeWS.y > 0.001f ? slotSizeWS.y / newCarSizeWS.y : 1f;
        var s = carRT.localScale;
        carRT.localScale = new Vector3(s.x * kx, s.y * ky, s.z);

        // (Optional) If you prefer parenting under the slot, uncomment:
        // carRT.SetParent(slotRT, worldPositionStays:false);
        // carRT.anchoredPosition = Vector2.zero;

        // --- Lock dragging & raycasts so it can’t be moved again ---
        var drag = dragged.GetComponent<DragAndDropScript>();
        if (drag) drag.Lock();
        var img = dragged.GetComponent<Image>();
        if (img) img.raycastTarget = false;

        // --- Count progress (✅ THIS is the fix) ---
        // Use the bool overload to increment only on correct placement.
        ProgressCounter.Instance?.CarPlaced(true);

        Debug.Log($"[DropPlace] Snapped & locked: {dragged.name}");
    }

    static Vector2 GetWorldSize(RectTransform rt)
    {
        rt.GetWorldCorners(_corners); // 0 = BL, 2 = TR
        Vector3 bl = _corners[0], tr = _corners[2];
        return new Vector2(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y));
    }
}
