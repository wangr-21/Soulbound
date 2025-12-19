using UnityEngine;

public class ChestController : MonoBehaviour
{
    public GameObject puzzlePanel; // 关联谜题面板
    private bool isOpened = false; // 标记宝箱是否已使用（关闭后不再打开）

    void Start()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
    }

    void Update()
    {
        // 仅当宝箱未打开时，才允许检测输入
        if (!isOpened)
        {
            // 核心逻辑：按O键 + 射线命中当前宝箱 → 打开谜题
            if (Input.GetKeyDown(KeyCode.O))
            {
                // 从主相机发射射线（瞄准鼠标位置/屏幕中心，保持原有瞄准逻辑）
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // 确认射线命中的是当前宝箱的Collider
                    if (hit.collider.gameObject == this.gameObject)
                    {
                        OpenPuzzle();
                    }
                }
            }
        }
    }

    // 打开谜题面板
    public void OpenPuzzle()
    {
        if (!isOpened)
        {
            puzzlePanel.SetActive(true);
            PuzzleManager.Instance.StartPuzzle(this);
            isOpened = true; // 标记为已打开，防止重复触发
        }
    }

    // 锁定宝箱（关闭后不再打开）
    public void LockChest()
    {
        isOpened = true;
    }
}