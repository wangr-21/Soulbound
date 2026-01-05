using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SoldierRecallTrigger : MonoBehaviour
{
    [Header("触发设置")]
    public float triggerRange = 3f; // 触发距离
    public TMP_Text promptText;     // 提示文本（按B键回忆）

    private Transform player;
    private bool isInRange;         // 是否在触发范围内
    private bool isRecalling;       // 是否正在回忆（进入SoldierScene）

    void Start()
    {
        // 初始化隐藏提示文本
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        // 找到玩家物体
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 初始化重置状态，防止残留
        ResetRecallState();
    }

    void Update()
    {
        // 玩家/文本为空 或 已进入回忆场景 → 跳过检测
        if (player == null || promptText == null || isRecalling) return;

        // 计算玩家与触发点的距离
        float distance = Vector3.Distance(transform.position, player.position);

        // 进入触发范围
        if (distance <= triggerRange)
        {
            isInRange = true;
            promptText.gameObject.SetActive(true);
            promptText.text = "按 B 键进入士兵回忆"; // 自定义提示文本

            // 按B键触发跳转
            if (Input.GetKeyDown(KeyCode.B))
            {
                EnterSoldierRecall();
            }
        }
        // 离开触发范围 → 重置状态
        else if (isInRange)
        {
            ResetRecallState();
        }
    }

    // 进入SoldierScene的核心逻辑
    void EnterSoldierRecall()
    {
        isRecalling = true;

        // 显示光标（方便在SoldierScene操作UI）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 重置时间缩放（防止时间异常）
        Time.timeScale = 1f;

        // 淡出背景音乐（和DowlScene逻辑一致）
        MainBGMController.Instance?.FadeOut(0.5f);

        // 通知 PlayerSoulController 进入回忆场景
        if (PlayerSoulController.Instance != null)
        {
            PlayerSoulController.Instance.EnterMemoryScene("SoldierScene");
        }

        // Additive模式加载SoldierScene（不销毁主场景）
        SceneManager.LoadScene("SoldierScene", LoadSceneMode.Additive);
    }

    // 重置回忆状态（关键：返回主界面时调用）
    public void ResetRecallState()
    {
        isInRange = false;
        isRecalling = false;
        // 隐藏提示文本
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
        // 同步重置光标为锁定状态（主界面默认状态）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Gizmos可视化触发范围（方便调试）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // 用黄色区分DowlScene的触发范围
        Gizmos.DrawWireSphere(transform.position, triggerRange);
    }
}