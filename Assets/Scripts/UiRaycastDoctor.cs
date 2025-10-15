using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiRaycastDoctor : MonoBehaviour
{
    public Canvas canvas;
    GraphicRaycaster _gr; PointerEventData _ped; List<RaycastResult> _hits = new();

    void Awake()
    {
        if (!canvas) canvas = FindFirstObjectByType<Canvas>();
        _gr = canvas ? canvas.GetComponent<GraphicRaycaster>() : null;
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && _gr)
        {
            _hits.Clear();
            _ped = new PointerEventData(EventSystem.current){ position = Input.mousePosition };
            _gr.Raycast(_ped, _hits);
            Debug.Log(_hits.Count == 0 ? "[UI] Click hit NOTHING"
                                       : "[UI] Click hits (front→back): " + string.Join(", ", _hits.ConvertAll(h => h.gameObject.name)));
        }
    }
}
