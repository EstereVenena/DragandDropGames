using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BombExplosionAnim : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite unlitSprite;        // single image
    public Sprite litSprite;          // single image
    public Sprite[] explosionFrames;  // 4+ sliced sprites from the sheet

    [Header("Timing")]
    public float fuseSeconds = 2f;    // time from lit to explosion
    public float explosionFPS = 12f;  // how fast to play the frames

    [Header("Audio (optional)")]
    public AudioSource sfx;
    public AudioClip fuseSfx;
    public AudioClip explodeSfx;

    [Header("VFX / Feedback (optional)")]
    public ParticleSystem explosionParticles;
    public bool vibrateOnExplosion = true;

    [Header("Damage")]
    public float damageRadius = 100f;
    public bool autoDamage = true;    // disable if BombController already handles it

    // Cached components
    Image uiImg;
    SpriteRenderer sr;
    bool exploded;
    Coroutine fuseRoutine;

    void Awake()
    {
        uiImg = GetComponent<Image>();
        sr = GetComponent<SpriteRenderer>();

        SetSprite(unlitSprite);
        if (uiImg) uiImg.preserveAspect = true;
    }

    void OnEnable()
    {
        // Optional: start automatically when spawned
        StartFuse();
    }

    public void StartFuse(float overrideSeconds = -1f)
    {
        if (overrideSeconds > 0f) fuseSeconds = overrideSeconds;

        // Prevent multiple concurrent coroutines
        if (fuseRoutine != null)
            StopCoroutine(fuseRoutine);

        fuseRoutine = StartCoroutine(FuseRoutine());
    }

    IEnumerator FuseRoutine()
    {
        SetSprite(litSprite);
        if (sfx && fuseSfx) sfx.PlayOneShot(fuseSfx);

        float t = 0f;
        while (t < fuseSeconds && !exploded)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!exploded)
            yield return ExplosionRoutine();
    }

    IEnumerator ExplosionRoutine()
    {
        if (exploded) yield break;
        exploded = true;

        // Haptic feedback (mobile only)
#if UNITY_ANDROID || UNITY_IOS
        if (vibrateOnExplosion)
            Handheld.Vibrate();
#endif

        if (sfx && explodeSfx)
            sfx.PlayOneShot(explodeSfx);

        if (explosionParticles)
            explosionParticles.Play();

        if (explosionFrames != null && explosionFrames.Length > 0)
        {
            float frameTime = 1f / Mathf.Max(1f, explosionFPS);
            foreach (var frame in explosionFrames)
            {
                SetSprite(frame);
                yield return new WaitForSeconds(frameTime);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        if (autoDamage)
            DoDamage();

        // Short delay before destruction (let SFX/VFX finish)
        Destroy(gameObject, 0.1f);
    }

    void DoDamage()
    {
        Vector3 worldPos = transform.position;
        var hits = Physics2D.OverlapCircleAll(worldPos, damageRadius);
        foreach (var h in hits)
        {
            var obstacle = h.GetComponent<ObstaclesControllerScript>();
            if (obstacle) obstacle.StartToDestroy(Color.red);
        }
    }

    void SetSprite(Sprite s)
    {
        if (uiImg) uiImg.sprite = s;
        if (sr) sr.sprite = s;
    }
}
