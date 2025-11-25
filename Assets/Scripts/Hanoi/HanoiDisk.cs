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
    private HanoiTower startTower;   // tornis, no kura sākām vilkt

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // ja neesam piesaistīti tornim vai tornis tukšs – neko nedaram
        if (currentTower == null || currentTower.disks.Count == 0)
            return;

        // ļaujam vilkt tikai, ja šis disks ir savā tornī augšējais
        if (currentTower.disks.Peek() != this)
            return;

        startTower = currentTower;

        // izņemam sevi no torņa kaudzes, kamēr velkam
        currentTower.disks.Pop();

        // lai būtu virs citiem UI elementiem
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

        // savācam VISUS UI objektus zem kursora
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == gameObject)
                continue;

            var tower = r.gameObject.GetComponentInParent<HanoiTower>();
            if (tower != null && tower.CanPlaceDisk(this))
            {
                // veiksmīgi nometām uz derīga torņa
                tower.PlaceDisk(this);   // skaita gājienu + win check
                startTower = null;
                return;
            }
        }

        // ja neatradās derīgs tornis – atpakaļ sākuma tornī bez +move
        startTower.PlaceDisk(this, false);
        startTower = null;
    }
}
