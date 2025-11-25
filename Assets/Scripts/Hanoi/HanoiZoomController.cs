using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HanoiZoomController : MonoBehaviour
{
    [Header("Target (HanoiZoomRoot)")]
    [Tooltip("RectTransform, kas satur visu Hanojas spēli (background + torņi + diski).")]
    public RectTransform target; // piem., Canvas/HanoiZoomRoot

    [Header("Buttons (optional)")]
    public Button zoomInButton;   // + poga
    public Button zoomOutButton;  // - poga

    [Header("Zoom Settings")]
    [Tooltip("Minimālais mērogs (1 = oriģinālais izmērs).")]
    public float minScale = 1f;

    [Tooltip("Maksimālais mērogs.")]
    public float maxScale = 1.6f;

    [Tooltip("Cik daudz mainām scale katrā solī (poga vai rullītis).")]
    public float step = 0.15f;

    [Tooltip("Cik sekundes notiek mīkstā animācija.")]
    public float tweenSeconds = 0.15f;

    [Header("Input")]
    [Tooltip("Vai atļaut zoom ar peles rullīti.")]
    public bool enableMouseWheel = true;

    [Tooltip("Rullīša jutīgums (1 = normāli).")]
    public float wheelSensitivity = 1f;

    private float _targetScale = 1f;
    private Coroutine _tween;

    private void Awake()
    {
        // Ja nav ielikts inspectorā – mēģinām atrast pēc nosaukuma
        if (!target)
        {
            var go = GameObject.Find("HanoiZoomRoot");
            if (go != null)
                target = go.GetComponent<RectTransform>();
        }

        if (!target)
        {
            Debug.LogWarning("[HanoiZoomController] Target nav iestatīts un 'HanoiZoomRoot' nav atrasts.");
            enabled = false;
            return;
        }

        // Ankori un pivot tieši centrā, lai zoom nešiftētos
        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot    = new Vector2(0.5f, 0.5f);

        // Starta scale
        _targetScale = 1f;
        target.localScale = Vector3.one;

        // Piesienam pogas, ja ir piešķirtas
        if (zoomInButton != null)
            zoomInButton.onClick.AddListener(ZoomIn);

        if (zoomOutButton != null)
            zoomOutButton.onClick.AddListener(ZoomOut);
    }

    private void OnDestroy()
    {
        // Droši atvienojam listenerus
        if (zoomInButton != null)
            zoomInButton.onClick.RemoveListener(ZoomIn);

        if (zoomOutButton != null)
            zoomOutButton.onClick.RemoveListener(ZoomOut);
    }

    private void Update()
    {
        if (!enableMouseWheel || target == null)
            return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            float delta = Mathf.Sign(scroll) * step * wheelSensitivity;
            SetScaleAnimated(_targetScale + delta);
        }
    }

    // --- Publiskās funkcijas pogām ---

    public void ZoomIn()
    {
        SetScaleAnimated(_targetScale + step);
    }

    public void ZoomOut()
    {
        SetScaleAnimated(_targetScale - step);
    }

    public void SetScaleInstant(float s)
    {
        _targetScale = Mathf.Clamp(s, minScale, maxScale);
        target.localScale = new Vector3(_targetScale, _targetScale, 1f);
    }

    public void SetScaleAnimated(float s)
    {
        _targetScale = Mathf.Clamp(s, minScale, maxScale);

        if (_tween != null)
            StopCoroutine(_tween);

        _tween = StartCoroutine(TweenScale(_targetScale, tweenSeconds));
    }

    private IEnumerator TweenScale(float toScale, float seconds)
    {
        float from = target.localScale.x;

        if (Mathf.Approximately(from, toScale) || seconds <= 0f)
        {
            SetScaleInstant(toScale);
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            float s = Mathf.Lerp(from, toScale, k);
            target.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        target.localScale = new Vector3(toScale, toScale, 1f);
        _tween = null;
    }
}
