// Assets/Scripts/OptionsAudioUI.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OptionsAudioUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Slider master;
    public Slider music;
    public Slider sfx;

    [Tooltip("Try to auto-find sliders by name if fields are left unassigned.")]
    public bool autoFindSliders = true;

    [Tooltip("Wait a bit for AudioManager to spawn (DontDestroyOnLoad) before wiring UI.")]
    public float waitForManagerSeconds = 2f;

    void OnEnable()
    {
        StartCoroutine(InitRoutine());
    }

    IEnumerator InitRoutine()
    {
        // Optional autofind by common names
        if (autoFindSliders)
        {
            if (!master) master = GameObject.Find("MasterSlider")?.GetComponent<Slider>();
            if (!music)  music  = GameObject.Find("MusicSlider") ?.GetComponent<Slider>();
            if (!sfx)    sfx    = GameObject.Find("SFXSlider")   ?.GetComponent<Slider>();
        }

        // Wait for AudioManager singleton to exist (eg. created in another scene)
        float t = 0f;
        while (AudioManager.I == null && t < waitForManagerSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null; // next frame
        }
        if (AudioManager.I == null)
        {
            Debug.LogWarning("[OptionsAudioUI] AudioManager not found; UI will be disabled.");
            yield break;
        }

        // Guard against missing sliders (don’t NRE)
        if (!master || !music || !sfx)
        {
            Debug.LogWarning("[OptionsAudioUI] One or more Slider references are missing.");
            yield break;
        }

        // Initialize slider values without triggering callbacks
        master.SetValueWithoutNotify(AudioManager.I.GetMaster01());
        music .SetValueWithoutNotify(AudioManager.I.GetMusic01());
        sfx   .SetValueWithoutNotify(AudioManager.I.GetSFX01());

        // Hook listeners (remove old first to avoid stacking)
        master.onValueChanged.RemoveAllListeners();
        music .onValueChanged.RemoveAllListeners();
        sfx   .onValueChanged.RemoveAllListeners();

        master.onValueChanged.AddListener(v => AudioManager.I.SetMaster01(v));
        music .onValueChanged.AddListener(v => AudioManager.I.SetMusic01(v));
        sfx   .onValueChanged.AddListener(v => AudioManager.I.SetSFX01(v));
    }
}
