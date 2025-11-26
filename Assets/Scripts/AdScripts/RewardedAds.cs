using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Ad Unit IDs")]
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    string _adUnitId;

    [Header("UI")]
    [SerializeField] Button _rewardedAdButton;
    [SerializeField] ObstaclesSpawnScript obstaclesSpawner;

    public static event System.Action OnRewardClaimed;

    private bool _isLoaded = false;

    private void Awake()
    {
        _adUnitId = _androidAdUnitId;

        if (!obstaclesSpawner)
            obstaclesSpawner = FindFirstObjectByType<ObstaclesSpawnScript>();

        // Ja inspektorā jau ielikts button refs – sasienam
        if (_rewardedAdButton)
            SetButton(_rewardedAdButton);
    }

    private void Start()
    {
        // Pirmo load darīsim tikai, ja Ads jau inicializēts
        if (Advertisement.isInitialized)
            LoadAd();
        else
            Debug.Log("[RewardedAds] Waiting for AdsInitializer to finish...");
    }

    // ================== LOAD ==================
    public void LoadAd()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[RewardedAds] Ads not initialized yet, cannot load.");
            return;
        }

        Debug.Log("[RewardedAds] Loading rewarded ad...");
        _isLoaded = false;
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (!placementId.Equals(_adUnitId)) return;

        Debug.Log("[RewardedAds] Ad loaded OK.");
        _isLoaded = true;

        if (_rewardedAdButton)
        {
            _rewardedAdButton.interactable = true;
            Debug.Log("[RewardedAds] Button enabled for click.");
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"[RewardedAds] Failed to load ({error}): {message}");
        StartCoroutine(WaitAndLoad(5f));
    }

    IEnumerator WaitAndLoad(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadAd();
    }

    // ================== BUTTON BIND ==================
    public void SetButton(Button button)
    {
        if (!button)
        {
            Debug.LogWarning("[RewardedAds] SetButton called with null Button.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ShowAd);

        _rewardedAdButton = button;

        // TESTA REŽĪMS: ļaujam klikšķināt pat ja nav loaded, lai redzētu logus
        _rewardedAdButton.interactable = true;

        Debug.Log("[RewardedAds] Button bound to ShowAd().");
    }

    // ================== SHOW ==================
    public void ShowAd()
    {
        Debug.Log("[RewardedAds] ShowAd() called.");

        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[RewardedAds] Cannot show – Ads not initialized.");
            return;
        }

        if (!_isLoaded)
        {
            Debug.LogWarning("[RewardedAds] Ad not loaded yet, loading again.");
            LoadAd();
            return;
        }

        if (_rewardedAdButton)
            _rewardedAdButton.interactable = false;

        Debug.Log("[RewardedAds] Showing rewarded ad...");
        Advertisement.Show(_adUnitId, this);
    }

    // ================== SHOW CALLBACKS ==================
    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log("[RewardedAds] Ad show started. Pausing game time.");
        Time.timeScale = 0f;
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log("[RewardedAds] Ad clicked.");
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"[RewardedAds] Failed to show ({error}): {message}");
        Time.timeScale = 1f;
        StartCoroutine(WaitAndLoad(5f));
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"[RewardedAds] Ad completed with state: {showCompletionState}");
        Time.timeScale = 1f;

        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("[RewardedAds] Reward granted.");

            OnRewardClaimed?.Invoke();

            if (obstaclesSpawner)
                obstaclesSpawner.ClearAllSpawned(); // vai tava metode
            else
                Debug.LogWarning("[RewardedAds] No ObstaclesSpawnScript assigned.");

            if (_rewardedAdButton)
                _rewardedAdButton.interactable = false;

            StartCoroutine(WaitAndLoad(10f));
        }
        else
        {
            Debug.Log("[RewardedAds] Not fully watched, no reward.");
            if (_rewardedAdButton)
                _rewardedAdButton.interactable = false;

            StartCoroutine(WaitAndLoad(5f));
        }
    }
}
