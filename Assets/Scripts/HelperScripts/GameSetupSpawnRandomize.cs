// Assets/Scripts/GameSetupSpawnRandomize.cs
// Purpose:
// 1) Spawn matching pairs (silhouette + car) under given layers.
// 2) Randomize appearance (uniform scale, rotation, optional mirror).
// 3) Place items with collision-free spacing inside playArea.
// 4) Respect "no-spawn" RectTransforms (e.g., Back button, Score panel).
// 5) Safety-clamp scales after randomize and dump final transforms for debugging.

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameSetupSpawnRandomize : MonoBehaviour
{
    // ============================ Assign in Inspector ============================
    [Header("Parents / Layers")]
    [Tooltip("UI RectTransform that defines the playable map area (local space for placement).")]
    public RectTransform playArea;            // Map (RectTransform)
    [Tooltip("Layer to hold silhouettes (child of Map).")]
    public Transform silhouettesLayer;        // Layer_Silhouettes
    [Tooltip("Layer to hold cars (child of Map).")]
    public Transform carsLayer;               // Layer_Cars

    [Header("Prefabs (UI roots)")]
    [Tooltip("Each prefab must have RectTransform + Image + DropPlaceScript on root.")]
    public GameObject[] silhouettePrefabs;
    [Tooltip("Each prefab must have RectTransform + Image + DragAndDropScript on root.")]
    public GameObject[] carPrefabs;

    [Header("Layout")]
    [Tooltip("How many pairs to spawn (clamped to available prefabs).")]
    public int count = 12;
    [Tooltip("Inner margin inside playArea for spawning (left/right = x, bottom/top = y).")]
    public Vector2 padding = new Vector2(60f, 60f);

    // ============================ Randomization Controls ============================
    [Header("Randomize: Silhouettes")]
    public bool randomizeSilhouettePosition = true;
    public bool randomizeSilhouetteScale = true;
    public Vector2 silhouetteScaleRange = new Vector2(0.5f, 2.0f); // uniform scale (abs)
    public bool randomizeSilhouetteRotation = true;
    public float silhouetteMaxRotationDeg = 60f;                    // picks -max..+max
    public bool allowSilhouetteMirrorX = true;
    public bool allowSilhouetteMirrorY = false;
    [Range(0f,1f)] public float silhouetteMirrorXChance = 0.5f;
    [Range(0f,1f)] public float silhouetteMirrorYChance = 0.0f;

    [Header("Randomize: Cars")]
    public bool randomizeCarPosition = true;
    public bool randomizeCarScale = true;
    public Vector2 carScaleRange = new Vector2(0.5f, 2.0f);
    public bool randomizeCarRotation = true;
    public float carMaxRotationDeg = 60f;                            // picks -max..+max
    public bool allowCarMirrorX = true;
    public bool allowCarMirrorY = false;
    [Range(0f,1f)] public float carMirrorXChance = 0.5f;
    [Range(0f,1f)] public float carMirrorYChance = 0.0f;

    // ============================ Spacing / Forbidden ============================
    [Header("Spawn Collision Avoidance")]
    [Tooltip("Minimum center-to-center spacing between spawned items (playArea local units/pixels).")]
    public float minSpacing = 120f;
    [Tooltip("Extra cushion around each item when testing spacing (helps with rotation/scale).")]
    public float spacingInflate = 20f;
    [Tooltip("How many random attempts per item before relaxing spacing.")]
    public int maxTriesPerItem = 200;

    [Header("No-Spawn Zones (RectTransforms in UI)")]
    [Tooltip("Areas where spawning is forbidden (e.g., Back button, Score/time HUD). Size/position in editor.")]
    public List<RectTransform> noSpawnZones = new List<RectTransform>();

    // ============================ Safety / Debug ============================
    [Header("Safety Clamps")]
    [Tooltip("Hard lower/upper bounds enforced AFTER randomization.")]
    public float clampScaleMin = 0.5f;
    public float clampScaleMax = 2.0f;

    [Tooltip("When ON, logs final scale/rot per spawned item so you can spot outliers.")]
    public bool debugLogFinalTransforms = true;

    // ============================ Internals ============================
    readonly List<RectTransform> _spawnedSlots = new();
    readonly List<RectTransform> _spawnedCars  = new();

    // --------------------------- Lifecycle ---------------------------
    void Awake()
    {
        if (!playArea) { Debug.LogError("[Setup] playArea not assigned.", this); enabled = false; return; }
        if (!silhouettesLayer) silhouettesLayer = playArea;
        if (!carsLayer)        carsLayer        = playArea;
    }

    void Start()
    {
        // Make sure all UI rects (including forbidden zones) have correct sizes/positions.
        Canvas.ForceUpdateCanvases();

        SpawnPairs();

        // Randomize appearance FIRST so spacing uses real scaled sizes.
        RandomizeAppearance(
            _spawnedSlots,
            randomizeSilhouetteScale,  silhouetteScaleRange,
            randomizeSilhouetteRotation, silhouetteMaxRotationDeg,
            allowSilhouetteMirrorX, silhouetteMirrorXChance,
            allowSilhouetteMirrorY, silhouetteMirrorYChance
        );

        RandomizeAppearance(
            _spawnedCars,
            randomizeCarScale,  carScaleRange,
            randomizeCarRotation, carMaxRotationDeg,
            allowCarMirrorX, carMirrorXChance,
            allowCarMirrorY, carMirrorYChance
        );

        // 🔒 Safety clamps on scale after randomization (catches rogue 0.1)
        ClampListScales(_spawnedSlots, clampScaleMin, clampScaleMax);
        ClampListScales(_spawnedCars,  clampScaleMin, clampScaleMax);

        // Then place with spacing + forbidden zones respected.
        if (randomizeSilhouettePosition) RandomizePositionsWithSpacing(_spawnedSlots);
        if (randomizeCarPosition)        RandomizePositionsWithSpacing(_spawnedCars);

        // Normalize any snapAnchor localScale to the slot scale (prevents tiny anchors)
        foreach (var slotRT in _spawnedSlots)
        {
            var slot = slotRT ? slotRT.GetComponent<DropPlaceScript>() : null;
            if (slot && slot.snapAnchor)
            {
                // keep mirror signs from anchor, but match magnitude to slot
                float sx = Mathf.Sign(slot.snapAnchor.localScale.x == 0 ? 1f : slot.snapAnchor.localScale.x);
                float sy = Mathf.Sign(slot.snapAnchor.localScale.y == 0 ? 1f : slot.snapAnchor.localScale.y);
                float mag = Mathf.Max(Mathf.Abs(slotRT.localScale.x), Mathf.Abs(slotRT.localScale.y));
                slot.snapAnchor.localScale = new Vector3(sx * mag, sy * mag, 1f);
            }
        }

        // Register cars for progress (if you use a ProgressCounter singleton)
        foreach (var car in _spawnedCars)
            ProgressCounter.Instance?.RegisterCar(car.gameObject);

        // 📣 Debug dump (see what actually ended up in scene)
        DebugDump(_spawnedSlots, "SLOTS");
        DebugDump(_spawnedCars,  "CARS");
    }

    // --------------------------- Spawning ---------------------------
    void SpawnPairs()
    {
        int n = Mathf.Min(count, Mathf.Min(silhouettePrefabs.Length, carPrefabs.Length));
        for (int i = 0; i < n; i++)
        {
            var slotRT = InstantiateUI(silhouettePrefabs[i], silhouettesLayer, $"Silhouette[{i}]");
            var carRT  = InstantiateUI(carPrefabs[i],        carsLayer,        $"Car[{i}]");
            if (!slotRT || !carRT) continue;

            // keep tags consistent (used by DropPlaceScript)
            carRT.tag  = carPrefabs[i].tag;
            slotRT.tag = silhouettePrefabs[i].tag;

            // 👉 make this silhouette accept this exact car tag
            var slot = slotRT.GetComponent<DropPlaceScript>();
            if (slot) slot.requiredTag = carRT.tag;   // <-- important glue

            // clamp drag to play area
            var drag = carRT.GetComponent<DragAndDropScript>();
            if (drag) drag.playArea = playArea;

            _spawnedSlots.Add(slotRT);
            _spawnedCars.Add(carRT);
        }
    }

    RectTransform InstantiateUI(GameObject prefab, Transform parent, string dbgName)
    {
        if (!prefab)
        {
            Debug.LogWarning($"[Setup] Missing prefab entry for {dbgName}.", this);
            return null;
        }

        // Instantiate as GameObject (avoids InvalidCastException if prefab root isn’t a RectTransform).
        GameObject go = Instantiate(prefab, parent);
        go.name = $"{prefab.name} (inst)";

        // Expect a RectTransform on the ROOT.
        var rt = go.GetComponent<RectTransform>();
        if (!rt)
        {
            Debug.LogError($"[Setup] Prefab '{prefab.name}' has NO RectTransform on the root.", prefab);
            return null;
        }

        // Reset UI transform baseline.
        rt.anchoredPosition3D = Vector3.zero;
        rt.localRotation      = Quaternion.identity;
        rt.localScale         = Vector3.one;

        // Ensure it renders as UI (Image on root). If SpriteRenderer exists, convert it.
        var img = rt.GetComponent<Image>();
        if (!img)
        {
            var sr = rt.GetComponent<SpriteRenderer>();
            if (sr && sr.sprite)
            {
                img = rt.gameObject.AddComponent<Image>();
                img.sprite = sr.sprite;
                img.preserveAspect = true;
                Destroy(sr);
                Debug.LogWarning($"[Setup] '{prefab.name}' used SpriteRenderer; converted to UI Image.", rt);
            }
            else
            {
                Debug.LogWarning($"[Setup] '{prefab.name}' has neither Image nor SpriteRenderer on root — may be invisible.", rt);
            }
        }

        return rt;
    }

    // --------------------------- Positioning with spacing + forbidden ---------------------------
    void RandomizePositionsWithSpacing(List<RectTransform> items)
    {
        if (items.Count == 0) return;

        // Work in playArea local coordinates.
        var rect = playArea.rect;
        float xmin = rect.xMin + padding.x;
        float xmax = rect.xMax - padding.x;
        float ymin = rect.yMin + padding.y;
        float ymax = rect.yMax - padding.y;

        // Build forbidden rectangles in playArea local space once.
        var forbidden = BuildForbiddenRectsInPlayArea();

        // For spacing checks: remember centers we placed already.
        List<Vector2> placedCenters = new List<Vector2>(items.Count);

        foreach (var rt in items)
        {
            if (!rt) { placedCenters.Add(Vector2.zero); continue; }

            // Estimate item radius for spacing: half of the larger side * scale + inflate.
            float magX = Mathf.Abs(rt.localScale.x);
            float magY = Mathf.Abs(rt.localScale.y);
            var size   = rt.rect.size;
            float radius = 0.5f * Mathf.Max(size.x * magX, size.y * magY) + spacingInflate;

            bool placed  = false;

            // Try strict spacing first.
            for (int attempt = 0; attempt < Mathf.Max(1, maxTriesPerItem); attempt++)
            {
                Vector2 candidate = new Vector2(Random.Range(xmin, xmax), Random.Range(ymin, ymax));
                if (PointInsideAnyRect(candidate, forbidden)) continue;

                bool overlaps = false;
                for (int j = 0; j < placedCenters.Count; j++)
                {
                    float d = Vector2.Distance(candidate, placedCenters[j]);
                    if (d < minSpacing + radius) { overlaps = true; break; }
                }
                if (overlaps) continue;

                rt.anchoredPosition = candidate;
                placedCenters.Add(candidate);
                placed = true;
                break;
            }

            // If that failed, relax the spacing a bit.
            if (!placed)
            {
                for (int extra = 0; extra < 60; extra++)
                {
                    Vector2 candidate = new Vector2(Random.Range(xmin, xmax), Random.Range(ymin, ymax));
                    if (PointInsideAnyRect(candidate, forbidden)) continue;

                    bool overlaps = false;
                    for (int j = 0; j < placedCenters.Count; j++)
                    {
                        float d = Vector2.Distance(candidate, placedCenters[j]);
                        if (d < 0.5f * minSpacing + radius) { overlaps = true; break; }
                    }
                    if (overlaps) continue;

                    rt.anchoredPosition = candidate;
                    placedCenters.Add(candidate);
                    placed = true;
                    break;
                }
            }

            // Absolute last resort: place anywhere valid outside forbidden.
            if (!placed)
            {
                int safety = 200;
                while (safety-- > 0)
                {
                    Vector2 candidate = new Vector2(Random.Range(xmin, xmax), Random.Range(ymin, ymax));
                    if (!PointInsideAnyRect(candidate, forbidden))
                    {
                        rt.anchoredPosition = candidate;
                        placedCenters.Add(candidate);
                        break;
                    }
                }
            }
        }
    }

    // --------------------------- Helpers ---------------------------
    // Convert each noSpawnZone RectTransform into an AABB Rect in playArea local space.
    List<Rect> BuildForbiddenRectsInPlayArea()
    {
        List<Rect> list = new List<Rect>();
        if (noSpawnZones == null || noSpawnZones.Count == 0) return list;

        foreach (var zone in noSpawnZones)
        {
            if (!zone) continue;

            Vector3[] wc = new Vector3[4];
            zone.GetWorldCorners(wc);

            // Convert world corners to playArea local space.
            for (int i = 0; i < 4; i++)
                wc[i] = playArea.InverseTransformPoint(wc[i]);

            float minX = Mathf.Min(Mathf.Min(wc[0].x, wc[1].x), Mathf.Min(wc[2].x, wc[3].x));
            float maxX = Mathf.Max(Mathf.Max(wc[0].x, wc[1].x), Mathf.Max(wc[2].x, wc[3].x));
            float minY = Mathf.Min(Mathf.Min(wc[0].y, wc[1].y), Mathf.Min(wc[2].y, wc[3].y));
            float maxY = Mathf.Max(Mathf.Max(wc[0].y, wc[1].y), Mathf.Max(wc[2].y, wc[3].y));

            list.Add(Rect.MinMaxRect(minX, minY, maxX, maxY));
        }
        return list;
    }

    // Simple point-in-rect test.
    bool PointInsideAnyRect(Vector2 p, List<Rect> rects)
    {
        for (int i = 0; i < rects.Count; i++)
            if (rects[i].Contains(p)) return true;
        return false;
    }

    // --------------------------- Appearance Randomization ---------------------------
    void RandomizeAppearance(
        List<RectTransform> items,
        bool doScale, Vector2 scaleRange,
        bool doRotation, float maxRotationDeg,
        bool mirrorX, float mirrorXChance,
        bool mirrorY, float mirrorYChance
    )
    {
        // Normalize range and prevent zero
        float sMin = Mathf.Min(scaleRange.x, scaleRange.y);
        float sMax = Mathf.Max(scaleRange.x, scaleRange.y);
        sMin = Mathf.Max(0.0001f, sMin);

        foreach (var rt in items)
        {
            if (!rt) continue;

            // Random uniform scale magnitude
            float magnitude = doScale ? Random.Range(sMin, sMax) : 1f;

            // Optional mirror flips (negative scale)
            float signX = (mirrorX && Random.value < mirrorXChance) ? -1f : 1f;
            float signY = (mirrorY && Random.value < mirrorYChance) ? -1f : 1f;

            rt.localScale = new Vector3(signX * magnitude, signY * magnitude, 1f);

            // Random Z rotation (within ±maxRotationDeg)
            if (doRotation && maxRotationDeg > 0f)
            {
                float z = Random.Range(-maxRotationDeg, maxRotationDeg);
                rt.localRotation = Quaternion.Euler(0f, 0f, z);
            }
        }
    }

    // --------------------------- Safety / Debug Helpers ---------------------------
    void ClampUniformLocalScale(RectTransform rt, float minMag, float maxMag)
    {
        if (!rt) return;
        // Preserve mirror signs, clamp magnitude uniformly.
        float sx = Mathf.Sign(rt.localScale.x == 0 ? 1f : rt.localScale.x);
        float sy = Mathf.Sign(rt.localScale.y == 0 ? 1f : rt.localScale.y);
        float mag = Mathf.Max(Mathf.Abs(rt.localScale.x), Mathf.Abs(rt.localScale.y)); // use larger axis as "magnitude"
        mag = Mathf.Clamp(mag, minMag, maxMag);
        rt.localScale = new Vector3(sx * mag, sy * mag, 1f);
    }

    void ClampListScales(List<RectTransform> list, float minMag, float maxMag)
    {
        foreach (var rt in list) ClampUniformLocalScale(rt, minMag, maxMag);
    }

    void DebugDump(List<RectTransform> list, string label)
    {
        if (!debugLogFinalTransforms) return;
        foreach (var rt in list)
        {
            if (!rt) continue;
            var ls = rt.localScale;
            var lr = rt.localEulerAngles.z;
            Debug.Log($"[Setup][{label}] {rt.name} scale=({ls.x:F3},{ls.y:F3}) rotZ={lr:F1}", rt);
        }
    }

    // ============================ Scene View Debug (optional) ============================
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!playArea || noSpawnZones == null) return;
        UnityEditor.Handles.color = new Color(1f, 0.3f, 0.3f, 0.25f);

        foreach (var z in noSpawnZones)
        {
            if (!z) continue;
            Vector3[] wc = new Vector3[4];
            z.GetWorldCorners(wc);
            UnityEditor.Handles.DrawAAConvexPolygon(wc[0], wc[1], wc[2], wc[3]); // translucent quad
        }
    }
#endif
}
