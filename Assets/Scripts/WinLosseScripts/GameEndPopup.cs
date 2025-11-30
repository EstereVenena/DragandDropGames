// Assets/Scripts/WinLosseScripts/GameEndPopup.cs
// Reusable Win/Lose popup with text OR sprite title, fade, pause, and scene buttons.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using TMPro;

[DisallowMultipleComponent]
public class GameEndPopup : MonoBehaviour
{
    public enum PopupType { Win, Lose }

    // Globāls flags, lai citi skripti var saprast, ka spēle ir pauzē
    public static bool IsGamePaused { get; private set; }

    // Cik reizes šajā spēles skrējienā izmantots continue (kopīgs visai spēlei)
    private static int continuesUsed = 0;

    [Header("Continue limits")]
    [Tooltip("Cik reizes atļauts turpināt spēli pēc reklāmas vienā game run.")]
    public int maxContinuesPerRun = 1;

    // ----------------------------- References -----------------------------
    [Header("References")]
    [Tooltip("Root object of the popup (panel). If null, uses this.gameObject.")]
    public GameObject root;

    [Tooltip("CanvasGroup used for fading. Will be added if missing.")]
    public CanvasGroup canvasGroup;

    [Tooltip("Optional TMP title (used if sprite title disabled or missing).")]
    public TMP_Text titleText;

    [Tooltip("Main message/body text.")]
    public TMP_Text messageText;

    [Tooltip("Secondary text line (tips/details).")]
    public TMP_Text extraInfoText;

    [Header("Title as Sprite (optional)")]
    [Tooltip("If true, uses a sprite for the big title (YOU WIN / GAME OVER).")]
    public bool useSpriteTitle = true;

    [Tooltip("Image component that displays the title sprite.")]
    public Image titleImage;

    [Tooltip("Sprite to use when type == Win.")]
    public Sprite winSprite;

    [Tooltip("Sprite to use when type == Lose.")]
    public Sprite loseSprite;

    [Tooltip("If true, calls SetNativeSize() on the title image when shown.")]
    public bool setNativeSizeForTitle = false;

    // ----------------------------- Buttons -----------------------------
    [Header("Buttons")]
    public Button retryButton;     // reload current scene
    public Button menuButton;      // go to Main Menu scene (if set)
    public Button nextButton;      // go to Next scene (if set)

    [Tooltip("Optional: Continue (rewarded ad) button")]
    public Button continueButton;

    // ----------------------------- Config -----------------------------
    [Header("Config")]
    [Min(0.01f)] public float fadeDuration = 0.25f;

    [Tooltip("If true, Time.timeScale is set to 0f when popup shows, and restored to 1f when hidden.")]
    public bool pauseOnShow = true;

    [Tooltip("Scene name for Main Menu. Leave empty to hide Menu button.")]
    public string mainMenuScene = "";

    [Tooltip("Scene name for Next level. Leave empty to hide Next button.")]
    public string nextScene = "";

    [Tooltip("Fallback title (if not using sprites).")]
    public string winTitle = "YOU WIN!";

    [Tooltip("Fallback title (if not using sprites).")]
    public string loseTitle = "GAME OVER";

    // ----------------------------- Optional SFX -----------------------------
    [Header("Optional SFX")]
    public AudioSource sfxSource;
    public AudioClip winClip;
    public AudioClip loseClip;

    // ----------------------------- Events -----------------------------
    [Header("Events")]
    public UnityEvent OnShown;
    public UnityEvent OnHidden;

    [Header("Rewarded Ad Events")]
    [Tooltip("Called after a rewarded ad is successfully watched and reward granted.")]
    public UnityEvent OnRewardedContinue;

    // ----------------------------- State -----------------------------
    bool isShowing;

