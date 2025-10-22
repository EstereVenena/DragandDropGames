// Assets/Scripts/BombExplosionAnim.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BombExplosionAnim : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite unlitSprite;        // single image
    public Sprite litSprite;          // single image
    public Sprite[] explosionFrames;  // 4 sliced sprites from the sheet

    [Header("Timing")]
    public float fuseSeconds = 2f;    // time from lit to explosion
    public float explosionFPS = 12f;  // how fast to play the 4 frames

    [Header("Audio (optional)")]
    public AudioSource sfx;
    public AudioClip fuseSfx;
    public AudioClip explodeSfx;

    // If your old bomb logic needs a radius:
    public float damageRadius = 220f;

    Image uiImg;               // for UI bombs
    SpriteRenderer sr;         // for world-space bombs
    bool exploded;

    void Awake()
    {
        uiImg = GetComponent<Image>();
        sr    = GetComponent<SpriteRenderer>();

        SetSprite(unlitSprite);
        if (uiImg) uiImg.preserveAspect = true;
    }

    void OnEnable()
    {
        // Start the fuse automatically; or call StartFuse() from your spawner/controller.
        StartCoroutine(FuseRoutine());
    }

    public void StartFuse(float overrideSeconds = -1f)
    {
        if (overrideSeconds > 0f) fuseSeconds = overrideSeconds;
        StopAllCoroutines();
        StartCoroutine(FuseRoutine());
    }

    IEnumerator FuseRoutine()
    {
        // show lit
        SetSprite(litSprite);
        if (sfx && fuseSfx) sfx.PlayOneShot(fuseSfx);

        // wait fuse
        float t = 0f;
        while (t < fuseSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // explode
        yield return StartCoroutine(ExplosionRoutine());
        DoDamage();
        Destroy(gameObject, 0.05f);
    }

    IEnumerator ExplosionRoutine()
    {
        if (exploded) yield break;
        exploded = true;

        if (sfx && explodeSfx) sfx.PlayOneShot(explodeSfx);

        if (explosionFrames != null && explosionFrames.Length > 0)
        {
            float frameTime = 1f / Mathf.Max(1f, explosionFPS);
            for (int i = 0; i < explosionFrames.Length; i++)
            {
                SetSprite(explosionFrames[i]);
                yield return new WaitForSeconds(frameTime);
            }
        }
        else
        {
            // fallback: just keep lit sprite briefly
            yield return new WaitForSeconds(0.2f);
        }
    }

    void DoDamage()
    {
        // If you’re using UI + 2D physics, this still works if the bomb sits at a world position.
        // Adjust as needed for your project.
        Vector3 worldPos = transform.position;
        var hits = Physics2D.OverlapCircleAll(worldPos, damageRadius);
        foreach (var h in hits)
        {
            // Call your obstacle/car destroy logic here.
            var obstacle = h.GetComponent<ObstaclesControllerScript>();
            if (obstacle) obstacle.StartToDestroy(Color.red);
        }
    }

    void SetSprite(Sprite s)
    {
        if (uiImg) uiImg.sprite = s;
        if (sr)    sr.sprite = s;
    }
}
