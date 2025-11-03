// ObstaclesSpawnScript.cs (final corrected)
// - Obstacles keep raycasts OFF (so they don’t block drags).
// - Bombs have raycasts ON (so they can detect clicks or drag overlap).
// - Only drag-over adds penalties (click explosions do NOT add penalties).
// - Auto-wires PlayArea + PenaltyCounterUI at runtime.

using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ObstaclesSpawnScript : MonoBehaviour
{
    [Header("Play Area (RectTransform)")]
    public RectTransform playArea;                // Assign if possible; will auto-resolve if null
    public Transform spawnParentOverride;         // Optional; defaults to playArea

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

    [Header("Two-sided spawn & flipping")]
    public bool spawnFromBothSides = true;                // true = left or right spawn
    [Range(0f,1f)] public float rightSideChance = 0.5f;   // Bias toward right side
    public bool autoFlipVisual = true;                    // Mirror visuals to face travel

    [Tooltip("If PrefabFacing.visualRootName is empty, we try this name. If still not found, flip the root.")]
    public string defaultFlipChildName = "Sprite";

    // -------------------- Unity --------------------

    void Awake()
    {
        // Try to resolve playArea if not assigned
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
                var rts = myCanvas.GetComponentsInChildren<RectTransform>(true)
                                  .Where(rt => rt != myCanvas.transform && rt.rect.width > 0 && rt.rect.height > 0);
                playArea = rts.OrderByDescending(rt => rt.rect.width * rt.rect.height).FirstOrDefault();
            }
        }
        if (!playArea) playArea = transform.parent as RectTransform; // last resort
        if (!spawnParentOverride && playArea) spawnParentOverride = playArea;

        if (!playArea)
            Debug.LogWarning("[Spawner] ⚠ No playArea assigned (and couldn't auto-link). Spawning will be skipped.", this);
        else
            Debug.Log($"[Spawner] ✅ Using playArea: {playArea.name}", playArea);
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
            Debug.LogWarning("[Spawner] Assign playArea (RectTransform panel) to avoid runtime null refs.", this);
    }
