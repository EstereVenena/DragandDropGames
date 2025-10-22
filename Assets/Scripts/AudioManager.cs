using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

[DefaultExecutionOrder(-100)] // init before UI/others
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    // ───────────────────────── Mixer & Params ─────────────────────────
    [Header("Mixer Setup")]
    [SerializeField] private AudioMixer mixer;                     // drag your GameMixer
    [SerializeField] private string masterParam = "MasterVol";     // exposed param names
    [SerializeField] private string musicParam  = "MusicVol";
    [SerializeField] private string sfxParam    = "SFXVol";

    [Header("Mixer Groups (optional but recommended)")]
    [SerializeField] private AudioMixerGroup masterGroup;          // drag Master
    [SerializeField] private AudioMixerGroup musicGroup;           // drag Music
    [SerializeField] private AudioMixerGroup sfxGroup;             // drag SFX

    // ───────────────────────── Defaults & Save Keys ─────────────────────────
    [Header("Defaults (0–1)")]
    [Range(0f, 1f)] public float defaultMaster = 0.8f;
    [Range(0f, 1f)] public float defaultMusic  = 0.8f;
    [Range(0f, 1f)] public float defaultSFX    = 0.8f;

    const string KEY_MASTER = "vol_master";
    const string KEY_MUSIC  = "vol_music";
    const string KEY_SFX    = "vol_sfx";

    // ───────────────────────── Music Playback ─────────────────────────
    [Header("Music Playback")]
    public AudioClip startMusic;                // optional: auto-play on first scene
    [Range(0f,1f)] public float musicGain = 1f; // pre-mixer gain (multiplies with mixer)
    public float fadeInSeconds  = 1.5f;
    public float fadeOutSeconds = 0.8f;

    // ───────────────────────── SFX Playback ─────────────────────────
    [Header("SFX")]
    [Tooltip("Small pool so multiple SFX can overlap without cutting each other.")]
    public int sfxVoices = 4;

    // ───────────────────────── Internal State ─────────────────────────
    private AudioSource musicSource;
    private AudioSource[] sfxSources;
    private int sfxIndex;
    private Coroutine musicFadeCo;

    // ───────────────────────── Lifecycle ─────────────────────────
    void Awake()
    {
        // Singleton
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Set up sources
        SetupSources();

        // Load saved volumes -> mixer
        float m  = PlayerPrefs.GetFloat(KEY_MASTER, defaultMaster);
        float mu = PlayerPrefs.GetFloat(KEY_MUSIC,  defaultMusic);
        float s  = PlayerPrefs.GetFloat(KEY_SFX,    defaultSFX);
        SetMaster01(m, save:false);
        SetMusic01 (mu, save:false);
        SetSFX01   (s,  save:false);

        // Auto-start music (optional)
        if (startMusic) PlayMusic(startMusic, loop:true, fadeTime:fadeInSeconds);
    }

    // ───────────────────────── Public Volume API (0..1) ─────────────────────────
    public void SetMaster01(float v, bool save = true) { SetMixer(masterParam, v); if (save) PlayerPrefs.SetFloat(KEY_MASTER, v); }
    public void SetMusic01 (float v, bool save = true) { SetMixer(musicParam,  v); if (save) PlayerPrefs.SetFloat(KEY_MUSIC,  v); }
    public void SetSFX01   (float v, bool save = true) { SetMixer(sfxParam,    v); if (save) PlayerPrefs.SetFloat(KEY_SFX,    v); }

    public float GetMaster01() => Get01(masterParam);
    public float GetMusic01 () => Get01(musicParam);
    public float GetSFX01   () => Get01(sfxParam);

    // ───────────────────────── Music Control ─────────────────────────
    public void PlayMusic(AudioClip clip, bool loop = true, float fadeTime = 0.5f)
    {
        if (!clip) return;
        if (musicFadeCo != null) StopCoroutine(musicFadeCo);
        musicFadeCo = StartCoroutine(FadeInMusicRoutine(clip, loop, Mathf.Max(0f, fadeTime)));
    }

    public void StopMusic(float fadeTime = -1f)
    {
        if (fadeTime < 0f) fadeTime = fadeOutSeconds;
        if (musicFadeCo != null) StopCoroutine(musicFadeCo);
        musicFadeCo = StartCoroutine(FadeOutMusicRoutine(Mathf.Max(0f, fadeTime)));
    }

    // ───────────────────────── SFX One-shots ─────────────────────────
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!clip) return;
        var src = sfxSources[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfxSources.Length;
        src.pitch = pitch;
        // pre-mixer gain; actual output still controlled by mixer SFXVol & MasterVol
        src.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ───────────────────────── Internals ─────────────────────────
    void SetupSources()
    {
        // Music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0f; // faded in
        musicSource.outputAudioMixerGroup = ResolveGroup(musicGroup, "Music");

        // SFX pool
        sfxVoices = Mathf.Max(1, sfxVoices);
        sfxSources = new AudioSource[sfxVoices];
        for (int i = 0; i < sfxVoices; i++)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.loop = false;
            a.volume = 1f;
            a.outputAudioMixerGroup = ResolveGroup(sfxGroup, "SFX");
            sfxSources[i] = a;
        }
    }

    AudioMixerGroup ResolveGroup(AudioMixerGroup assigned, string fallbackName)
    {
        if (assigned) return assigned;
        if (mixer != null)
        {
            var arr = mixer.FindMatchingGroups(fallbackName);
            if (arr != null && arr.Length > 0) return arr[0];
        }
        return null; // fine—source will still play via default route if mixer missing
    }

    // 0..1 -> dB; 0 => -80 dB (mute), 1 => 0 dB
    void SetMixer(string param, float v01)
    {
        if (!mixer) return;
        float dB = (v01 <= 0.0001f) ? -80f : 20f * Mathf.Log10(v01);
        mixer.SetFloat(param, dB);
    }

    float Get01(string param)
    {
        if (!mixer) return 1f;
        if (mixer.GetFloat(param, out float dB))
            return Mathf.Clamp01(Mathf.Pow(10f, dB / 20f));
        return 1f;
    }

    IEnumerator FadeInMusicRoutine(AudioClip clip, bool loop, float time)
    {
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        if (time <= 0f)
        {
            musicSource.volume = musicGain;
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicGain, t / time);
            yield return null;
        }
        musicSource.volume = musicGain;
        musicFadeCo = null;
    }

    IEnumerator FadeOutMusicRoutine(float time)
    {
        if (!musicSource.isPlaying || time <= 0f)
        {
            musicSource.Stop();
            musicSource.volume = 0f;
            musicFadeCo = null;
            yield break;
        }

        float start = musicSource.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / time);
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = 0f;
        musicFadeCo = null;
    }
}
