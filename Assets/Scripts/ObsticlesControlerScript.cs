using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform), typeof(Image))]
public class ObstaclesControllerScript : MonoBehaviour
{
    [HideInInspector]
    public float speed = 1f;

    public float fadeDuration = 1.5f;
    public float waveAmplitude = 25f;
    public float waveFrequency = 1f;

    private ObjectScript objectScript;
    private ScreenBoundariesScript screenBoundariesScript;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Image image;

    private Color originalColor;
    private bool isFadingOut = false;
    public bool isExploding = false;

    private float baseY;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        originalColor = image.color;

        baseY = rectTransform.anchoredPosition.y;

        objectScript = FindFirstObjectByType<ObjectScript>();
        screenBoundariesScript = FindFirstObjectByType<ScreenBoundariesScript>();

        StartCoroutine(FadeIn());
    }

    void Update()
    {
        // Wavy horizontal movement
        float waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
        Vector2 pos = rectTransform.anchoredPosition;
        pos.x -= speed * Time.deltaTime;
        pos.y = baseY + waveOffset;
        rectTransform.anchoredPosition = pos;

        float worldX = transform.position.x;

        // Left edge fade out
        if (speed > 0 && worldX < (screenBoundariesScript.minX + 80) && !isFadingOut)
        {
            StartToDestroy();
        }

        // Right edge fade out
        if (speed < 0 && worldX > (screenBoundariesScript.maxX - 80) && !isFadingOut)
        {
            StartToDestroy();
        }

        // Mouse collision with Bomb (without car dragging)
        if (CompareTag("Bomb") && !isExploding &&
            RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, Camera.main))
        {
            Debug.Log("The cursor collided with a bomb! (without car)");
            TriggerExplosion();
        }

        // Collision with flying object while dragging
        if (ObjectScript.drag && !isFadingOut &&
            RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, Camera.main))
        {
            Debug.Log("The cursor collided with a flying object!");

            if (ObjectScript.lastDragged != null)
            {
                StartCoroutine(ShrinkAndDestroy(ObjectScript.lastDragged, 0.5f));
                ObjectScript.lastDragged = null;
                ObjectScript.drag = false;
            }

            StartToDestroy();
        }
    }

    public void TriggerExplosion()
    {
        isExploding = true;
        objectScript.effects.PlayOneShot(objectScript.audioCli[6], 5f);

        if (TryGetComponent<Animator>(out Animator animator))
        {
            animator.SetBool("explode", true);
        }

        image.color = Color.red;
        StartCoroutine(RecoverColor(0.4f));

        StartCoroutine(Vibrate());
        StartCoroutine(WaitBeforeExplode());
    }

    IEnumerator WaitBeforeExplode()
    {
        float radius = 0f;

        if (TryGetComponent<CircleCollider2D>(out CircleCollider2D circleCollider))
        {
            radius = circleCollider.radius * transform.lossyScale.x;
        }

        yield return new WaitForSeconds(1f);
        ExplodeAndDestroy(radius);
        Destroy(gameObject);
    }

    void ExplodeAndDestroy(float radius)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (var hit in hitColliders)
        {
            if (hit != null && hit.gameObject != gameObject)
            {
                FlyingObjectsControllerScript obj = hit.GetComponent<FlyingObjectsControllerScript>();

                if (obj != null && !obj.isExploding)
                {
                    obj.StartToDestroy();
                }
            }
        }
    }

    public void StartToDestroy()
    {
        if (!isFadingOut)
        {
            isFadingOut = true;
            StartCoroutine(FadeOutAndDestroy());

            image.color = Color.cyan;
            StartCoroutine(RecoverColor(0.5f));

            objectScript.effects.PlayOneShot(objectScript.audioCli[5]);
            StartCoroutine(Vibrate());
        }
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
        float t = 0f;
        float startAlpha = canvasGroup.alpha;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        Destroy(gameObject);
    }

    IEnumerator ShrinkAndDestroy(GameObject target, float duration)
    {
        Vector3 originalScale = target.transform.localScale;
        Quaternion originalRotation = target.transform.rotation;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            target.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t / duration);
            float angle = Mathf.Lerp(0f, 360f, t / duration);
            target.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        Destroy(target);
    }

    IEnumerator RecoverColor(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        image.color = originalColor;
    }

    IEnumerator Vibrate()
    {
        Vector2 originalPosition = rectTransform.anchoredPosition;
        float duration = 0.3f;
        float elapsed = 0f;
        float intensity = 5f;

        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = originalPosition + Random.insideUnitCircle * intensity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
    }

    void OnDrawGizmosSelected()
    {
        if (TryGetComponent<CircleCollider2D>(out CircleCollider2D circleCollider))
        {
            Gizmos.color = Color.red;
            float radius = circleCollider.radius * transform.lossyScale.x;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
