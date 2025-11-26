// Assets/Scripts/Game/RewardedContinueController.cs
// Klausās RewardedAds.OnRewardClaimed un dod "turpināt spēli" pēc reklāmas.

using UnityEngine;

[DisallowMultipleComponent]
public class RewardedContinueController : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("UI sodu skaitītājs (X X X ikonas).")]
    public PenaltyCounterUI penaltyCounter;

    [Tooltip("Lose/Win popup root (GameEndPopup panelis vai tā parents).")]
    public GameObject popupRoot;

    void Awake()
    {
        if (!penaltyCounter)
            penaltyCounter = FindFirstObjectByType<PenaltyCounterUI>(FindObjectsInactive.Exclude);

        if (!popupRoot)
        {
            var popup = FindFirstObjectByType<GameEndPopup>(FindObjectsInactive.Exclude);
            if (popup) popupRoot = popup.gameObject;
        }

        if (!penaltyCounter)
            Debug.LogWarning("[RewardedContinueController] PenaltyCounterUI not found in scene.");

        if (!popupRoot)
            Debug.LogWarning("[RewardedContinueController] popupRoot not assigned or found.");
    }

    void OnEnable()
    {
        RewardedAds.OnRewardClaimed += HandleReward;
    }

    void OnDisable()
    {
        RewardedAds.OnRewardClaimed -= HandleReward;
    }

    private void HandleReward()
    {
        Debug.Log("[RewardedContinueController] Reward received – resetting penalties and resuming game.");

        // 1) Notīrām X X X sodi
        if (penaltyCounter)
            penaltyCounter.ResetPenalties();

        // 2) Paslēpjam GameOver popup, lai ekrāns atkal ir tīrs
        if (popupRoot)
            popupRoot.SetActive(false);

        // 3) Drošībai atgriežam laika skalu uz 1
        Time.timeScale = 1f;
    }
}
