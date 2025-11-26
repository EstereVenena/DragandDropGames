using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class BannerAd : MonoBehaviour
{
    [SerializeField] string _androidAdUnitId = "Banner_Android";
    string _adUnitId;

    [SerializeField] Button _bannerButton;
    public bool isBannerVisible = false;

    [SerializeField] BannerPosition _bannerPosition = BannerPosition.BOTTOM_CENTER;

    private void Awake()
    {
        _adUnitId = _androidAdUnitId;
        Advertisement.Banner.SetPosition(_bannerPosition);
    }

    // ===================== LOAD =====================
    public void LoadBanner()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.Log("[BannerAd] Tried to load banner before Ads was initialized.");
            return;
        }

        Debug.Log("Loading Banner ad!");
        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback  = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        Advertisement.Banner.Load(_adUnitId, options);
    }

    void OnBannerLoaded()
    {
        Debug.Log("Banner ad loaded!");

        // FIX: ja nav poga – nekritam ārā
        if (_bannerButton != null)
            _bannerButton.interactable = true;
    }

    void OnBannerError(string message)
    {
        Debug.LogWarning("[BannerAd] Error: " + message);
        // pēc vajadzības var lēnām mēģināt pārlādēt
        // LoadBanner();
    }

    // ===================== BUTTON TOGGLE =====================
    public void ShowBannerAd()
    {
        if (isBannerVisible)
        {
            HideBannerAd();
        }
        else
        {
            BannerOptions options = new BannerOptions
            {
                clickCallback = OnBannerClicked,
                hideCallback  = OnBannerHidden,
                showCallback  = OnBannerShown
            };

            Advertisement.Banner.Show(_adUnitId, options);
        }
    }

    // ===================== PERSISTENT BANNER (HANOI) =====================
    public void ShowPersistent()
    {
        if (isBannerVisible)
            return;

        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback  = OnBannerHidden,
            showCallback  = OnBannerShown
        };

        Advertisement.Banner.Show(_adUnitId, options);
    }

    public void HideBannerAd()
    {
        Advertisement.Banner.Hide();
        isBannerVisible = false;
    }

    void OnBannerClicked()
    {
        Debug.Log("[BannerAd] Banner clicked.");
    }

    void OnBannerHidden()
    {
        Debug.Log("[BannerAd] Banner hidden.");
        isBannerVisible = false;
    }

    void OnBannerShown()
    {
        Debug.Log("[BannerAd] Banner shown.");
        isBannerVisible = true;
    }

    public void SetButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ShowBannerAd);
        _bannerButton = button;
        _bannerButton.interactable = false;
    }
}
