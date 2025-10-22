// Assets/Scripts/Game/LoseWinController.cs
// Listens for lose (max penalties) and win (ProgressCounter.onAllMatched) to show GameEndPopup.

using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class LoseWinController : MonoBehaviour
{
    [Header("Refs")]
    public PenaltyCounterUI penaltyCounter;   // auto-found if null
    public GameEndPopup popup;               // auto-found if null
    public ProgressCounter progressCounter;  // auto-found if null

    [Header("Texts")]
    [TextArea] public string loseMessage = "Too many mistakes!";
    [TextArea] public string loseExtraInfo = "Tip: avoid dragging cars across bombs.";
    [TextArea] public string winMessage = "All cars placed!";
    [TextArea] public string winExtraInfo = "Great work, driver.";

    // We’ll cache the private UnityEvent from ProgressCounter via reflection
    UnityEvent _onAllMatched;

    void Awake()
    {
        if (!penaltyCounter)
            penaltyCounter = FindFirstObjectByType<PenaltyCounterUI>(FindObjectsInactive.Exclude);
        if (!popup)
            popup = FindFirstObjectByType<GameEndPopup>(FindObjectsInactive.Exclude);
        if (!progressCounter)
            progressCounter = FindFirstObjectByType<ProgressCounter>(FindObjectsInactive.Exclude);

        if (!penaltyCounter) Debug.LogWarning("[LoseWinController] PenaltyCounterUI not found.");
        if (!popup)          Debug.LogWarning("[LoseWinController] GameEndPopup not found.");
        if (!progressCounter) Debug.LogWarning("[LoseWinController] ProgressCounter not found.");

        // Reflect the private UnityEvent on ProgressCounter: "onAllMatched"
        if (progressCounter)
        {
            var fi = typeof(ProgressCounter).GetField("onAllMatched",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fi != null)
            {
                _onAllMatched = fi.GetValue(progressCounter) as UnityEvent;
            }
            else
            {
                Debug.LogWarning("[LoseWinController] Could not reflect ProgressCounter.onAllMatched.");
            }
        }
    }

    void OnEnable()
    {
        if (penaltyCounter) penaltyCounter.OnMaxPenalties.AddListener(OnLose);
        if (_onAllMatched != null) _onAllMatched.AddListener(OnWin);
    }

    void OnDisable()
    {
        if (penaltyCounter) penaltyCounter.OnMaxPenalties.RemoveListener(OnLose);
        if (_onAllMatched != null) _onAllMatched.RemoveListener(OnWin);
    }

    public void OnLose()
    {
        if (!popup) return;
        popup.Show(GameEndPopup.PopupType.Lose, loseMessage, loseExtraInfo);
    }

    public void OnWin()
    {
        if (!popup) return;
        popup.Show(GameEndPopup.PopupType.Win, winMessage, winExtraInfo);
    }
}
