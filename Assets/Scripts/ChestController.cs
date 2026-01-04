using UnityEngine;

public class ChestController : MonoBehaviour
{
    public GameObject puzzlePanel; // 谜题面板
    private bool isOpened = false; // 是否已打开

    // 新增：距离检测相关变量（与日记本逻辑对齐）
    [SerializeField] private float interactionDistance = 3f; // 交互距离
    private Transform playerTransform; // 玩家位置
    private bool isPlayerNear = false; // 玩家是否在附近

    void Start()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);

        // 新增：获取玩家位置（通过标签"Player"，与日记本一致）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void Update()
    {
        // 新增：检测玩家是否在交互范围内
        CheckPlayerDistance();

        // 修改：触发条件改为“未打开 + 玩家在附近 + 按O键”（移除射线检测）
        if (!isOpened && isPlayerNear && Input.GetKeyDown(KeyCode.O))
        {
            OpenPuzzle();
        }
    }

    // 新增：检测玩家与宝箱的距离
    void CheckPlayerDistance()
    {
        if (playerTransform == null) return;
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = distance <= interactionDistance;
    }

    // 打开谜题面板
    public void OpenPuzzle()
    {
        if (!isOpened)
        {
            puzzlePanel.SetActive(true);
            PuzzleManager.Instance.StartPuzzle(this);
            isOpened = true;
        }
    }

    public void LockChest()
    {
        isOpened = true;
    }

    // 新增：场景中显示交互范围（可选，方便调试）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}