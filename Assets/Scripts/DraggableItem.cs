using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string carId; // e.g., "sedan", "truck"
    public float snapDistanceFactor = 0.35f; // threshold vs slot size (0..1)

    RectTransform rt;
    Canvas canvas;
    CanvasGroup cg;
    Transform originalParent;
    Vector2 originalAnchored;
    Quaternion originalRot;
    Vector3 originalScale;

    void Awake(){
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData e){
        originalParent = rt.parent;
        originalAnchored = rt.anchoredPosition;
        originalRot = rt.localRotation;
        originalScale = rt.localScale;

        // Bring to top to avoid being hidden under UI
        rt.SetAsLastSibling();

        // Allow pointer to pass through while dragging
        cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData e){
        // UI should move in anchored space
        rt.anchoredPosition += e.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData e){
        // If we released over a slot, IDropHandler handles the parenting.
        // But in case you use only end-drag hit test, we’ll raycast:
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(e, results);

        SilhouetteSlot best = null;
        float bestScore = float.MaxValue;

        foreach(var r in results){
            var slot = r.gameObject.GetComponentInParent<SilhouetteSlot>();
            if(slot == null) continue;
            if(!slot.Accepts(carId)) continue;

            // Distance score vs slot rect
            var slotRt = (RectTransform)slot.transform;
            var thisCenter = WorldCenter(rt);
            var slotCenter = WorldCenter(slotRt);
            float pxDist = Vector2.Distance(thisCenter, slotCenter);

            // Normalize by slot size to get scale-independent threshold
            var slotSize = slotRt.rect.size * slotRt.lossyScale;
            float norm = pxDist / (0.5f * (slotSize.x + slotSize.y) * 0.5f);

            if(norm < bestScore){ bestScore = norm; best = slot; }
        }

        if(best != null && bestScore <= snapDistanceFactor){
            best.SnapHere(rt);
        } else {
            // return to origin
            rt.SetParent(originalParent, worldPositionStays:false);
            rt.anchoredPosition = originalAnchored;
            rt.localRotation = originalRot;
            rt.localScale = originalScale;
        }

        // Restore raycasts so future drops work
        cg.blocksRaycasts = true;
    }

    Vector2 WorldCenter(RectTransform r){
        Vector3[] corners = new Vector3[4];
        r.GetWorldCorners(corners);
        return (Vector2)((corners[0] + corners[2]) * 0.5f);
    }
}
