using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HanoiGameManager : MonoBehaviour
{
    public static HanoiGameManager Instance;

    public HanoiTower towerA;
    public HanoiTower towerB;
    public HanoiTower towerC;

    public HanoiDisk[] disks;

    public GameObject winPanel;
    public TMP_Text movesText;

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

        for (int i = disks.Length - 1; i >= 0; i--)
        {
            var disk = disks[i];
            disk.transform.SetParent(towerA.transform, false);

            var rt = disk.GetComponent<RectTransform>();
            if (rt != null)
            {
                float towerHeight = ((RectTransform)towerA.transform).rect.height;
                float diskHeight = rt.rect.height;

                float baseY = -towerHeight * 0.5f + diskHeight * 0.5f;
                float y = baseY + towerA.yStep * stackIndex;

                rt.anchoredPosition = new Vector2(towerA.xOffset, y);
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

    public void RestartLevel()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
