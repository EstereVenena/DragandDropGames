using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Ad Unit IDs")]
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    // Ja gribēsi iOS, pieliksi te arī iOS id
    string _adUnitId;

    [Header("UI")]
    [SerializeField] Button _rewardedAdButton;
    [SerializeField] ObstaclesSpawnScript obstaclesSpawner;

    public static event System.Action OnRewardClaimed;

    private void Awake()
    {
        // Šobrīd tikai Android
        _adUnitId = _androidAdUnitId;

        if (!_rewardedAdButton)
            Debug.LogWarning("[RewardedAds] Button not assigned.");

        if (!obstaclesSpawner)
            obstaclesSpawner = FindFirstObjectByType<ObstaclesSpawnScript>();

        // Ja poga jau ir pieseta Inspectorā – uzreiz piesienam click
        if (_rewardedAdButton)
        {
            SetButton(_rewardedAdButton);
        }
    }

    private void Start()
    {
        // mēģinam ielādēt pirmo reklāmu startā
        LoadAd();
    }

    // -------------------- Load --------------------
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
        if (!placementId.Equals(_adUnitId))
            return;

        Debug.Log("[RewardedAds] Ad loaded!");

        if (_rewardedAdButton)
            _rewardedAdButton.interactable = true;
        else
            Debug.LogWarning("[RewardedAds] Ad loaded but button is null.");
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"[RewardedAds] Failed to load ({error}): {message}");
        // pēc nelielas pauzes mēģinam vēlreiz
        StartCoroutine(WaitAndLoad(5f));
    }

    IEnumerator WaitAndLoad(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadAd();
    }

    // -------------------- Show --------------------
    public void ShowAd()
    {
        if (!_rewardedAdButton)
            Debug.LogWarning("[RewardedAds] ShowAd called but button is null.");

        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[RewardedAds] Tried to show ad but Ads not initialized.");
            return;
        }

        if (_rewardedAdButton)
            _rewardedAdButton.interactable = false;

        Debug.Log("[RewardedAds] Showing rewarded ad...");
        Advertisement.Show(_adUnitId, this);
    }

    public void SetButton(Button button)
    {
        if (!button) return;

        // Nodzēšam vecos listenerus, ja bija
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ShowAd);

        _rewardedAdButton = button;
        _rewardedAdButton.interactable = false; // līdz brīdim, kad ad ielādēsies
    }

    // -------------------- Show callbacks --------------------
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"[RewardedAds] Failed to show ({error}): {message}");
        Time.timeScale = 1f; // drošībai atjaunojam, ja kas izgāzās
        StartCoroutine(WaitAndLoad(5f));
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log("[RewardedAds] Ad show started. Pausing game time.");
        Time.timeScale = 0f;
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log("[RewardedAds] Ad clicked.");
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"[RewardedAds] Ad completed with state: {showCompletionState}");

        // Atjaunojam laiku vienalga, kā beidzās
        Time.timeScale = 1f;

        // Reward dodam TIKAI, ja tiešām noskatīts līdz galam
        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("[RewardedAds] Reward granted.");

            // Notify event listeners
            OnRewardClaimed?.Invoke();

            // Notīram visus lidojošos objektus
            if (obstaclesSpawner)
                obstaclesSpawner.ClearAllSpawned();
            else
                Debug.LogWarning("[RewardedAds] No ObstaclesSpawnScript assigned to clear obstacles.");

            if (_rewardedAdButton)
                _rewardedAdButton.interactable = false;

            // Pēc laika ielādējam nākamo reklāmu
            StartCoroutine(WaitAndLoad(10f));
        }
        else
        {
            Debug.Log("[RewardedAds] Ad not fully watched – no reward, just reload ad.");
            StartCoroutine(WaitAndLoad(5f));
            if (_rewardedAdButton)
                _rewardedAdButton.interactable = false;
        }
    }
}
