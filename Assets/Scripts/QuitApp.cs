using UnityEngine;

public class QuitApp : MonoBehaviour
{
    // Call this from your button's OnClick
    public void Quit()
    {
#if UNITY_EDITOR
        // Stops play mode in the Editor
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // WebGL apps can't programmatically close the tab.
        Debug.Log("Quit requested on WebGL. Ask the user to close the tab.");
#else
        Application.Quit();           // Quits on Windows/macOS/Linux/Android/iOS
#endif
    }

    // Optional: also quit on Esc/Back
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Quit();
    }
}
