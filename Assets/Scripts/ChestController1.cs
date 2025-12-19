using UnityEngine;

public class ChestController1 : MonoBehaviour
{
    public GameObject puzzlePanel1; // 关联拼图面板PuzzlePanel1
    private bool isOpened = false; // 宝箱是否已使用（关闭后不再打开）

    void Start()
    {
        if (puzzlePanel1 != null) puzzlePanel1.SetActive(false);
    }

    void Update()
    {
        // 仅当宝箱未打开时，检测O键输入
        if (!isOpened)
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                // 射线检测：瞄准宝箱才能触发
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.gameObject == this.gameObject)
                    {
                        OpenPuzzlePanel();
                    }
                }
            }
        }
    }

    // 打开拼图面板
    public void OpenPuzzlePanel()
    {
        if (!isOpened)
        {
            puzzlePanel1.SetActive(true);
            PuzzleManager1.Instance.StartPuzzle(this); // 通知拼图管理器启动谜题
            isOpened = true; // 锁定宝箱，避免重复打开
        }
    }

    // 锁定宝箱（完成/超时后调用，不可再打开）
    public void LockChest1()
    {
        isOpened = true;
    }
}