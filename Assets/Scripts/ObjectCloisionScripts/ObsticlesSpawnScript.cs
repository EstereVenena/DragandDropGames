using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ObstaclesSpawnScript : MonoBehaviour
{
    [Header("Play Area (RectTransform)")]
    public RectTransform playArea;
    public Transform spawnParentOverride;

    [Header("Prefabs")]
    public GameObject[] cloudsPrefabs;
    public GameObject[] obstaclesPrefabs;
    public GameObject bombPrefab;

    [Header("Spawn Intervals (seconds)")]
    public float cloudSpawnInterval = 3f;
    public float obstacleSpawnInterval = 3f;
    public float bombSpawnInterval = 8f;

    [Header("Speeds (UI px/sec)")]
    public float cloudMinSpeed = 50f;
    public float cloudMaxSpeed = 150f;
    public float obstaclesMinSpeed = 100f;
    public float obstaclesMaxSpeed = 220f;
    public float bombMinSpeed = 60f;
    public float bombMaxSpeed = 120f;

    [Header("Spawn Placement")]
    public float edgeMargin = 80f;
    public bool bombsSpawnInsideArea = true;

    [Header("Two-sided spawn & flipping")]
    public bool spawnFromBothSides = true;
    [Range(0f, 1f)] public float rightSideChance = 0.5f;
    public bool autoFlipVisual = true;
    public string defaultFlipChildName = "Sprite";

    // Track spawned objects
    private List<RectTransform> spawnedObjects = new List<RectTransform>();

    // -------------------- Unity --------------------
    void Awake()
    {
        // Auto-resolve playArea if not assigned
        if (!playArea)
        {
            var sb = FindFirstObjectByType<ScreenBoundriesScript>();
            if (sb && sb.playArea) playArea = sb.playArea;
        }
        if (!playArea) playArea = transform.parent as RectTransform;
        if (!spawnParentOverride && playArea) spawnParentOverride = playArea;

        if (!playArea)
            Debug.LogWarning("[Spawner] ⚠ No playArea assigned (spawning skipped).", this);
    }

    void Start()
    {
        if (!playArea) return;
        InvokeRepeating(nameof(SpawnCloud), 0f, cloudSpawnInterval);
        InvokeRepeating(nameof(SpawnObstacle), 0f, obstacleSpawnInterval);
        InvokeRepeating(nameof(SpawnBomb), 5f, bombSpawnInterval);
    }

    // -------------------- Spawners --------------------
    void SpawnCloud()
    {
        if (cloudsPrefabs == null || cloudsPrefabs.Length == 0) return;
        SpawnMoving(cloudsPrefabs, cloudMinSpeed, cloudMaxSpeed);
    }

    void SpawnObstacle()
    {
        if (obstaclesPrefabs == null || obstaclesPrefabs.Length == 0) return;
        SpawnMoving(obstaclesPrefabs, obstaclesMinSpeed, obstaclesMaxSpeed);
    }

    void SpawnBomb()
    {
        if (!bombPrefab || !playArea) return;

        var rt = InstantiateUI(bombPrefab, out RectTransform itemRT);
        var rect = playArea.rect;

        var img = itemRT.GetComponent<Image>();
        if (img) img.raycastTarget = true;

        if (bombsSpawnInsideArea)
        {
            float x = Random.Range(rect.xMin, rect.xMax);
            float y = Random.Range(rect.yMin, rect.yMax);
            itemRT.anchoredPosition = new Vector2(x, y);

            var bomb = itemRT.GetComponent<BombController>();
            if (bomb) bomb.speed = 0f;

            WireBomb(itemRT);

            if (autoFlipVisual) ApplyFlip(itemRT, moveRight: true);
        }
        else
        {
            bool fromRight = ChooseRightSide();
            float y = Random.Range(rect.yMin, rect.yMax);
            float x = fromRight ? (rect.xMax + edgeMargin) : (rect.xMin - edgeMargin);
            itemRT.anchoredPosition = new Vector2(x, y);

            var bomb = itemRT.GetComponent<BombController>();
            if (bomb)
            {
                float spd = Random.Range(bombMinSpeed, bombMaxSpeed);
                bomb.speed = fromRight ? -spd : +spd;
            }

            WireBomb(itemRT);

            if (autoFlipVisual) ApplyFlip(itemRT, moveRight: !fromRight);
        }
    }

    // -------------------- Helpers --------------------
    void SpawnMoving(GameObject[] prefabs, float minSpeed, float maxSpeed)
    {
        var prefab = prefabs[Random.Range(0, prefabs.Length)];
        var goRT = InstantiateUI(prefab, out RectTransform itemRT);

        var img = goRT.GetComponent<Image>();
        if (img) img.raycastTarget = false;

        var rect = playArea.rect;
        float y = Random.Range(rect.yMin, rect.yMax);

        bool fromRight = ChooseRightSide();
        float startX = fromRight ? (rect.xMax + edgeMargin) : (rect.xMin - edgeMargin);
        itemRT.anchoredPosition = new Vector2(startX, y);

        var ctrl = itemRT.GetComponent<ObstaclesControllerScript>();
        if (ctrl)
        {
            float spd = Random.Range(minSpeed, maxSpeed);
            ctrl.speed = fromRight ? -spd : +spd;
        }

        if (autoFlipVisual)
            ApplyFlip(itemRT, moveRight: !fromRight);
    }

    bool ChooseRightSide() => !spawnFromBothSides ? true : Random.value < rightSideChance;

    RectTransform InstantiateUI(GameObject prefab, out RectTransform itemRT)
    {
        var parent = spawnParentOverride ? spawnParentOverride : (Transform)playArea;
        var go = Instantiate(prefab, parent);

        itemRT = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        itemRT.localScale = Vector3.one;
        itemRT.localRotation = Quaternion.identity;
        itemRT.SetAsFirstSibling();

        spawnedObjects.Add(itemRT);

        return itemRT;
    }

    void WireBomb(RectTransform bombRT)
    {
        var bomb = bombRT.GetComponent<BombController>();
        if (!bomb) return;

        bomb.playArea = playArea;
        bomb.penaltyUI = FindFirstObjectByType<PenaltyCounterUI>();
        bomb.addPenaltyOnClick = false;
        bomb.addPenaltyOnDragOverlap = true;
        bomb.addPenaltyOnTimeout = false;
        bomb.requireVisibleInPlayArea = true;
    }

    void ApplyFlip(RectTransform rt, bool moveRight)
    {
        var cfg = rt.GetComponent<PrefabFacing>();
        bool primaryFacesRight = cfg ? (cfg.primary == PrefabFacing.Direction.Right) : true;

        Transform target = rt;
        string childName = (cfg != null && !string.IsNullOrEmpty(cfg.visualRootName))
                           ? cfg.visualRootName
                           : defaultFlipChildName;

        if (!string.IsNullOrEmpty(childName))
        {
            var child = rt.Find(childName);
            if (child) target = child;
        }

        var s = target.localScale;
        float absX = Mathf.Abs(s.x);

        bool wantPositiveWhenMovingRight = primaryFacesRight;
        bool signPositive = moveRight ? wantPositiveWhenMovingRight : !wantPositiveWhenMovingRight;
        if (cfg && cfg.extraInvert) signPositive = !signPositive;

        s.x = signPositive ? absX : -absX;
        target.localScale = s;
    }

    // -------------------- Public API --------------------
    public void DestroyAllSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
            if (obj) Destroy(obj.gameObject);

        spawnedObjects.Clear();
    }
}
