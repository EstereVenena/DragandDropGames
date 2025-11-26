using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Advertisements.Advertisement;

public class AdManager : MonoBehaviour
{
    [Header("Refs")]
    public AdsInitializer adsInitializer;
    public InterstitialAd interstitialAd;
    public RewardedAds rewardedAds;
    public BannerAd bannerAd;

    [Header("Toggles")]
    [SerializeField] bool turnOffInterstitialAd = false;
    [SerializeField] bool turnOffRewardedAds = false;
    [SerializeField] bool turnOffBannerAd = false;

    [Header("Scene Settings")]
    [Tooltip("Scēnas nosaukums, kurā gribi redzēt banner (Hanojas spēle).")]
    public string hanoiSceneName = "HanoiScene"; // <- nomaini uz savu scēnas nosaukumu

    public static AdManager Instance { get; private set; }

    private bool _adsInitialized = false;
    private bool _firstSceneLoad = true;

    private void Awake()
    {
        if (adsInitializer == null)
            adsInitializer = FindFirstObjectByType<AdsInitializer>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (adsInitializer != null)
        {
            adsInitializer.OnAdsInitialized += HandleAdsInitialized;
        }
        else
        {
            Debug.LogWarning("[AdManager] AdsInitializer not found.");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (adsInitializer != null)
            adsInitializer.OnAdsInitialized -= HandleAdsInitialized;
    }

    // ===================== INIT =====================
    private void HandleAdsInitialized()
    {
        _adsInitialized = true;
        Debug.Log("[AdManager] Ads initialized → loading ad units.");

        // Atrodam references, ja tās nav uzliktas ar roku
        if (interstitialAd == null)
            interstitialAd = FindFirstObjectByType<InterstitialAd>();

        if (rewardedAds == null)
            rewardedAds = FindFirstObjectByType<RewardedAds>();

        if (bannerAd == null)
            bannerAd = FindFirstObjectByType<BannerAd>();

        if (!turnOffInterstitialAd && interstitialAd != null)
        {
            interstitialAd.OnInterstitialAdReady += HandleInterstitialReady;
            interstitialAd.LoadAd();
        }

        if (!turnOffRewardedAds && rewardedAds != null)
        {
            rewardedAds.LoadAd();
        }

        if (!turnOffBannerAd && bannerAd != null)
        {
            bannerAd.LoadBanner();
        }
    }

    private void HandleInterstitialReady()
    {
        Debug.Log("[AdManager] Interstitial ad is ready.");
        // Pirmo scēnu atstājam bez automātiskas reklāmas,
        // pārējās rādīsim OnSceneLoaded pēc pirmās scēnas.
    }

    // ===================== SCENE LOADED =====================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AdManager] Scene loaded: {scene.name}");

        // Atrodam references scēnā (Buttons) un piesienam
        RebindSceneButtons();

        // Banner scēnas specifiskā loģika
        HandleBannerForScene(scene);

        // Interstitial pirms katras JAUNAS scēnas (izņemot pirmo starta ielādi)
        if (!_adsInitialized || turnOffInterstitialAd || interstitialAd == null)
        {
            if (!_adsInitialized)
                Debug.Log("[AdManager] Ads not initialized yet, skipping interstitial this time.");
            return;
        }

        if (_firstSceneLoad)
        {
            _firstSceneLoad = false;
            Debug.Log("[AdManager] First scene load – skipping interstitial.");
            return;
        }

        // Katru nākamo scēnu – ja gatava, rādam interstitial
        if (interstitialAd.isReady)
        {
            Debug.Log("[AdManager] Showing interstitial at scene load.");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("[AdManager] Interstitial not ready at scene load, loading again.");
            interstitialAd.LoadAd();
        }
    }

    // ===================== HELPERS =====================
    private void RebindSceneButtons()
    {
        // Interstitial Button
        if (interstitialAd == null)
            interstitialAd = FindFirstObjectByType<InterstitialAd>();

        if (interstitialAd != null)
        {
            GameObject go = null;
            try { go = GameObject.FindGameObjectWithTag("InterstitialButton"); }
            catch { }

            if (go != null)
            {
                var btn = go.GetComponent<Button>();
                if (btn != null)
                    interstitialAd.SetButton(btn);
            }
        }

        // Rewarded Button
        if (rewardedAds == null)
            rewardedAds = FindFirstObjectByType<RewardedAds>();

        if (rewardedAds != null)
        {
            GameObject go = null;
            try { go = GameObject.FindGameObjectWithTag("RewardedButton"); }
            catch { }

            if (go != null)
            {
                var btn = go.GetComponent<Button>();
                if (btn != null)
                    rewardedAds.SetButton(btn);
            }
        }

        // Banner Button (ja joprojām gribi manuālu toggle kaut kur)
        if (bannerAd == null)
            bannerAd = FindFirstObjectByType<BannerAd>();

        if (bannerAd != null)
        {
            GameObject go = null;
            try { go = GameObject.FindGameObjectWithTag("BannerButton"); }
            catch { }

            if (go != null)
            {
                var btn = go.GetComponent<Button>();
                if (btn != null)
                    bannerAd.SetButton(btn);
            }
        }
    }

    private void HandleBannerForScene(Scene scene)
    {
        if (turnOffBannerAd || bannerAd == null)
            return;

        bool isHanoi = !string.IsNullOrEmpty(hanoiSceneName) &&
                       scene.name == hanoiSceneName;

        if (isHanoi)
        {
            Debug.Log("[AdManager] Hanoi scene detected → showing banner.");
            bannerAd.LoadBanner();
            bannerAd.ShowPersistent();
        }
        else
        {
            Debug.Log("[AdManager] Non-Hanoi scene → hiding banner.");
            bannerAd.HideBannerAd();
        }
    }
}