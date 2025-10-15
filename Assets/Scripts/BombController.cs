using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BombController : MonoBehaviour
{
    [HideInInspector] public float speed;
    public float fuseSeconds = 3f;
    public float waveAmplitude = 25f;
    public float waveFrequency = 1f;
    public float radiusPx = 220f;

    RectTransform rt;
    float baseY;
    float t;
    bool exploded;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        baseY = rt ? rt.anchoredPosition.y : transform.position.y;
        StartCoroutine(FuseTimer());
    }

    IEnumerator FuseTimer()
    {
        float timer = 0f;
        while (timer < fuseSeconds)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        Explode();
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
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector3 center = rt ? (Vector3)rt.position : transform.position;
        var hits = Physics2D.OverlapCircleAll(center, radiusPx);

        foreach (var h in hits)
        {
            var target = h ? h.GetComponent<ObstaclesControllerScript>() : null;
            if (target) target.StartToDestroy(Color.red);
        }

        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        float t = 0f, dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / dur);
            yield return null;
        }
        Destroy(gameObject);
    }
}
