// Assets/Scripts/UI/PenaltyCounterUI.cs
// Manages the crossed-car penalty icons and fires OnMaxPenalties when limit is reached.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PenaltyCounterUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform container;         // Where icons live (e.g., HorizontalLayoutGroup)
    public GameObject penaltyIconPrefab;    // Car-with-cross icon prefab
    public int maxPenalties = 3;

    [Header("Optional Feedback")]
    public AudioSource sfx;
    public AudioClip addPenaltySfx;

    [Header("Events")]
    public UnityEvent OnMaxPenalties;       // Fired once when reaching max

    private readonly List<GameObject> _icons = new();
    private int _count;
    private bool _firedMax;

    public int Count => _count;
    public bool IsAtMax => _count >= Mathf.Max(1, maxPenalties);

    void Awake()
    {
        if (!container) container = transform as RectTransform;
        maxPenalties = Mathf.Max(1, maxPenalties);
    }

    public void ResetPenalties()
    {
        _count = 0;
        _firedMax = false;

        for (int i = 0; i < _icons.Count; i++)
            if (_icons[i]) Destroy(_icons[i]);
        _icons.Clear();
    }

    public void AddPenalties(int n = 1)
    {
        for (int i = 0; i < n; i++) AddOne();
    }

    private void AddOne()
    {
        if (IsAtMax) return; // already capped

        _count++;

        if (sfx && addPenaltySfx)
            sfx.PlayOneShot(addPenaltySfx);

        if (penaltyIconPrefab && container)
        {
            var go = Instantiate(penaltyIconPrefab, container);
            var rt = go.GetComponent<RectTransform>();
            if (rt)
            {
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }
            var img = go.GetComponent<Image>();
            if (img) img.raycastTarget = false;

            _icons.Add(go);
        }

        if (IsAtMax && !_firedMax)
        {
            _firedMax = true;
            OnMaxPenalties?.Invoke();
        }
    }
}
