// DragStateHooks.cs
// Tiny adapter that sets DragState for any UI element you drag.

using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class DragStateHooks : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    RectTransform rt;

    void Awake() => rt = transform as RectTransform;

    public void OnBeginDrag(PointerEventData eventData)
    {
        DragState.Begin(rt);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DragState.End();
    }
}
