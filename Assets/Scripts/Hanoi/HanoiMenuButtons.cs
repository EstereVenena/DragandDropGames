using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hanoi
{
    public class HanoiMenuButtons : MonoBehaviour
    {
        public void StartHanoi()
        {
            // piemērs ar reklāmu starpā:
            // AdManager.Instance.ShowInterstitialThen(() => SceneManager.LoadScene("HanoiScene"));

            SceneManager.LoadScene("HanoiScene");
        }
    }
}
