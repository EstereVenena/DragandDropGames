// Assets/Scripts/UI/GameEndPopup.cs
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

    // ----------------------------- Config -----------------------------
    [Header("Config")]
    [Min(0.01f)] public float fadeDuration = 0.25f;
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
        if (retryButton) retryButton.onClick.AddListener(OnRetry);
        if (menuButton)  menuButton.onClick.AddListener(OnMenu);
        if (nextButton)  nextButton.onClick.AddListener(OnNext);

        // Initial button visibility by config
        if (menuButton) menuButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(mainMenuScene));
        if (nextButton) nextButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(nextScene));
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

        if (pauseOnShow) Time.timeScale = 0f;

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

        if (pauseOnShow) Time.timeScale = 1f;
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
            t += (pauseOnShow ? Time.unscaledDeltaTime : Time.deltaTime);
            float a = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / fadeDuration));
            canvasGroup.alpha = a;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        // Disable interaction if fully hidden
        if (Mathf.Approximately(targetAlpha, 0f))
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        onDone?.Invoke();
    }

    // ----------------------------- Button Handlers -----------------------------

    public void OnRetry()
    {
        if (pauseOnShow) Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }

    public void OnMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuScene)) return;
        if (pauseOnShow) Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
    }

    public void OnNext()
    {
        if (string.IsNullOrWhiteSpace(nextScene)) return;
        if (pauseOnShow) Time.timeScale = 1f;
        SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
    }
}
