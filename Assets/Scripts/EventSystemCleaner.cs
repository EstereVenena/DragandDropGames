using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemCleaner : MonoBehaviour
{
    void Awake()
    {
        var all = FindObjectsOfType<EventSystem>();
        if (all.Length > 1)
        {
            Debug.LogWarning($"[Cleaner] Found {all.Length} EventSystems — removing extras!");
            for (int i = 1; i < all.Length; i++) Destroy(all[i].gameObject);
        }
        if (all.Length == 0) Debug.LogError("[Cleaner] No EventSystem found!");
    }
}
