// Assets/Scripts/ProgressCounter.cs
//
// Simple progress HUD that shows Remaining/Total cars.
// Counts DropPlaceScript instances (your actual drop targets).
// No SilhouetteSlot dependency.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ProgressCounter : MonoBehaviour
{
    public static ProgressCounter Instance { get; private set; }

    [Header("UI (assign one, or we'll try to find it)")]
    [SerializeField] private Text uiText;          // Legacy UI.Text (optional)
    [SerializeField] private TMP_Text tmpText;     // TextMeshProUGUI (optional)
    [Tooltip("If left empty, we'll try tag/name 'ProgressText'.")]
    [SerializeField] private string progressTextTagOrName = "ProgressText";

    [Header("When complete")]
    [SerializeField] private UnityEvent onAllMatched;

    [Header("Display")]
    [Tooltip("Show Remaining/Total (e.g., 12/12 Cars Left)")]
    [SerializeField] private bool showRemaining = true;
    [SerializeField] private string suffix = " Cars Left";

    [Header("Auto setup")]
    [Tooltip("Recount next frame so spawners in Start() have time to run.")]
    [SerializeField] private bool autoCountAtStart = true;

    [Header("Debug logs")]
    [SerializeField] private bool verboseLogs = true;

    int totalSlots;
    int correct;

    void Awake()
    {
        // simple singleton (destroy extra component, not GO)
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        TryBindUIText();
        UpdateUI();

        if (verboseLogs)
            Debug.Log($"[ProgressCounter] Awake. Bound Text={(uiText ? "yes" : "no")} TMP={(tmpText ? "yes" : "no")}");
    }

    void Start()
    {
        if (autoCountAtStart) StartCoroutine(RecountNextFrame());
    }

    IEnumerator RecountNextFrame()
    {
        yield return null; // wait a frame for spawners
        CountAllSlotsInScene();
        ResetCount(totalSlots);
        if (verboseLogs) Debug.Log($"[ProgressCounter] Counted {totalSlots} slots. Starting at {totalSlots}/{totalSlots}.");
    }

    // ------------ Public API ------------
    public void AddOne()
    {
        correct = Mathf.Min(correct + 1, totalSlots);
        UpdateUI();

        if (totalSlots > 0 && correct >= totalSlots)
        {
            if (verboseLogs) Debug.Log("[ProgressCounter] All matched.");
            onAllMatched?.Invoke();
        }
    }

    public void SetTotal(int n)         { totalSlots = Mathf.Max(0, n); UpdateUI(); }
    public void SetTotalAndReset(int n) { totalSlots = Mathf.Max(0, n); correct = 0; UpdateUI(); }

    public void CountAllSlotsInScene()
    {
        // Modern API (no obsolete warning):
        // Include inactive objects; no sorting for speed.
        var slots = Object.FindObjectsByType<DropPlaceScript>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

        totalSlots = slots?.Length ?? 0;

        if (verboseLogs)
            Debug.Log($"[ProgressCounter] Slots -> DropPlace:{totalSlots}  Using:{totalSlots}");

        UpdateUI();
    }

    public void ResetCount(int newTotal = -1)
    {
        correct = 0;
        if (newTotal >= 0) totalSlots = newTotal;
        UpdateUI();
    }

    // ---- Legacy shims (keep old calls compiling; harmless) ----
    public void CarPlaced()                => AddOne();
    public void CarPlaced(string _)        => AddOne();
    public void CarPlaced(bool isCorrect)  { if (isCorrect) AddOne(); }
    public void CarRemoved()               { correct = Mathf.Max(0, correct - 1); UpdateUI(); }
    public void RegisterCar()              { totalSlots++; UpdateUI(); }
    public void RegisterCar(string _)      => RegisterCar();
    public void RegisterCar(RectTransform _) => RegisterCar();
    public void RegisterCar(GameObject _)    => RegisterCar();

    // ------------ UI helpers ------------
    void UpdateUI()
    {
        int remaining = Mathf.Max(0, totalSlots - correct);
        string txt = showRemaining ? $"{remaining}/{totalSlots}{suffix}" : $"{correct}/{totalSlots}";

        if (uiText)  uiText.text = txt;
        if (tmpText) tmpText.text = txt;
    }

    void TryBindUIText()
    {
        if (uiText || tmpText) return;

        // Try by tag first
        if (!string.IsNullOrWhiteSpace(progressTextTagOrName))
        {
            try
            {
                var go = GameObject.FindGameObjectWithTag(progressTextTagOrName);
                if (go)
                {
                    tmpText = go.GetComponent<TMP_Text>();
                    if (!tmpText) uiText = go.GetComponent<Text>();
                    if (tmpText || uiText) return;
                }
            }
            catch { /* tag not defined; ignore */ }
        }

        // Try by name
        var named = GameObject.Find(progressTextTagOrName);
        if (named)
        {
            tmpText = named.GetComponent<TMP_Text>();
            if (!tmpText) uiText = named.GetComponent<Text>();
        }
    }
}
