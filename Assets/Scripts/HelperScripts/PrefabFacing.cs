// PrefabFacing.cs
using UnityEngine;

public class PrefabFacing : MonoBehaviour
{
    public enum Direction { Right, Left }

    [Header("How this prefab faces when its speed is positive")]
    public Direction primary = Direction.Right;

    [Header("Optional: flip only this child (e.g., 'Sprite')")]
    public string visualRootName = "Sprite"; // empty = flip root

    [Tooltip("If your art is odd, invert the result after logic.")]
    public bool extraInvert = false;
}
