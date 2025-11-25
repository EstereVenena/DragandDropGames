using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HanoiGameManager : MonoBehaviour
{
    public static HanoiGameManager Instance;

    [Header("Towers")]
    public HanoiTower towerA;
    public HanoiTower towerB;
    public HanoiTower towerC;

    [Header("Disks")]
    public HanoiDisk[] disks;

    [Header("UI")]
    public GameObject winPanel;
    public TMP_Text movesText;

    [Header("Game Settings")]
    public int totalDisks = 3;

    private int moves = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        totalDisks = disks.Length;
        SetupInitialState();
        HideWinPanel();
    }

    private void SetupInitialState()
    {
        moves = 0;
        UpdateMovesText();

        towerA.ResetTower();
        towerB.ResetTower();
        towerC.ResetTower();

        int stackIndex = 0;

        // Liekam visus diskus uz TowerA
        for (int i = disks.Length - 1; i >= 0; i--)
        {
            HanoiDisk disk = disks[i];

            disk.transform.SetParent(towerA.transform);

            var rt = disk.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(
                    towerA.xOffset,
                    towerA.yStep * stackIndex
                );
            }

            towerA.disks.Push(disk);
            disk.currentTower = towerA;

            stackIndex++;
        }
    }

    public void RegisterMove()
    {
        moves++;
        UpdateMovesText();
    }

    private void UpdateMovesText()
    {
        if (movesText != null)
            movesText.text = $"Kustības: {moves}";
    }

    public bool IsWin(HanoiTower tower)
    {
        return tower.disks.Count == totalDisks;
    }

    public void ShowWin()
    {
        if (winPanel != null)
            winPanel.SetActive(true);
    }

    public void HideWinPanel()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    // ===== BUTTON EVENTS =====

    public void RestartLevel()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("TitleScene"); /// <- Ieliec šeit savu titula scēnas nosaukumu!
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
