using UnityEngine;
using UnityEngine.SceneManagement;

public class ExtraLifeReward : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Cik sodus noņemt, kad spēlētājs noskatās rewarded reklāmu.")]
    public int penaltiesToRemove = 1;

    [Tooltip("Aizvērt GameEndPopup pēc extra life piešķiršanas.")]
    public bool closePopupOnReward = true;

    PenaltyCounterUI _penalty;
    GameEndPopup _popup;

    // --- LIMIT 1x PER LEVEL ---
    string _currentSceneName;
    bool _usedExtraLifeThisLevel;

    void Awake()
    {
        _currentSceneName = SceneManager.GetActiveScene().name;
        _usedExtraLifeThisLevel = false;
    }

    void OnEnable()
    {
        RewardedAds.OnRewardClaimed += HandleReward;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        RewardedAds.OnRewardClaimed -= HandleReward;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jauna levela sākums → drīkst atkal izmantot extra life
        _currentSceneName = scene.name;
        _usedExtraLifeThisLevel = false;
        Debug.Log($"[ExtraLifeReward] Scene loaded '{_currentSceneName}' → extra life reset.");
    }

    void HandleReward()
    {
        // Ja jau izmantots šajā levelā → ignorējam
        if (_usedExtraLifeThisLevel)
        {
            Debug.Log("[ExtraLifeReward] Extra life already used on this level – ignoring reward.");
            return;
        }

        _usedExtraLifeThisLevel = true;

        // Atrodam references, ja nav iedotas ar roku
        if (_penalty == null)
            _penalty = FindFirstObjectByType<PenaltyCounterUI>(FindObjectsInactive.Include);

        if (_popup == null)
            _popup = FindFirstObjectByType<GameEndPopup>(FindObjectsInactive.Include);

        // Noņemam sodus
        if (_penalty != null)
        {
            int n = Mathf.Max(1, penaltiesToRemove);
            _penalty.RemovePenalties(n);
            Debug.Log($"[ExtraLifeReward] Removed {n} penalties. New count = {_penalty.Count}");
        }
        else
        {
            Debug.LogWarning("[ExtraLifeReward] PenaltyCounterUI not found.");
        }

        // Aizveram Lose popup (lai var turpināt spēli)
        if (closePopupOnReward && _popup != null)
        {
            _popup.Hide();
            Debug.Log("[ExtraLifeReward] Closed GameEndPopup after extra life.");
        }
    }
}
