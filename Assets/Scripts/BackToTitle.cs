// Assets/Scripts/BackButtonScript.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class BackButtonScript : MonoBehaviour
{
    [Header("Scene to load (must be in Build Settings)")]
    public string sceneToLoad = "TitleScene";

    [Header("Optional SFX")]
    public AudioSource audioSource;
    public AudioClip clickSfx;

    bool _loading;

    void Awake()
    {
        // Make sure time isn't paused when this scene opens.
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        // Wire automatically if you forgot to do it in the Inspector.
        var btn = GetComponent<UnityEngine.UI.Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnBackClicked);
    }

    public void OnBackClicked()
    {
        if (_loading) return;
        _loading = true;

        // Play click SFX (optional)
        if (audioSource && clickSfx)
            audioSource.PlayOneShot(clickSfx);

        // Basic safety checks
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("[BackButton] 'sceneToLoad' not set.");
            _loading = false;
            return;
        }

        // Ensure scene is actually added to Build Settings (editor only hint)
#if UNITY_EDITOR
        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
            Debug.LogWarning($"[BackButton] Scene '{sceneToLoad}' not found in Build Settings.");
#endif

        SceneManager.LoadScene(sceneToLoad);
    }
}
