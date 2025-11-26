using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RewardedButtonBinder : MonoBehaviour
{
    void OnEnable()
    {
        // Atrodam RewardedAds arī tad, ja tas ir DontDestroyOnLoad objektā
        var ads = FindFirstObjectByType<RewardedAds>(FindObjectsInactive.Include);
        if (ads == null)
        {
            Debug.LogWarning("[RewardedButtonBinder] RewardedAds not found in scene.");
            return;
        }

        var btn = GetComponent<Button>();
        ads.SetButton(btn);

        Debug.Log("[RewardedButtonBinder] Bound this button to RewardedAds.ShowAd().");
    }
}
