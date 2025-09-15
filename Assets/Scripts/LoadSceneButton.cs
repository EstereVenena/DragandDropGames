using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "CityScene";

    public void Load()
    {
        Debug.Log($"[LoadSceneButton] Clicked. Requested scene: '{sceneName}'");

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[LoadSceneButton] Scene name is empty.");
            return;
        }

        // Verify the scene is in Build Settings
        if (!IsSceneInBuild(sceneName))
        {
            Debug.LogError($"[LoadSceneButton] Scene '{sceneName}' is NOT in Build Settings. " +
                           $"Add it via File > Build Settings > 'Scenes In Build'.");
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private bool IsSceneInBuild(string name)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var n = System.IO.Path.GetFileNameWithoutExtension(path);
            if (n == name) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    // Optional: avoid typos — drag the scene asset in the Inspector.
    [SerializeField] private UnityEditor.SceneAsset sceneAsset;
    private void OnValidate()
    {
        if (sceneAsset != null) sceneName = sceneAsset.name;
    }
#endif
}
