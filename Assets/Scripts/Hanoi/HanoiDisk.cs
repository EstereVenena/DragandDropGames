using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HanoiDisk : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Mazākais disks = 1, lielākais = 3/4/5")]
    public int size = 1;

    [Tooltip("Kurā tornī šis disks pašlaik atrodas")]
    public HanoiTower currentTower;

    private Canvas parentCanvas;
    private HanoiTower startTower;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentTower == null || currentTower.disks.Count == 0)
            return;

        if (currentTower.disks.Peek() != this)
            return;

        startTower = currentTower;
        currentTower.disks.Pop();

        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (startTower == null)
            return;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var worldPos
        );

        transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (startTower == null)
            return;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == gameObject)
                continue;

            var tower = r.gameObject.GetComponentInParent<HanoiTower>();
            if (tower != null && tower.CanPlaceDisk(this))
            {
                tower.PlaceDisk(this);
                startTower = null;
                return;
            }
        }

        startTower.PlaceDisk(this, false);
        startTower = null;
    }
}
