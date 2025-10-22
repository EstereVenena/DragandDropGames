// DragState.cs
// Global helper so other scripts (e.g., BombController) can know if a car is being dragged.

using UnityEngine;

public static class DragState
{
    public static RectTransform Current { get; private set; }
    public static bool IsDragging => Current != null;

    public static void Begin(RectTransform rt)
    {
        Current = rt;
        // Debug.Log($"[DragState] Begin: {rt?.name}");
    }

    public static void End()
    {
        // Debug.Log("[DragState] End");
        Current = null;
    }
}
