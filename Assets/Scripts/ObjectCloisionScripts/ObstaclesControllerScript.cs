using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ObstaclesControllerScript : MonoBehaviour
{
    [Header("Motion")]
    [HideInInspector] public float speed = 1f;

    [Header("Bobbing (Sine)")]
    public bool bobEnabled = true;
    [Min(0f)] public float waveAmplitude = 25f;
    [Min(0f)] public float waveFrequency = 1f;
    public float phaseOffset = 0f;

    [Header("FX")]
    public float fadeDuration = 1f;
    public float explosionRadiusPx = 220f;

    [Header("Bounds")]
    public ScreenBoundriesScript screenBoundriesScript;
    public float worldEdgeMargin = 0.25f;

    // Cached references
    private ObjectScript objectScript;
    private CanvasGroup canvasGroup;
    internal RectTransform rt;
    private Image image;
    private Color originalColor;
    private Camera uiCam;

    // State
    private bool isFadingOut = false;
    internal bool isExploding = false;
    private float baseY;
    private float wavePhase;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        if (image)
        {
            originalColor = image.color;
            image.raycastTarget = false;
        }

        objectScript = Object.FindFirstObjectByType<ObjectScript>(FindObjectsInactive.Exclude);
        if (!screenBoundriesScript)
            screenBoundriesScript = Object.FindFirstObjectByType<ScreenBoundriesScript>(FindObjectsInactive.Exclude);

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas)
            uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? rootCanvas.worldCamera : null;
    }

    void Start()
    {
        baseY = rt ? rt.anchoredPosition.y : transform.position.y;
        wavePhase = phaseOffset;
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        HandleMovement();
        HandleBoundsCheck();
        HandleInput();
    }

    #region Movement & Bounds
    private void HandleMovement()
    {
        if (!rt) return;

        // Horizontal movement
        Vector2 pos = rt.anchoredPosition;
        pos.x += speed * Time.deltaTime;

        // Sine bobbing
        if (bobEnabled && waveAmplitude > 0f && waveFrequency > 0f)
        {
            wavePhase += Time.deltaTime * Mathf.PI * 2f * waveFrequency;
            pos.y = baseY + Mathf.Sin(wavePhase) * waveAmplitude;
        }
        else
        {
            pos.y = baseY;
        }

        rt.anchoredPosition = pos;
    }

    private void HandleBoundsCheck()
    {
        if (isFadingOut || !screenBoundriesScript || !rt) return;

        float leftEdge = screenBoundriesScript.minCamX - worldEdgeMargin;
        float rightEdge = screenBoundriesScript.maxCamX + worldEdgeMargin;
        Vector3 worldX = rt.TransformPoint(rt.rect.center);

        if ((speed > 0f && worldX.x > rightEdge) || (speed < 0f && worldX.x < leftEdge))
            BeginFadeOut();
    }
    #endregion

    #region Input
    private void HandleInput()
    {
        if (!rt) return;

        if (!TryGetInputPosition(out Vector2 inputPos))
            return;

        // JA kaut kas ir nogājis sviestā un koordinātes ir NaN/Inf – neko nedaram, lai nebūtu error.
        if (!IsValidScreenPos(inputPos))
            return;

        bool hit = RectTransformUtility.RectangleContainsScreenPoint(rt, inputPos, uiCam);

        // Hover explode for Bomb
        if (CompareTag("Bomb") && !isExploding && hit)
            TriggerExplosion();

        // Drag hit
        if (ObjectScript.drag && hit && !isFadingOut)
        {
            if (ObjectScript.lastDragged != null)
            {
                StartCoroutine(ShrinkAndDestroy(ObjectScript.lastDragged, 0.5f));
                ObjectScript.lastDragged = null;
                ObjectScript.drag = false;
            }

            StartToDestroy(CompareTag("Bomb") ? Color.red : Color.cyan);
        }
    }

    /// <summary>
    /// Nolasām ievades pozīciju – editorī/PC izmanto peli, uz touch ierīcēm – pirmo touch.
    /// Ja nav nevienas ievades, atgriežam false.
    /// </summary>
    bool TryGetInputPosition(out Vector2 position)
    {
        // Touch ir prioritāte uz ierīcēm, kur tas ir pieejams
        if (Input.touchSupported && Input.touchCount > 0)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        // Ja ir pele – ņemam tās pozīciju
        if (Input.mousePresent)
        {
            position = Input.mousePosition;
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    bool IsValidScreenPos(Vector2 p)
    {
        return !(float.IsNaN(p.x) || float.IsNaN(p.y) ||
                 float.IsInfinity(p.x) || float.IsInfinity(p.y));
    }
    #endregion

    #region Explosion / Destroy
    public void TriggerExplosion()
    {
        if (isExploding) return;
        isExploding = true;

        objectScript?.effects.PlayOneShot(objectScript.audioCli[6], 5f);
        if (TryGetComponent<Animator>(out var anim)) anim.SetBool("explode", true);

        if (image)
        {
            image.color = Color.red;
            StartCoroutine(RecoverColor(0.4f));
        }

        StartCoroutine(Vibrate());
        StartCoroutine(ExplodeNearby());
    }

    IEnumerator ExplodeNearby()
    {
        Vector2 myPos = rt ? (Vector2)rt.position : (Vector2)transform.position;

        // UI Distance check
        var obstacles = Object.FindObjectsByType<ObstaclesControllerScript>(FindObjectsSortMode.None);
        foreach (var o in obstacles)
        {
            if (o == null || o == this || o.isExploding) continue;
            Vector2 theirPos = o.rt ? (Vector2)o.rt.position : (Vector2)o.transform.position;
            if (Vector2.Distance(myPos, theirPos) <= explosionRadiusPx)
                o.StartToDestroy(Color.cyan);
        }

        // Physics2D overlap
        if (TryGetComponent<CircleCollider2D>(out var circle))
        {
            float radius = circle.radius * transform.lossyScale.x;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                if (hit != null && hit.gameObject != gameObject)
                {
                    var obj = hit.GetComponent<ObstaclesControllerScript>();
                    if (obj != null && !obj.isExploding)
                        obj.StartToDestroy(Color.cyan);
                }
            }
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
        objectScript?.effects.PlayOneShot(objectScript.audioCli[5]);

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

    IEnumerator ShrinkAndDestroy(GameObject target, float duration)
    {
        Vector3 origScale = target.transform.localScale;
        Quaternion origRot = target.transform.rotation;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            target.transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t / duration);
            float angle = Mathf.Lerp(0, 360, t / duration);
            target.transform.rotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        Destroy(target);
    }

    IEnumerator RecoverColor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (image) image.color = originalColor;
    }

    IEnumerator Vibrate()
    {
#if UNITY_ANDROID
        Handheld.Vibrate();
#endif
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
    #endregion
}
