using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RecallTrigger : MonoBehaviour
{
    [Header("触发设置")]
    public float triggerRange = 3f;
    public TMP_Text promptText;

    private Transform player;
    private bool isInRange;
    private bool isRecalling;

    void Start()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 确保游戏启动时状态正确
        ResetRecallState();
    }

    void OnEnable()
    {
        // 监听场景卸载事件
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void Update()
    {
        // 如果正在回忆中，不执行范围检测
        if (isRecalling) return;

        if (player == null || promptText == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= triggerRange)
        {
            if (!isInRange)
            {
                isInRange = true;
                promptText.gameObject.SetActive(true);
                promptText.text = "按 B 键进入回忆";
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                EnterRecall();
            }
        }
        else if (isInRange)
        {
            isInRange = false;
            promptText.gameObject.SetActive(false);
        }
    }

    void EnterRecall()
    {
        isRecalling = true;

        // 立即隐藏提示
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 恢复时间
        Time.timeScale = 1f;

        // 淡出主场景BGM
        MainBGMController.Instance?.FadeOut(0.5f);

        // 添加式加载回忆场景
        SceneManager.LoadScene("DowlScene", LoadSceneMode.Additive);
    }

    // 重置回忆状态
    public void ResetRecallState()
    {
        isRecalling = false;
        isInRange = false;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    // 场景卸载事件处理
    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == "DowlScene")
        {
            ResetRecallState();

            // 重新查找玩家，确保引用有效
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRange);
    }
}