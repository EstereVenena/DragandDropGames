using UnityEngine;
using UnityEngine.UI;

public class OptionsAudioUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    void Start()
    {
        // Initialize from current settings
        if (AudioManager.I)
        {
            masterSlider.SetValueWithoutNotify(AudioManager.I.GetMaster01());
            musicSlider .SetValueWithoutNotify(AudioManager.I.GetMusic01());
            sfxSlider   .SetValueWithoutNotify(AudioManager.I.GetSFX01());
        }

        // Wire events
        masterSlider.onValueChanged.AddListener(v => AudioManager.I?.SetMaster01(v));
        musicSlider .onValueChanged.AddListener(v => AudioManager.I?.SetMusic01(v));
        sfxSlider   .onValueChanged.AddListener(v => AudioManager.I?.SetSFX01(v));
    }
}
