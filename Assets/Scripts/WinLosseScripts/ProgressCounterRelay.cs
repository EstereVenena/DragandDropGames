// Assets/Scripts/Game/ProgressCounterRelay.cs
// Add this to the same GameObject as ProgressCounter. Drag LoseWinController.OnWin into the UnityEvent.

using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ProgressCounterRelay : MonoBehaviour
{
    public UnityEvent OnAllMatchedRelay;

    ProgressCounter _pc;

    void Awake()
    {
        _pc = GetComponent<ProgressCounter>();
        if (!_pc) Debug.LogWarning("[ProgressCounterRelay] ProgressCounter not found on this GameObject.");
    }

    // Call this from ProgressCounter when all matched, if you decide to expose a public hook.
    // Or, if you keep ProgressCounter private, you can invoke OnAllMatchedRelay from wherever you detect win.
    public void Fire()
    {
        OnAllMatchedRelay?.Invoke();
    }
}
