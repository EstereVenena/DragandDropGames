using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam  = "MusicVol";
    [SerializeField] private string sfxParam    = "SFXVol";

    [Header("Defaults (0..1)")]
    [Range(0f,1f)] public float defaultMaster = 0.8f;
    [Range(0f,1f)] public float defaultMusic  = 0.8f;
    [Range(0f,1f)] public float defaultSFX    = 0.8f;

    const string KEY_MASTER = "vol_master";
    const string KEY_MUSIC  = "vol_music";
    const string KEY_SFX    = "vol_sfx";

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Load saved values (0..1)
        float m = PlayerPrefs.GetFloat(KEY_MASTER, defaultMaster);
        float mu= PlayerPrefs.GetFloat(KEY_MUSIC,  defaultMusic);
        float s = PlayerPrefs.GetFloat(KEY_SFX,    defaultSFX);

        SetMaster01(m, save:false);
        SetMusic01(mu,  save:false);
        SetSFX01(s,     save:false);
    }

    // Public API (0..1 linear slider values)
    public void SetMaster01(float v, bool save=true) { SetMixer(masterParam, v); if (save) PlayerPrefs.SetFloat(KEY_MASTER, v); }
    public void SetMusic01 (float v, bool save=true) { SetMixer(musicParam,  v); if (save) PlayerPrefs.SetFloat(KEY_MUSIC,  v); }
    public void SetSFX01   (float v, bool save=true) { SetMixer(sfxParam,    v); if (save) PlayerPrefs.SetFloat(KEY_SFX,    v); }

    public float GetMaster01() => Get01(masterParam);
    public float GetMusic01 () => Get01(musicParam);
    public float GetSFX01   () => Get01(sfxParam);

    // --- internals ---
    void SetMixer(string param, float v01)
    {
        // Convert 0..1 slider to decibels. 0 => -80dB (silent), 1 => 0dB
        float dB = (v01 <= 0.0001f) ? -80f : 20f * Mathf.Log10(v01);
        mixer.SetFloat(param, dB);
    }

    float Get01(string param)
    {
        if (mixer.GetFloat(param, out float dB))
            return Mathf.Clamp01(Mathf.Pow(10f, dB / 20f));
        return 1f;
    }
}
