using UnityEngine;

public class SceneBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f; // unpause if frozen
    }
}
