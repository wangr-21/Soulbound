using UnityEngine;

public class ChestController1 : MonoBehaviour
{
    public GameObject puzzlePanel1; // 拼图谜题面板
    private bool isOpened = false; // 是否已打开

    // 新增：距离检测相关变量
    [SerializeField] private float interactionDistance = 3f;
    private Transform playerTransform;
    private bool isPlayerNear = false;

    void Start()
    {
        if (puzzlePanel1 != null) puzzlePanel1.SetActive(false);

        // 新增：获取玩家位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void Update()
    {
        // 新增：检测玩家是否在附近
        CheckPlayerDistance();

        // 修改：触发条件（移除射线检测）
        if (!isOpened && isPlayerNear && Input.GetKeyDown(KeyCode.O))
        {
            OpenPuzzlePanel();
        }
    }

    // 新增：检测玩家距离
    void CheckPlayerDistance()
    {
        if (playerTransform == null) return;
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = distance <= interactionDistance;
    }

    // 打开拼图面板
    public void OpenPuzzlePanel()
    {
        if (!isOpened)
        {
            puzzlePanel1.SetActive(true);
            PuzzleManager1.Instance.StartPuzzle(this);
            isOpened = true;
        }
    }

    public void LockChest1()
    {
        isOpened = true;
    }

    // 新增：显示交互范围（可选）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}