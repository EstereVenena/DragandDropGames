// Assets/Scripts/UI/MapZoomController.cs
// Vienkāršs un drošs zoom priekš UI kartes / spēles laukuma.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapZoomController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("RectTransform, kuru gribi zoomot (tavs Map / Background root).")]
    public RectTransform target;   // piem. Canvas/Map vai Background image

    [Header("Buttons (optional)")]
    public Button zoomInButton;
    public Button zoomOutButton;

    [Header("Zoom Settings")]
    [Tooltip("Minimālais zoom. 1 = sākuma izmērs.")]
    public float minScale = 1.0f;
    [Tooltip("Maksimālais zoom.")]
    public float maxScale = 2.0f;
    [Tooltip("Solītis vienam + / - klikšķim.")]
    public float step = 0.15f;
    [Tooltip("Cik ātri animēt zoom (sekundēs).")]
    public float tweenSeconds = 0.15f;

    [Header("Input")]
        [Header("Follow (optional)")]
    [Tooltip("UI elements (mašīna), kuram kartei jāsako.")]
    public RectTransform followTarget;
    [Tooltip("Vai sekot targetam.")]
    public bool followEnabled = false;
    [Tooltip("Offset no ekrāna centra (px UI telpā).")]
    public Vector2 followOffset = Vector2.zero;
    [Tooltip("Cik gludi sekot (0 = instant).")]
    public float followSpeed = 10f;

    [Tooltip("Atļaut zoom ar peles rullīti.")]
    public bool enableMouseWheel = true;
    [Tooltip("Atļaut pinch (2 pirksti) uz touch ierīcēm.")]
    public bool enablePinch = true;
    [Tooltip("Peles rullīša jūtība.")]
    public float wheelSensitivity = 1.0f;
    [Tooltip("Pinch jūtība.")]
    public float pinchSensitivity = 0.005f;

    Coroutine _tween;
    float _targetScale = 1f;
    float _initialScale = 1f;

    void Awake()
    {
        // Ja target nav uzlikts Inspectorā, mēģinām atrast "Map"
        if (!target)
        {
            var t = GameObject.Find("Map");
            if (t) target = t.GetComponent<RectTransform>();
        }

        if (!target)
        {
            Debug.LogWarning("[MapZoomController] Target nav uzlikts un 'Map' neatradu.");
            enabled = false;
            return;
        }

        // NEKUSTINĀM anchorus/pivotu – tu jau esi to sakārtojis.
        _initialScale = target.localScale.x;
        _targetScale  = _initialScale;

        // neļaujam izzoomot mazāk par sākotnējo izmēru
        if (minScale < _initialScale)
            minScale = _initialScale;

        // Piesienam pogas (ja iedotas)
        if (zoomInButton)  zoomInButton.onClick.AddListener(ZoomIn);
        if (zoomOutButton) zoomOutButton.onClick.AddListener(ZoomOut);
    }

    void Start()
    {
        // Piespiedu kārtā noliekam sākuma scale,
        // gadījumam, ja editorā kaut kas bija samainīts.
        SetScaleInstant(_initialScale);
    }

    void Update()
    {
        // Peles rullītis
        if (enableMouseWheel)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                float delta = Mathf.Sign(scroll) * step * wheelSensitivity;
                SetScaleAnimated(_targetScale + delta);
            }
        }

        // Pinch uz touch
        if (enablePinch && Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            float prevMag = (t0Prev - t1Prev).magnitude;
            float currMag = (t0.position - t1.position).magnitude;
            float diff = currMag - prevMag;

            if (Mathf.Abs(diff) > 0.01f)
            {
                float delta = diff * pinchSensitivity;
                SetScaleAnimated(_targetScale + delta);
            }
        }
    }

    // Pogām
    public void ZoomIn()  => SetScaleAnimated(_targetScale + step);
    public void ZoomOut() => SetScaleAnimated(_targetScale - step);

    // Uzreiz uzliek scale (bez animācijas)
    public void SetScaleInstant(float s)
    {
        _targetScale = Mathf.Clamp(s, minScale, maxScale);
        target.localScale = new Vector3(_targetScale, _targetScale, 1f);
    }

    // Ar animāciju
    public void SetScaleAnimated(float s)
    {
        _targetScale = Mathf.Clamp(s, minScale, maxScale);
        if (_tween != null) StopCoroutine(_tween);
        _tween = StartCoroutine(TweenScale(_targetScale, tweenSeconds));
    }

    IEnumerator TweenScale(float toScale, float seconds)
    {
        float from = target.localScale.x;
        if (Mathf.Approximately(from, toScale))
            yield break;

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
