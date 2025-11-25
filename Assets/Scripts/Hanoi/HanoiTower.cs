using System.Collections.Generic;
using UnityEngine;

public class HanoiTower : MonoBehaviour
{
    // disku kaudze uz šī torņa
    public Stack<HanoiDisk> disks = new Stack<HanoiDisk>();

    [Tooltip("Horizontāla nobīde diskiem (no torņa centra)")]
    public float xOffset = 0f;

    [Tooltip("Vertikālais solis starp diskiem (pikseļi)")]
    public float yStep = 80f;  // pieregulē pēc disku augstuma

    /// <summary>
    /// Vai drīkst likt šo disku uz šī torņa.
    /// </summary>
    public bool CanPlaceDisk(HanoiDisk disk)
    {
        if (disks.Count == 0) return true;
        return disk.size < disks.Peek().size;
    }

    /// <summary>
    /// Uzliek disku uz šī torņa.
    /// countMoveAndCheckWin = false izmanto, kad nevajag +1 gājienu un win pārbaudi
    /// (piem., sākuma izvietojums vai atdošana atpakaļ pēc neveiksmīga drop).
    /// </summary>
    public void PlaceDisk(HanoiDisk disk, bool countMoveAndCheckWin = true)
    {
        // ieliekam stackā
        disks.Push(disk);
        disk.currentTower = this;

        RectTransform towerRt = GetComponent<RectTransform>();
        RectTransform diskRt  = disk.GetComponent<RectTransform>();

        if (towerRt != null && diskRt != null)
        {
            // noliekam kā bērnu šim tornim lokālajās koordinātēs
            disk.transform.SetParent(transform, worldPositionStays: false);

            float towerHeight = towerRt.rect.height;
            float diskHeight  = diskRt.rect.height;

            // apakšējā diska centrs pie torņa pamatnes
            float baseY = -towerHeight * 0.5f + diskHeight * 0.5f;

            // konkrētā diska Y (virs apakšējā)
            float y = baseY + yStep * (disks.Count - 1);

            diskRt.anchoredPosition = new Vector2(xOffset, y);
        }

        if (!countMoveAndCheckWin)
            return;

        if (HanoiGameManager.Instance == null)
            return;

        HanoiGameManager.Instance.RegisterMove();

        if (HanoiGameManager.Instance.IsWin(this))
        {
            HanoiGameManager.Instance.ShowWin();
        }
    }

    /// <summary>
    /// Sākuma izvietojums bez gājienu skaitīšanas.
    /// </summary>
    public void PlaceDiskInitial(HanoiDisk disk)
    {
        PlaceDisk(disk, false);
    }

    public void ResetTower()
    {
        disks.Clear();
    }
}
