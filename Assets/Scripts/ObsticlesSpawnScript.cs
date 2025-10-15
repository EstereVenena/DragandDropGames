using System.Linq;
using UnityEngine;

public class ObstaclesSpawnScript : MonoBehaviour
{
    [Header("Play Area (RectTransform)")]
    public RectTransform playArea;                // assign in Inspector if you can
    public Transform spawnParentOverride;         // optional; defaults to playArea

    [Header("Prefabs")]
    public GameObject[] cloudsPrefabs;
    public GameObject[] obstaclesPrefabs;
    public GameObject bombPrefab;

    [Header("Spawn Intervals (seconds)")]
    public float cloudSpawnInterval    = 3f;
    public float obstacleSpawnInterval = 3f;
    public float bombSpawnInterval     = 8f;

    [Header("Speeds (UI px/sec)")]
    public float cloudMinSpeed     =  50f;
    public float cloudMaxSpeed     = 150f;
    public float obstaclesMinSpeed = 100f;
    public float obstaclesMaxSpeed = 220f;
    public float bombMinSpeed      =  60f;
    public float bombMaxSpeed      = 120f;

    [Header("Spawn Placement")]
    [Tooltip("Extra pixels beyond panel edge to spawn off-screen smoothly.")]
    public float edgeMargin = 80f;

    [Tooltip("Bombs appear at a random point inside the play area (no sliding).")]
    public bool bombsSpawnInsideArea = true;

    void Awake()
    {
        // 1) Use assigned
        // 2) Use ScreenBoundriesScript.playArea
        // 3) Use GameObject tagged "PlayArea"
        // 4) Use child named "PlayArea"
        // 5) Use largest RectTransform under same Canvas
        // 6) Use parent RectTransform
        if (!playArea)
        {
            var sb = FindFirstObjectByType<ScreenBoundriesScript>(FindObjectsInactive.Exclude);
            if (sb && sb.playArea) playArea = sb.playArea;
        }

        if (!playArea)
        {
            var tagged = GameObject.FindWithTag("PlayArea");
            if (tagged) playArea = tagged.GetComponent<RectTransform>();
        }

        if (!playArea)
        {
            var named = FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                        .FirstOrDefault(rt => rt && rt.name == "PlayArea");
            if (named) playArea = named;
        }

        if (!playArea)
        {
            var myCanvas = GetComponentInParent<Canvas>();
            if (myCanvas)
            {
                // Pick the widest/“largest” panel under this canvas
                var rts = myCanvas.GetComponentsInChildren<RectTransform>(true)
                                  .Where(rt => rt != myCanvas.transform && rt.rect.width > 0 && rt.rect.height > 0);
                playArea = rts.OrderByDescending(rt => rt.rect.width * rt.rect.height).FirstOrDefault();
            }
        }

        if (!playArea)
        {
            // Last resort: parent RectTransform (keeps you from crashing)
            playArea = transform.parent as RectTransform;
        }

        if (!spawnParentOverride && playArea) spawnParentOverride = playArea;

        if (!playArea)
        {
            Debug.LogWarning("[Spawner] ⚠ No playArea assigned (and couldn't auto-link). Spawning will be skipped.", this);
        }
        else
        {
            Debug.Log($"[Spawner] ✅ Using playArea: {playArea.name}", playArea);
        }
    }

    void Start()
    {
        if (!playArea) return;

        InvokeRepeating(nameof(SpawnCloud),     0f,  cloudSpawnInterval);
        InvokeRepeating(nameof(SpawnObstacle),  0f,  obstacleSpawnInterval);
        InvokeRepeating(nameof(SpawnBomb),      5f,  bombSpawnInterval);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!playArea)
        {
            Debug.LogWarning("[Spawner] Assign playArea (RectTransform panel) to avoid runtime null refs.", this);
        }
    }
#endif

    void SpawnCloud()
    {
        if (!playArea || cloudsPrefabs == null || cloudsPrefabs.Length == 0) return;

        var prefab = cloudsPrefabs[Random.Range(0, cloudsPrefabs.Length)];
        var rt = InstantiateUI(prefab, out RectTransform itemRT);

        var rect = playArea.rect;
        float y  = Random.Range(rect.yMin, rect.yMax);
        itemRT.anchoredPosition = new Vector2(rect.xMax + edgeMargin, y);

        var speed = Random.Range(cloudMinSpeed, cloudMaxSpeed);
        var ctrl  = itemRT.GetComponent<ObstaclesControllerScript>();
        if (ctrl) ctrl.speed = speed;
    }

    void SpawnObstacle()
    {
        if (!playArea || obstaclesPrefabs == null || obstaclesPrefabs.Length == 0) return;

        var prefab = obstaclesPrefabs[Random.Range(0, obstaclesPrefabs.Length)];
        var rt = InstantiateUI(prefab, out RectTransform itemRT);

        var rect = playArea.rect;
        float y  = Random.Range(rect.yMin, rect.yMax);
        itemRT.anchoredPosition = new Vector2(rect.xMax + edgeMargin, y);

        var speed = Random.Range(obstaclesMinSpeed, obstaclesMaxSpeed);
        var ctrl  = itemRT.GetComponent<ObstaclesControllerScript>();
        if (ctrl) ctrl.speed = speed;
    }

    void SpawnBomb()
    {
        if (!playArea || bombPrefab == null) return;

        var rt = InstantiateUI(bombPrefab, out RectTransform itemRT);
        var rect = playArea.rect;

        if (bombsSpawnInsideArea)
        {
            float x = Random.Range(rect.xMin, rect.xMax);
            float y = Random.Range(rect.yMin, rect.yMax);
            itemRT.anchoredPosition = new Vector2(x, y);

            var bomb = itemRT.GetComponent<BombController>();
            if (bomb) bomb.speed = 0f;
        }
        else
        {
            float y = Random.Range(rect.yMin, rect.yMax);
            itemRT.anchoredPosition = new Vector2(rect.xMax + edgeMargin, y);

            var bomb = itemRT.GetComponent<BombController>();
            if (bomb) bomb.speed = Random.Range(bombMinSpeed, bombMaxSpeed);
        }
    }

    RectTransform InstantiateUI(GameObject prefab, out RectTransform itemRT)
    {
        var parent = (spawnParentOverride ? spawnParentOverride : (Transform)playArea);
        var go = Instantiate(prefab, parent);
        itemRT = go.GetComponent<RectTransform>();
        if (!itemRT)
        {
            // If a non-UI prefab is dropped by mistake, retrofit a RectTransform
            go.AddComponent<RectTransform>();
            itemRT = go.GetComponent<RectTransform>();
        }

        itemRT.localScale = Vector3.one;
        itemRT.localRotation = Quaternion.identity;
        return itemRT;
    }
}
