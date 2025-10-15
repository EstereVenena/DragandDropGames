using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SilhouetteSlot : MonoBehaviour, IDropHandler
{
    public string slotId;

    // Fired when a *new* correct item is snapped here
    public static event Action<SilhouetteSlot> OnCorrectPlaced;

    bool isFilled;          // snapshot to avoid double counting
    public bool Reported;   // used by the counter to avoid recounting

    public bool Accepts(string carId)
    {
        return string.Equals(carId, slotId, StringComparison.OrdinalIgnoreCase);
    }

    public void OnDrop(PointerEventData e)
    {
        var drag = e.pointerDrag ? e.pointerDrag.GetComponent<DraggableItem>() : null;
        if (drag == null || !Accepts(drag.carId)) return;

        SnapHere(drag.GetComponent<RectTransform>());
    }

    public void SnapHere(RectTransform item)
    {
        var slotRt = (RectTransform)transform;

        item.SetParent(slotRt, worldPositionStays: false);
        item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.anchoredPosition = Vector2.zero;
        item.localRotation = Quaternion.identity;
        item.localScale = Vector3.one;

        var cg = item.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.interactable = false; // lock once correct
        }

        if (!isFilled)
        {
            isFilled = true;
            OnCorrectPlaced?.Invoke(this); // <- tell the world
        }
    }
}
