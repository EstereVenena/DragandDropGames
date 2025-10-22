using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CarLiftSFX : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Clip")]
    [SerializeField] private AudioClip liftClip;   // assign per car in Inspector

    [Header("When to trigger")]
    public bool playOnBeginDrag = true;
    public bool alsoPlayOnPointerDown = false;

    [Header("Sound tweaks")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.5f)] public float pitchJitter = 0.06f;

    bool _playedThisLift;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (alsoPlayOnPointerDown) PlayOnce();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (playOnBeginDrag) PlayOnce();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _playedThisLift = false; // reset for next lift
    }

    void PlayOnce()
    {
        if (_playedThisLift || !liftClip) return;

        var am = ResolveAudioManager();
        if (!am)
        {
            Debug.LogWarning("[CarLiftSFX] No AudioManager found in scene.");
            return;
        }

        _playedThisLift = true;
        float pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        am.PlaySFX(liftClip, volume, pitch);
    }

    // Look up AudioManager without trying to assign to the singleton
    AudioManager ResolveAudioManager()
    {
        if (AudioManager.I != null) return AudioManager.I;

        // Normal scene search (active objects)
        var am = FindObjectOfType<AudioManager>();
        if (am) return am;

        // Last resort: includes inactive/DontDestroyOnLoad
        var all = Resources.FindObjectsOfTypeAll<AudioManager>();
        return (all != null && all.Length > 0) ? all[0] : null;
    }
}
