using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ObstaclesControllerScript : MonoBehaviour
{
    [Header("Motion")]
    [HideInInspector] public float speed = 1f; // px/sec in UI space

    [Header("Bobbing (Sine)")]
    public bool bobEnabled = true;
    [Min(0f)] public float waveAmplitude = 25f;
    [Min(0f)] public float waveFrequency = 1f;
    public float phaseOffset = 0f;

    [Header("FX")]
    public float fadeDuration = 1.0f;
    public float explosionRadiusPx = 220f;

    [Header("Bounds")]
    public ScreenBoundriesScript screenBoundriesScript;
    public float worldEdgeMargin = 0.25f;

    // cached
    private ObjectScript objectScript;
    private CanvasGroup canvasGroup;
    private RectTransform rt;
    private Image image;
    private Color originalColor;
    private Canvas rootCanvas;
    private Camera uiCam; // for Screen Space - Camera canvases

    // state
    private bool isFadingOut = false;
    internal bool isExploding = false;
    private float baseY;
    private float waveT;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        if (image)
        {
            originalColor = image.color;
            image.raycastTarget = false; // <<< IMPORTANT: never block drags
        }

        objectScript = Object.FindFirstObjectByType<ObjectScript>(FindObjectsInactive.Exclude);
        if (!screenBoundriesScript)
            screenBoundriesScript = Object.FindFirstObjectByType<ScreenBoundriesScript>(FindObjectsInactive.Exclude);

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas) uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? rootCanvas.worldCamera : null;
    }

    void Start()
    {
        baseY = rt ? rt.anchoredPosition.y : transform.position.y;
        waveT = phaseOffset;
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        // Move in UI space
        if (rt)
        {
            var pos = rt.anchoredPosition;
            pos.x += speed * Time.deltaTime;

            if (bobEnabled && waveAmplitude > 0f && waveFrequency > 0f)
            {
                waveT += Time.deltaTime * Mathf.PI * 2f * waveFrequency;
                pos.y = baseY + Mathf.Sin(waveT) * waveAmplitude;
            }
            else pos.y = baseY;

            rt.anchoredPosition = pos;

            // Robust off-screen check (world space corners with canvas camera)
            if (!isFadingOut && screenBoundriesScript)
            {
                // If you already compute minX/maxX as world coords, keep using them
                float leftEdge  = screenBoundriesScript.minX - worldEdgeMargin;
                float rightEdge = screenBoundriesScript.maxX + worldEdgeMargin;

                // World center X of this UI element
                Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
                float xWorld = worldCenter.x;

                if (speed > 0f && xWorld > rightEdge) BeginFadeOut();
                if (speed < 0f && xWorld < leftEdge)  BeginFadeOut();
            }
        }
        else
        {
            transform.position += new Vector3(speed * Time.deltaTime, 0f, 0f);
        }

        // Hover explode for items tagged Bomb (optional)
        if (CompareTag("Bomb") && !isExploding && rt &&
            RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, uiCam))
        {
            TriggerExplosion();
        }
    }

    public void TriggerExplosion()
    {
        if (isExploding) return;
        isExploding = true;

        if (objectScript) objectScript.effects.PlayOneShot(objectScript.audioCli[6], 5f);
        if (TryGetComponent<Animator>(out var anim)) anim.SetBool("explode", true);

        if (image)
        {
            image.color = Color.red;
            StartCoroutine(RecoverColor(0.4f));
        }

        StartCoroutine(Vibrate());
        StartCoroutine(ExplodeNow());
    }

    IEnumerator ExplodeNow()
    {
        Vector2 myUI = rt ? (Vector2)rt.position : (Vector2)transform.position;

        var victims = Object.FindObjectsByType<ObstaclesControllerScript>(FindObjectsSortMode.None);
        foreach (var v in victims)
        {
            if (!v || v == this) continue;
            var vrt = v.GetComponent<RectTransform>();
            Vector2 theirUI = vrt ? (Vector2)vrt.position : (Vector2)v.transform.position;

            if (Vector2.Distance(myUI, theirUI) <= explosionRadiusPx && !v.isExploding)
                v.StartToDestroy(Color.cyan);
        }

        yield return new WaitForSeconds(0.15f);
        BeginFadeOut();
    }

    public void StartToDestroy(Color c)
    {
        if (isFadingOut) return;

        if (image)
        {
            image.color = c;
            StartCoroutine(RecoverColor(0.5f));
        }

        StartCoroutine(Vibrate());
        if (objectScript) objectScript.effects.PlayOneShot(objectScript.audioCli[5]);

        BeginFadeOut();
    }

    void BeginFadeOut()
    {
        if (isFadingOut) return;
        isFadingOut = true;
        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutAndDestroy()
    {
        float t = 0f, start = canvasGroup.alpha;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }

    IEnumerator RecoverColor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (image) image.color = originalColor;
    }

    IEnumerator Vibrate()
    {
        if (!rt) yield break;
        Vector2 orig = rt.anchoredPosition;
        float dur = 0.3f, el = 0f, intensity = 5f;

        while (el < dur)
        {
            rt.anchoredPosition = orig + Random.insideUnitCircle * intensity;
            el += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = orig;
    }
}