#endif

    // -------------------- Spawners --------------------

    void SpawnCloud()
    {
        if (!playArea || cloudsPrefabs == null || cloudsPrefabs.Length == 0) return;
        SpawnMoving(cloudsPrefabs, cloudMinSpeed, cloudMaxSpeed);
    }

    void SpawnObstacle()
    {
        if (!playArea || obstaclesPrefabs == null || obstaclesPrefabs.Length == 0) return;
        SpawnMoving(obstaclesPrefabs, obstaclesMinSpeed, obstaclesMaxSpeed);
    }

    void SpawnBomb()
    {
        if (!playArea || bombPrefab == null) return;

        var rt = InstantiateUI(bombPrefab, out RectTransform itemRT);
        var rect = playArea.rect;

        // Bomb MUST be clickable for OnPointerClick to fire
        var img = itemRT.GetComponent<Image>();
        if (img) img.raycastTarget = true;

        if (bombsSpawnInsideArea)
        {
            float x = Random.Range(rect.xMin, rect.xMax);
            float y = Random.Range(rect.yMin, rect.yMax);
            itemRT.anchoredPosition = new Vector2(x, y);

            var bomb = itemRT.GetComponent<BombController>();
            if (bomb) bomb.speed = 0f;

            // Wire references even for stationary bombs
            WireBomb(itemRT);

            if (autoFlipVisual) ApplyFlip(itemRT, moveRight: true); // arbitrary when stationary
        }
        else
        {
            bool fromRight = ChooseRightSide(); // true => spawn right, move left
            float y = Random.Range(rect.yMin, rect.yMax);
            float x = fromRight ? (rect.xMax + edgeMargin) : (rect.xMin - edgeMargin);
            itemRT.anchoredPosition = new Vector2(x, y);

            var bomb = itemRT.GetComponent<BombController>();
            if (bomb)
            {
                float spd = Random.Range(bombMinSpeed, bombMaxSpeed);
                bomb.speed = fromRight ? -spd : +spd;
            }

            // Wire references for moving bombs
            WireBomb(itemRT);

            if (autoFlipVisual) ApplyFlip(itemRT, moveRight: !fromRight);
        }
    }

    // -------------------- Helpers --------------------

    void SpawnMoving(GameObject[] prefabs, float minSpeed, float maxSpeed)
    {
        var prefab = prefabs[Random.Range(0, prefabs.Length)];
        var goRT = InstantiateUI(prefab, out RectTransform itemRT);

        // Obstacles should NOT block input:
        var img = goRT.GetComponent<Image>();
        if (img) img.raycastTarget = false;

        var rect = playArea.rect;
        float y  = Random.Range(rect.yMin, rect.yMax);

        bool fromRight = ChooseRightSide(); // true => spawn right, move left
        float startX = fromRight ? (rect.xMax + edgeMargin) : (rect.xMin - edgeMargin);
        itemRT.anchoredPosition = new Vector2(startX, y);

        var ctrl = itemRT.GetComponent<ObstaclesControllerScript>();
        if (ctrl)
        {
            float spd = Random.Range(minSpeed, maxSpeed);
            ctrl.speed = fromRight ? -spd : +spd; // sign matches travel direction
        }

        if (autoFlipVisual)
            ApplyFlip(itemRT, moveRight: !fromRight);
    }

    bool ChooseRightSide()
    {
        if (!spawnFromBothSides) return true; // keep legacy behavior: from right only
        return Random.value < rightSideChance; // true = right, false = left
    }

    RectTransform InstantiateUI(GameObject prefab, out RectTransform itemRT)
    {
        var parent = (spawnParentOverride ? spawnParentOverride : (Transform)playArea);
        var go = Instantiate(prefab, parent);

        // Ensure UI RectTransform & sane defaults
        itemRT = go.GetComponent<RectTransform>();
        if (!itemRT) itemRT = go.AddComponent<RectTransform>();
        itemRT.localScale = Vector3.one;
        itemRT.localRotation = Quaternion.identity;

        // Keep obstacles behind draggables
        itemRT.SetAsFirstSibling();

        // NOTE: do NOT touch Image.raycastTarget here; set it per-type.
        return itemRT;
    }

    // Wires BombController refs so you don't have to drag them on the prefab.
    void WireBomb(RectTransform bombRT)
    {
        var bomb = bombRT.GetComponent<BombController>();
        if (!bomb) return;

        bomb.playArea  = playArea; // for inside-area check
        bomb.penaltyUI = FindFirstObjectByType<PenaltyCounterUI>(FindObjectsInactive.Exclude);

        // ✅ penalties only for DRAG overlap
        bomb.addPenaltyOnClick        = false;
        bomb.addPenaltyOnDragOverlap  = true;
        bomb.addPenaltyOnTimeout      = false;
        bomb.requireVisibleInPlayArea = true;

        Debug.Log($"[Spawner] Bomb wired → playArea={playArea?.name}, penaltyUI={(bomb.penaltyUI ? "OK" : "NULL")}");
    }

    // Flip visuals based on prefab's declared primary facing and travel direction
    void ApplyFlip(RectTransform rt, bool moveRight)
    {
        // 1) Read prefab config (defaults to primary Right if missing)
        var cfg = rt.GetComponent<PrefabFacing>();
        bool primaryFacesRight = cfg ? (cfg.primary == PrefabFacing.Direction.Right) : true;

        // 2) Choose which transform to flip (child visual root or fallback)
        Transform target = rt;
        string childName = (cfg != null && !string.IsNullOrEmpty(cfg.visualRootName))
                           ? cfg.visualRootName
                           : defaultFlipChildName;

        if (!string.IsNullOrEmpty(childName))
        {
            var child = rt.Find(childName);
            if (child) target = child;
        }

        // 3) Normalize and apply sign
        var s = target.localScale;
        float absX = Mathf.Abs(s.x);

        // If prefab naturally faces Right, we want +absX when moving right.
        // If prefab naturally faces Left, we want -absX when moving right.
        bool wantPositiveWhenMovingRight = primaryFacesRight; // true => + when right, - when left
        bool signPositive = moveRight ? wantPositiveWhenMovingRight : !wantPositiveWhenMovingRight;

        // Optional extra inversion for quirky assets
        if (cfg && cfg.extraInvert) signPositive = !signPositive;

        s.x = signPositive ? absX : -absX;
        target.localScale = s;
    }
}
