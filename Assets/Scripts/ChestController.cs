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
        // 检测鼠标点击宝箱
        if (Input.GetMouseButtonDown(0) && !isOpened)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == this.gameObject)
            {
                OpenPuzzle();
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
        }
    }

    // 锁定宝箱（关闭后不再打开）
    public void LockChest()
    {
        isOpened = true;
    }
}