    void Awake()
    {
        if (!root) root = gameObject;

        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        if (!canvasGroup)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Default start hidden
        root.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Button hooks
        if (retryButton)    retryButton.onClick.AddListener(OnRetry);
        if (menuButton)     menuButton.onClick.AddListener(OnMenu);
        if (nextButton)     nextButton.onClick.AddListener(OnNext);
        if (continueButton) continueButton.onClick.AddListener(OnContinuePressed);

        // Initial button visibility by config
        if (menuButton) menuButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(mainMenuScene));
        if (nextButton) nextButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(nextScene));
    }

    void OnEnable()
    {
        RewardedAds.OnRewardClaimed += HandleRewardClaimed;
    }

    void OnDisable()
    {
        RewardedAds.OnRewardClaimed -= HandleRewardClaimed;
    }

    // ----------------------------- Public API -----------------------------

    public void ShowLose(string message = "Try again.", string extra = "")
        => Show(PopupType.Lose, message, extra);

    public void ShowWin(string message = "Nice job!", string extra = "")
        => Show(PopupType.Win, message, extra);

    public void Show(PopupType type, string message, string extra)
    {
        if (isShowing) return;
        isShowing = true;

        // Title handling: sprite first, fallback to text
        bool canUseSprite = useSpriteTitle && titleImage &&
                            ((type == PopupType.Win && winSprite) || (type == PopupType.Lose && loseSprite));

        if (canUseSprite)
        {
            if (titleText) titleText.gameObject.SetActive(false);
            titleImage.gameObject.SetActive(true);
            titleImage.sprite = (type == PopupType.Win) ? winSprite : loseSprite;
            if (setNativeSizeForTitle) titleImage.SetNativeSize();
        }
        else
        {
            if (titleImage) titleImage.gameObject.SetActive(false);
            if (titleText)
            {
                titleText.gameObject.SetActive(true);
                titleText.text = (type == PopupType.Win) ? winTitle : loseTitle;
            }
        }

        if (messageText)   messageText.text   = message;
        if (extraInfoText) extraInfoText.text = extra;

        // Button visibility rules (tweak as you like)
        if (menuButton) menuButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(mainMenuScene));
        if (nextButton) nextButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(nextScene) && type == PopupType.Win);

        // Continue poga – tikai Lose popup + ja nav pārsniegts limits
        if (continueButton)
        {
            bool canContinue = (type == PopupType.Lose) && (continuesUsed < maxContinuesPerRun);
            continueButton.gameObject.SetActive(canContinue);
        }

        // SFX
        if (sfxSource)
        {
            var clip = (type == PopupType.Win) ? winClip : loseClip;
            if (clip) sfxSource.PlayOneShot(clip);
        }

        // Show & fade
        root.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(1f, null));

        if (pauseOnShow)
        {
            Time.timeScale = 0f;
            IsGamePaused = true;
        }

        OnShown?.Invoke();
    }

    public void Hide()
    {
        if (!isShowing) return;
        isShowing = false;

        StopAllCoroutines();
        StartCoroutine(FadeCanvas(0f, () =>
        {
            root.SetActive(false);
            OnHidden?.Invoke();
        }));

        if (pauseOnShow)
        {
            Time.timeScale = 1f;
            IsGamePaused = false;
        }
    }

    // Ērts helperis – ja vajag no GameManager reseto continue skaitu jaunam skrējienam
    public static void ResetContinues()
    {
        continuesUsed = 0;
    }

    // ----------------------------- Buttons -----------------------------

    void OnRetry()
{
    // Jauns skrējiens → nullējam continue skaitu
    continuesUsed = 0;

    if (pauseOnShow)
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
    }

    var scene = SceneManager.GetActiveScene();
    SceneManager.LoadScene(scene.buildIndex);
}


   void OnMenu()
{
    if (string.IsNullOrWhiteSpace(mainMenuScene)) return;

    continuesUsed = 0;

    if (pauseOnShow)
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
    }

    SceneManager.LoadScene(mainMenuScene);
}


    void OnNext()
    {
        if (string.IsNullOrWhiteSpace(nextScene)) return;

        if (pauseOnShow)
        {
            Time.timeScale = 1f;
            IsGamePaused = false;
        }

        SceneManager.LoadScene(nextScene);
    }

    /// <summary>
    /// "Turpināt spēlēt" poga – startē rewarded reklāmu.
    /// Spēle jau IR pauzē, tā nedrīkst atsākties šajā brīdī.
    /// </summary>
    public void OnContinuePressed()
    {
        if (AdManager.Instance != null && AdManager.Instance.rewardedAds != null)
        {
            AdManager.Instance.rewardedAds.ShowAd();
        }
        else
        {
            Debug.LogWarning("[GameEndPopup] AdManager / RewardedAds nav pieejams. Turpināšana bez reklāmas netiek atļauta.");
        }
    }

    /// <summary>
    /// Šo izsauc RewardedAds, ja reklāma noskatīta līdz galam (COMPLETED).
    /// </summary>
    void HandleRewardClaimed()
    {
        // Atzīmējam, ka continue izmantots
        continuesUsed++;

        // Atjaunojam spēli
        if (pauseOnShow)
        {
            Time.timeScale = 1f;
            IsGamePaused = false;
        }

        Hide();
        OnRewardedContinue?.Invoke();
    }

    // ----------------------------- Internals -----------------------------

    IEnumerator FadeCanvas(float targetAlpha, System.Action onDone)
    {
        float t = 0f;
        float start = canvasGroup.alpha;

        // Enable interaction only when visible
        if (targetAlpha > 0f)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        while (t < fadeDuration)
        {
            // Ja popup pauzē spēli, lietojam ne-scaloto laiku
            float dt = pauseOnShow ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float a = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / fadeDuration));
            canvasGroup.alpha = a;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        onDone?.Invoke();
    }
}
