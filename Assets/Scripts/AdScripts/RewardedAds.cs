using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    string _adUnitId;

    [SerializeField] Button _rewardedAdButton;
    [SerializeField] ObstaclesSpawnScript obstaclesSpawner;

    public static event System.Action OnRewardClaimed;

    private void Awake()
    {
        _adUnitId = _androidAdUnitId;

        if (!_rewardedAdButton)
            Debug.LogWarning("[RewardedAds] Button not assigned.");

        if (!obstaclesSpawner)
            obstaclesSpawner = FindFirstObjectByType<ObstaclesSpawnScript>();
    }

    public void LoadAd()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[RewardedAds] Ads not initialized yet.");
            return;
        }

        Debug.Log("[RewardedAds] Loading ad...");
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId.Equals(_adUnitId))
        {
            Debug.Log("[RewardedAds] Ad loaded!");
            _rewardedAdButton.interactable = true;
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"[RewardedAds] Failed to load: {message}");
        StartCoroutine(WaitAndLoad(5f));
    }

    IEnumerator WaitAndLoad(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadAd();
    }

    public void ShowAd()
    {
        _rewardedAdButton.interactable = false;
        Advertisement.Show(_adUnitId, this);
    }

    public void SetButton(Button button)
    {
        if (!button) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ShowAd);
        _rewardedAdButton = button;
        _rewardedAdButton.interactable = false;
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"[RewardedAds] Failed to show: {message}");
        StartCoroutine(WaitAndLoad(5f));
    }

    public void OnUnityAdsShowStart(string placementId) => Time.timeScale = 0f;

    public void OnUnityAdsShowClick(string placementId)
        => Debug.Log("[RewardedAds] Ad clicked.");

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log("[RewardedAds] Ad completed!");

        // Notify event listeners
        OnRewardClaimed?.Invoke();

        // Destroy all flying objects
        obstaclesSpawner?.DestroyAllSpawnedObjects();

        _rewardedAdButton.interactable = false;
        StartCoroutine(WaitAndLoad(10f));
        Time.timeScale = 1f;
    }
}
