using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HanoiRewardedHint : MonoBehaviour
{
    [Header("Refs")]
    public HanoiGameManager gameManager;   // iemet Inspectorā
    public Button hintButton;              // poga "Hint (ad)"
    public Color highlightColor = Color.yellow;
    public float highlightSeconds = 2f;

    [Header("Optional")]
    [Tooltip("Ja true – poga kļūst neaktīva, kamēr nav ielādēta nākamā reklāma.")]
    public bool disableButtonUntilReload = true;

    // internal
    private Dictionary<HanoiTower, Image> towerImages = new();
    private Dictionary<HanoiTower, Color> originalColors = new();

    void Awake()
    {
        if (!gameManager)
            gameManager = FindFirstObjectByType<HanoiGameManager>(FindObjectsInactive.Exclude);

        if (!hintButton)
            hintButton = GetComponentInChildren<Button>();

        // savācam torņu Image, lai varētu “izcelt”
        if (gameManager)
        {
            RegisterTower(gameManager.towerA);
            RegisterTower(gameManager.towerB);
            RegisterTower(gameManager.towerC);
        }

        if (hintButton)
            hintButton.onClick.AddListener(OnHintClicked);
    }

    void OnEnable()
    {
        RewardedAds.OnRewardClaimed += OnRewarded;
    }

    void OnDisable()
    {
        RewardedAds.OnRewardClaimed -= OnRewarded;
    }

    void RegisterTower(HanoiTower tower)
    {
        if (!tower) return;

        if (!towerImages.ContainsKey(tower))
        {
            var img = tower.GetComponent<Image>();
            if (!img)
                img = tower.gameObject.AddComponent<Image>(); // ja nav, uztaisām paši (vienkārša krāsas plate)

            towerImages[tower] = img;
            originalColors[tower] = img.color;
        }
    }

    // ===================== BUTTON PRESS =====================

    void OnHintClicked()
    {
        if (AdManager.Instance == null || AdManager.Instance.rewardedAds == null)
        {
            Debug.LogWarning("[HanoiRewardedHint] AdManager / RewardedAds not found.");
            return;
        }

        Debug.Log("[HanoiRewardedHint] Hint button → show rewarded ad.");

        AdManager.Instance.rewardedAds.ShowAd();

        if (disableButtonUntilReload && hintButton)
            hintButton.interactable = false;
    }

    // ===================== AFTER AD COMPLETED =====================

    void OnRewarded()
    {
        // Šo eventu šauj VISI rewarded ads, arī citās ainās, bet:
        // ja HanoiGameManager nav aktīvs – vienkārši ignorējam
        if (!gameManager || !gameObject.activeInHierarchy)
            return;

        Debug.Log("[HanoiRewardedHint] Reward received → show hint.");

        ShowHint();

        if (disableButtonUntilReload && hintButton)
        {
            // kādu brīdi vēl disabled; Reload parūpējas RewardedAds pats
            StartCoroutine(ReenableButtonLater(3f));
        }
    }

    IEnumerator ReenableButtonLater(float sec)
    {
        yield return new WaitForSeconds(sec);
        if (hintButton)
            hintButton.interactable = true;
    }

    // ===================== HINT LOĢIKA =====================

    void ShowHint()
    {
        if (gameManager == null || gameManager.disks == null || gameManager.disks.Length == 0)
            return;

        // atrodam MAZĀKO disku, kas šobrīd ir uz kāda torņa
        HanoiDisk smallest = null;
        foreach (var d in gameManager.disks)
        {
            if (!d || d.currentTower == null) continue;
            if (smallest == null || d.size < smallest.size)
                smallest = d;
        }

        if (smallest == null)
        {
            Debug.LogWarning("[HanoiRewardedHint] No disks found on towers.");
            return;
        }

        // Kurus torņus vispār apskatām
        List<HanoiTower> towers = new List<HanoiTower>();
        if (gameManager.towerA) towers.Add(gameManager.towerA);
        if (gameManager.towerB) towers.Add(gameManager.towerB);
        if (gameManager.towerC) towers.Add(gameManager.towerC);

        // Izceļam torņus, uz kuriem mazāko disku DRĪKST likt (pēc CanPlaceDisk),
        // izņemot to torni, uz kura tas jau stāv.
        List<HanoiTower> validTargets = new List<HanoiTower>();
        foreach (var t in towers)
        {
            if (t == null) continue;
            if (t == smallest.currentTower) continue;
            if (t.CanPlaceDisk(smallest))
                validTargets.Add(t);
        }

        if (validTargets.Count == 0)
        {
            Debug.Log("[HanoiRewardedHint] No valid target towers to highlight.");
            return;
        }

        // highlight
        StartCoroutine(HighlightTowers(validTargets));
    }

    IEnumerator HighlightTowers(List<HanoiTower> targets)
    {
        // uzliekam highlightColor
        foreach (var t in targets)
        {
            if (t == null) continue;
            if (!towerImages.TryGetValue(t, out var img) || img == null) continue;

            if (!originalColors.ContainsKey(t))
                originalColors[t] = img.color;

            img.color = highlightColor;
        }

        yield return new WaitForSeconds(highlightSeconds);

        // atgriežam oriģinālās krāsas
        foreach (var t in targets)
        {
            if (t == null) continue;
            if (!towerImages.TryGetValue(t, out var img) || img == null) continue;

            if (originalColors.TryGetValue(t, out var col))
                img.color = col;
        }
    }
}
