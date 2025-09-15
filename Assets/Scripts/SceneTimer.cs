using UnityEngine;
using TMPro;

public class SceneTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text label;   // drag your TMP text here
    [SerializeField] private bool useUnscaledTime = false; // ON if you want it to tick while paused
    [SerializeField] private bool alwaysShowHours = false; // force H:MM:SS

    private float unscaledStart;

    private void OnEnable()
    {
        // For unscaled timing we need a reference point
        unscaledStart = Time.unscaledTime;
        if (label) label.text = "00:00";
    }

    private void Update()
    {
        float seconds = useUnscaledTime
            ? Time.unscaledTime - unscaledStart
            : Time.timeSinceLevelLoad;   // resets to 0 when CityScene loads (LoadSceneMode.Single)

        if (label) label.text = FormatTime(seconds, alwaysShowHours);
    }

    private string FormatTime(float secs, bool forceHours)
    {
        int total = Mathf.FloorToInt(secs);
        int h = total / 3600;
        int m = (total % 3600) / 60;
        int s = total % 60;

        if (forceHours || h > 0) return $"{h:D2}:{m:D2}:{s:D2}";
        return $"{m:D2}:{s:D2}";
    }
}
