// Assets/Scripts/GameSetupSpawnRandomize.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameSetupSpawnRandomize : MonoBehaviour
{
    [Header("Parents / Layers")]
    public RectTransform playArea;            // assign Map (RectTransform)
    public Transform silhouettesLayer;        // assign Layer_Silhouettes (child of Map)
    public Transform carsLayer;               // assign Layer_Cars (child of Map)

    [Header("Prefabs (UI roots)")]
    // Use GameObject here — we’ll fetch the RectTransform after instantiation.
    public GameObject[] silhouettePrefabs;    // each must have RectTransform + Image + DropPlaceScript (on root)
    public GameObject[] carPrefabs;           // each must have RectTransform + Image + DragAndDropScript (on root)

    [Header("Layout")]
    public int count = 12;
    public Vector2 padding = new Vector2(60f, 60f);

    readonly List<RectTransform> _spawnedSlots = new();
    readonly List<RectTransform> _spawnedCars  = new();

    void Awake()
    {
        if (!playArea) { Debug.LogError("[Setup] playArea not assigned.", this); return; }
        if (!silhouettesLayer) silhouettesLayer = playArea;
        if (!carsLayer)        carsLayer        = playArea;

        SpawnPairs();
        RandomizePositions(_spawnedSlots);
        RandomizePositions(_spawnedCars);

        // Register cars for progress
        foreach (var car in _spawnedCars)
            ProgressCounter.Instance?.RegisterCar(car.gameObject);
    }

    void SpawnPairs()
    {
        int n = Mathf.Min(count, Mathf.Min(silhouettePrefabs.Length, carPrefabs.Length));
        for (int i = 0; i < n; i++)
        {
            var slotRT = InstantiateUI(silhouettePrefabs[i], silhouettesLayer, $"Silhouette[{i}]");
            var carRT  = InstantiateUI(carPrefabs[i],        carsLayer,        $"Car[{i}]");

            if (!slotRT || !carRT) continue; // skip broken entries

            // keep tags consistent (used by DropPlaceScript)
            carRT.tag  = carPrefabs[i].tag;
            slotRT.tag = silhouettePrefabs[i].tag;

            // hook playArea to cars
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

        // Instantiate as GameObject to avoid InvalidCastException
        GameObject go = Instantiate(prefab, parent);
        go.name = $"{prefab.name} (inst)";

        // Try to get RectTransform on the ROOT
        var rt = go.GetComponent<RectTransform>();
        if (!rt)
        {
            Debug.LogError($"[Setup] Prefab '{prefab.name}' has NO RectTransform on the root. " +
                           $"Make sure the UI root (this object) has RectTransform. GameObject path: {prefab.name}", prefab);
            return null;
        }

        // Reset UI transform
        rt.anchoredPosition3D = Vector3.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        // Ensure it renders in UI (Image on root). If only SpriteRenderer exists, convert.
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

    void RandomizePositions(List<RectTransform> items)
    {
        if (items.Count == 0) return;

        var rect = playArea.rect;
        float xmin = rect.xMin + padding.x;
        float xmax = rect.xMax - padding.x;
        float ymin = rect.yMin + padding.y;
        float ymax = rect.yMax - padding.y;

        foreach (var rt in items)
        {
            if (!rt) continue;
            float x = Random.Range(xmin, xmax);
            float y = Random.Range(ymin, ymax);
            rt.anchoredPosition = new Vector2(x, y);
        }
    }
}
