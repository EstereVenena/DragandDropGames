using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonPressFX : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Targets")]
    [SerializeField] private RectTransform target;   // Defaults to this object if left empty
    [SerializeField] private RectTransform icon;     // Optional: your X / car icon child

    [Header("Press Feel")]
    [Range(0.85f, 1.0f)] public float pressedScale = 0.94f;
    public float pressMoveY = -2f;
    public float animSpeed = 12f;

    [Header("Optional UI Shadow (Effect)")]
    public Shadow shadow;                            // (Add UI > Effects > Shadow to the button Image)
    public Vector2 pressedShadowMultiplier = new Vector2(0.3f, 0.3f);

    private Vector3 startScale;
    private Vector2 startPos;
    private Vector2 startShadow;

    void Awake()
    {
        if (!target) target = transform as RectTransform;
        startScale = target.localScale;
        startPos   = target.anchoredPosition;
        if (shadow) startShadow = shadow.effectDistance;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(TweenTo(
            startScale * pressedScale,
            startPos + new Vector2(0f, pressMoveY),
            shadow ? (Vector2?)Vector2.Scale(startShadow, pressedShadowMultiplier) : null
        ));

        if (icon) icon.localEulerAngles = new Vector3(0f, 0f, -6f); // subtle tilt
    }

    public void OnPointerUp(PointerEventData eventData)  => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();

    private void Release()
    {
        StopAllCoroutines();
        StartCoroutine(TweenTo(
            startScale,
            startPos,
            shadow ? (Vector2?)startShadow : null
        ));
        if (icon) icon.localEulerAngles = Vector3.zero;
    }

    private IEnumerator TweenTo(Vector3 s, Vector2 p, Vector2? shadowDst)
    {
        float k = animSpeed;
        while ((target.localScale - s).sqrMagnitude > 1e-6f ||
               (target.anchoredPosition - p).sqrMagnitude > 1e-3f ||
               (shadow && shadowDst.HasValue && (shadow.effectDistance - shadowDst.Value).sqrMagnitude > 1e-3f))
        {
            float t = 1f - Mathf.Exp(-k * Time.unscaledDeltaTime);
            target.localScale       = Vector3.Lerp(target.localScale, s, t);
            target.anchoredPosition = Vector2.Lerp(target.anchoredPosition, p, t);
            if (shadow && shadowDst.HasValue)
                shadow.effectDistance = Vector2.Lerp(shadow.effectDistance, shadowDst.Value, t);
            yield return null;
        }
        target.localScale = s;
        target.anchoredPosition = p;
        if (shadow && shadowDst.HasValue)
            shadow.effectDistance = shadowDst.Value;
    }
}
