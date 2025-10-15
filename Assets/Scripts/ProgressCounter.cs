using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections;
// If you use TMP, keep this using; harmless if package exists:
using TMPro;

public class ProgressCounter : MonoBehaviour
{
    public static ProgressCounter Instance { get; private set; }

    [Header("UI (assign one)")]
    [SerializeField] private Text uiText;              // Legacy UI.Text
    [SerializeField] private TMP_Text tmpText;         // TextMeshProUGUI

    [Header("When complete")]
    [SerializeField] private UnityEvent onAllMatched;

    [Header("Display")]
    [Tooltip("Show Remaining/Total (e.g., 12/12 Cars Left)")]
    [SerializeField] private bool showRemaining = true;
    [SerializeField] private string suffix = " Cars Left";

    [Header("Auto setup")]
    [Tooltip("Recount silhouettes next frame so spawners in Start have time to run.")]
    [SerializeField] private bool autoCountAtStart = true;

    private int totalSlots;
    private int correct;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Basic sanity logs (helps once, then you can mute)
        Debug.Log($"[ProgressCounter] Awake. UI bound? Text={(uiText? "yes":"no")}, TMP={(tmpText? "yes":"no")}");

        UpdateUI();
    }

    void Start()
    {
        if (autoCountAtStart) StartCoroutine(RecountNextFrame());
    }

    IEnumerator RecountNextFrame()
    {
        yield return null; // let spawners run
        CountAllSlotsInScene();
        ResetCount(totalSlots); // start as "total/total Cars Left"
        Debug.Log($"[ProgressCounter] Counted {totalSlots} silhouettes. Starting at {totalSlots}/{totalSlots}.");
    }

    void OnEnable()  { SilhouetteSlot.OnCorrectPlaced += HandleCorrectPlaced; }
    void OnDisable() { SilhouetteSlot.OnCorrectPlaced -= HandleCorrectPlaced; }

    void HandleCorrectPlaced(SilhouetteSlot slot)
    {
        if (slot == null || slot.Reported) return;
        slot.Reported = true;
        AddOne();
    }

    // ---- Public API ----
    public void AddOne()
    {
        correct++;
        UpdateUI();

        if (totalSlots > 0 && correct >= totalSlots)
        {
            Debug.Log("[ProgressCounter] All matched.");
            onAllMatched?.Invoke();
        }
    }

    public void SetTotal(int n)
    {
        totalSlots = Mathf.Max(0, n);
        UpdateUI();
    }

    public void SetTotalAndReset(int n)
    {
        totalSlots = Mathf.Max(0, n);
        correct = 0;
        UpdateUI();
    }

    public void CountAllSlotsInScene()
{
    // Prefer your actual drop targets:
    int dropSlots = FindObjectsOfType<DropPlaceScript>(includeInactive: true).Length;

    // Still support SilhouetteSlot if you ever use it:
    int silhouetteSlots = FindObjectsOfType<SilhouetteSlot>(includeInactive: true).Length;

    totalSlots = Mathf.Max(dropSlots, silhouetteSlots);
    Debug.Log($"[ProgressCounter] Counted slots -> DropPlace:{dropSlots}  Silhouette:{silhouetteSlots}  Using:{totalSlots}");
    UpdateUI();
}

    public void ResetCount(int newTotal = -1)
    {
        correct = 0;
        if (newTotal >= 0) totalSlots = newTotal;
        UpdateUI();
    }

    // ---- Legacy shims (keep older scripts compiling) ----
    public void CarPlaced()                { AddOne(); }
    public void CarPlaced(string slotId)   { AddOne(); }
    public void CarPlaced(bool isCorrect)  { if (isCorrect) AddOne(); }
    public void CarRemoved()               { correct = Mathf.Max(0, correct - 1); UpdateUI(); }

    public void RegisterCar()                          { totalSlots++; UpdateUI(); }
    public void RegisterCar(string id)                 { RegisterCar(); }
    public void RegisterCar(RectTransform car)         { RegisterCar(); }
    public void RegisterCar(GameObject car)            { RegisterCar(); }

    // ---- UI ----
    void UpdateUI()
    {
        int remaining = Mathf.Max(0, totalSlots - correct);
        string txt = showRemaining ? $"{remaining}/{totalSlots}{suffix}" : $"{correct}/{totalSlots}";

        if (uiText)   uiText.text = txt;
        if (tmpText) tmpText.text = txt;
    }
}
