using System.Collections.Generic;
using UnityEngine;

public class HanoiTower : MonoBehaviour
{
    public Stack<HanoiDisk> disks = new Stack<HanoiDisk>();

    public float xOffset = 0f;
    public float yStep = 80f;

    public bool CanPlaceDisk(HanoiDisk disk)
    {
        if (disks.Count == 0) return true;
        return disk.size < disks.Peek().size;
    }

    public void PlaceDisk(HanoiDisk disk, bool countMoveAndCheckWin = true)
    {
        disks.Push(disk);
        disk.currentTower = this;

        RectTransform towerRt = GetComponent<RectTransform>();
        RectTransform diskRt = disk.GetComponent<RectTransform>();

        if (towerRt != null && diskRt != null)
        {
            disk.transform.SetParent(transform, false);

            float towerHeight = towerRt.rect.height;
            float diskHeight = diskRt.rect.height;

            float baseY = -towerHeight * 0.5f + diskHeight * 0.5f;
            float y = baseY + yStep * (disks.Count - 1);

            diskRt.anchoredPosition = new Vector2(xOffset, y);
        }

        if (!countMoveAndCheckWin)
            return;

        if (HanoiGameManager.Instance == null)
            return;

        HanoiGameManager.Instance.RegisterMove();

        if (HanoiGameManager.Instance.IsWin(this))
            HanoiGameManager.Instance.ShowWin();
    }

    public void PlaceDiskInitial(HanoiDisk disk)
    {
        PlaceDisk(disk, false);
    }

    public void ResetTower()
    {
        disks.Clear();
    }
}
