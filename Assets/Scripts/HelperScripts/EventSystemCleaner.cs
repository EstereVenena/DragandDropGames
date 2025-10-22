// Assets/Scripts/HelperScripts/EventSystemCleaner.cs
// Automatically ensures only one EventSystem exists in the scene.

using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemCleaner : MonoBehaviour
{
    void Awake()
    {
        // Use new API: FindObjectsByType instead of deprecated FindObjectsOfType
        var all = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Exclude,   // ignore inactive ones
            FindObjectsSortMode.None       // no need to sort by instance ID
        );

        if (all.Length > 1)
        {
            Debug.LogWarning($"[Cleaner] Found {all.Length} EventSystems — removing extras!");
            for (int i = 1; i < all.Length; i++)
            {
                if (all[i] != null)
                    Destroy(all[i].gameObject);
            }
        }
        else if (all.Length == 0)
        {
            Debug.LogError("[Cleaner] No EventSystem found! Add one under your Canvas.");
        }
    }
}
