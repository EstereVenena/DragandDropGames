using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BombController : MonoBehaviour, IPointerClickHandler
{
    public enum ExplosionCause { Timeout, Click, DragOverlap }

    [Header("Motion & Fuse")]
    [HideInInspector] public float speed;
    public float fuseSeconds = 8f;
    public float waveAmplitude = 25f;
    public float waveFrequency = 1f;

    [Header("Explosion")]
    public float radiusPx = 220f;
    public bool explodeOnClick = true;
    public bool explodeOnDragOverlap = true;

    [Header("Penalty Rules")]
    public bool addPenaltyOnClick = true;
    public bool addPenaltyOnDragOverlap = true;
    public bool addPenaltyOnTimeout = false;
    public int penaltyPerExplosion = 1;

    [Header("Visibility Guard (optional)")]
    public RectTransform playArea;
    public bool requireVisibleInPlayArea = true;

    [Header("Penalty Hook")]
    public PenaltyCounterUI penaltyUI;

    // Internal
    RectTransform rt;
    float baseY;
    float t;
    bool exploded;
    Canvas rootCanvas;
    Camera uiCam;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            uiCam = rootCanvas.worldCamera;

        // make sure it’s clickable
        var img = GetComponent<Image>();
        if (img) img.raycastTarget = true;

        // fallback auto-find
        if (penaltyUI == null)
            penaltyUI = FindFirstObjectByType<PenaltyCounterUI>(FindObjectsInactive.Exclude);
    }

    void Start()
    {
        baseY = rt ? rt.anchoredPosition.y : transform.position.y;
        StartCoroutine(FuseTimer());
    }

    IEnumerator FuseTimer()
    {
        float timer = 0f;
        while (!exploded && timer < fuseSeconds)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        if (!exploded) Explode(ExplosionCause.Timeout);
    }

    void Update()
    {
        if (exploded) return;

        t += Time.deltaTime * Mathf.PI * 2f * waveFrequency;

        if (rt)
        {
            var pos = rt.anchoredPosition;
            pos.x += speed * Time.deltaTime;
            pos.y = baseY + Mathf.Sin(t) * waveAmplitude;
            rt.anchoredPosition = pos;
        }
        else
        {
            var pos = transform.position;
            pos.x += speed * Time.deltaTime;
            transform.position = pos;
        }

        // drag-overlap
        if (explodeOnDragOverlap && DragState.IsDragging && OverlapsUI(rt, DragState.Current, uiCam))
            Explode(ExplosionCause.DragOverlap);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (explodeOnClick && !exploded)
            Explode(ExplosionCause.Click);
    }

    // ---------------- core ----------------

    void Explode(ExplosionCause cause)
    {
        if (exploded) return;
        exploded = true;

        // visual/audio anim (if present)
        var anim = GetComponent<BombExplosionAnim>();
        if (anim) StartCoroutine(PlayAnimThenDestroy(anim));
        else       StartCoroutine(FadeOut());

        // “damage” obstacles
        Vector3 center = rt ? (Vector3)rt.position : transform.position;
        var hits = Physics2D.OverlapCircleAll(center, radiusPx);
        foreach (var h in hits)
        {
            var target = h ? h.GetComponent<ObstaclesControllerScript>() : null;
            if (target) target.StartToDestroy(Color.red);
        }

        // Penalty conditions
        bool inside = !requireVisibleInPlayArea || IsInsidePlayArea(uiCam);
        bool shouldAdd =
            (cause == ExplosionCause.Click       && addPenaltyOnClick) ||
            (cause == ExplosionCause.DragOverlap && addPenaltyOnDragOverlap) ||
            (cause == ExplosionCause.Timeout     && addPenaltyOnTimeout);

        if (shouldAdd && inside && penaltyUI)
        {
            penaltyUI.AddPenalties(penaltyPerExplosion);
            Debug.Log($"[Bomb] Penalty +{penaltyPerExplosion} (cause={cause}, inside={inside})");
        }
        else
        {
            Debug.Log($"[Bomb] No penalty (cause={cause}, inside={inside})");
        }
    }

    IEnumerator PlayAnimThenDestroy(BombExplosionAnim anim)
    {
        anim.StartFuse(0.01f);
        yield return new WaitForSeconds(0.35f);
        Destroy(gameObject);
    }

    IEnumerator FadeOut()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        float tt = 0f, dur = 0.35f;
        while (tt < dur)
        {
            tt += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, tt / dur);
            yield return null;
        }
        Destroy(gameObject);
    }

    static bool OverlapsUI(RectTransform a, RectTransform b, Camera cam)
    {
        if (!a || !b) return false;
        Vector3[] ac = new Vector3[4];
        Vector3[] bc = new Vector3[4];
        a.GetWorldCorners(ac);
        b.GetWorldCorners(bc);
        for (int i = 0; i < 4; i++)
        {
            if (cam)
            {
                ac[i] = cam.WorldToScreenPoint(ac[i]);
                bc[i] = cam.WorldToScreenPoint(bc[i]);
            }
        }
        return ToRect(ac).Overlaps(ToRect(bc));
    }

    bool IsInsidePlayArea(Camera cam)
    {
        if (!playArea || !rt) return true;
        Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
        Vector2 screenPt = cam ? (Vector2)cam.WorldToScreenPoint(worldCenter) : (Vector2)worldCenter;
        return RectTransformUtility.RectangleContainsScreenPoint(playArea, screenPt, cam);
    }

    static Rect ToRect(Vector3[] c)
    {
        float xMin = c[0].x, xMax = c[0].x, yMin = c[0].y, yMax = c[0].y;
        for (int i = 1; i < 4; i++)
        {
            var v = c[i];
            if (v.x < xMin) xMin = v.x;
            if (v.x > xMax) xMax = v.x;
            if (v.y < yMin) yMin = v.y;
            if (v.y > yMax) yMax = v.y;
        }
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
}
