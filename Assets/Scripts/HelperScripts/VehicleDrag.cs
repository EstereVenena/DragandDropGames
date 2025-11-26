using UnityEngine;
using UnityEngine.EventSystems;

public class VehicleDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    private RectTransform rt;
    private Canvas canvas;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    // Kad uzklikšķini uz mašīnas – padarām to par "aktīvo"
    public void OnPointerDown(PointerEventData eventData)
    {
        ObjectScript.lastDragged = gameObject;
        ObjectScript.drag = false; // vēl nesākam drag, tikai select
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ObjectScript.lastDragged = gameObject;
        ObjectScript.drag = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );

        rt.anchoredPosition = localPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ObjectScript.drag = false;
        // NE-nullējam lastDragged, lai pogas var turpināt transformēt pēdējo auto
        // ja gribi, var arī nullēt te – tad pogas strādās tikai drag laikā
        // ObjectScript.lastDragged = null;
    }
}
