using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToTitle : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "TitleScene";

    // Hook this to the Back button's OnClick()
    public void Go()
    {
        Debug.Log($"[BackToTitle] Clicked. Loading '{titleSceneName}'...");
        if (!IsSceneInBuild(titleSceneName))
        {
            Debug.LogError($"[BackToTitle] Scene '{titleSceneName}' is NOT in Build Settings.");
            return;
        }
        SceneManager.LoadScene(titleSceneName, LoadSceneMode.Single);
    }

    // Optional: Esc / Android Back navigates to TitleScene too
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Go();
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
    // Lets you drag the scene asset in Inspector (prevents typos)
    [SerializeField] private UnityEditor.SceneAsset sceneAsset;
    private void OnValidate()
    {
        if (sceneAsset != null) titleSceneName = sceneAsset.name;
    }
#endif
}
