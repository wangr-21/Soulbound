using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerUIManager : MonoBehaviour
{
    [Header("UI组件引用")]
    public Canvas playerCanvas;
    public TextMeshProUGUI movementInstructions;

    [Header("显示设置")]
    [TextArea] public string movementText = "WASD 前后左右移动\n空格键 跳跃";
    public float showDistance = 5f; // 用于调试，实际玩家UI通常不需要距离检测
    public float hideAfterSeconds = 10f; // 多少秒后自动隐藏

    private PlayerSoulController playerController;
    private bool isInstructionsVisible = false;
    private Coroutine hideCoroutine;

    void Start()
    {
        Debug.Log("=== PlayerUIManager 开始初始化 ===");

        // 获取玩家控制器
        playerController = GetComponent<PlayerSoulController>();
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerSoulController>();
        }

        if (playerController == null)
        {
            Debug.LogError("PlayerUIManager: 未找到PlayerSoulController组件！");
            enabled = false;
            return;
        }

        // 查找Canvas组件
        if (playerCanvas == null)
        {
            playerCanvas = GetComponentInChildren<Canvas>(true);
            if (playerCanvas == null)
            {
                Debug.LogError("PlayerUIManager: 未找到Canvas组件！");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"PlayerUIManager: 找到Canvas - {playerCanvas.name}");
            }
        }

        // 查找TextMeshPro组件
        if (movementInstructions == null)
        {
            movementInstructions = playerCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
            if (movementInstructions == null)
            {
                Debug.LogError("PlayerUIManager: 未找到TextMeshPro组件！");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"PlayerUIManager: 找到TextMeshPro组件 - {movementInstructions.name}");
            }
        }

        // 设置文本
        movementInstructions.text = movementText;

        // 初始隐藏UI
        HideInstructions();

        // 延迟显示（等玩家进入游戏后）
        StartCoroutine(ShowInstructionsAfterDelay(2f));

        Debug.Log("PlayerUIManager: 初始化完成");
    }

    void Update()
    {
        // 可选：当玩家移动或跳跃时隐藏提示
        if (isInstructionsVisible)
        {
            // 检查是否有输入，有输入就立即隐藏
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                HideInstructions();
            }
        }

        // 可选：按Tab键切换提示显示
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInstructions();
        }
    }

    private IEnumerator ShowInstructionsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowInstructions();
    }

    public void ShowInstructions()
    {
        if (movementInstructions != null && !isInstructionsVisible)
        {
            movementInstructions.gameObject.SetActive(true);
            isInstructionsVisible = true;
            Debug.Log("PlayerUIManager: 显示移动提示");

            // 启动自动隐藏协程
            if (hideAfterSeconds > 0)
            {
                if (hideCoroutine != null) StopCoroutine(hideCoroutine);
                hideCoroutine = StartCoroutine(HideAfterDelay(hideAfterSeconds));
            }
        }
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInstructions();
    }

    public void HideInstructions()
    {
        if (movementInstructions != null && isInstructionsVisible)
        {
            movementInstructions.gameObject.SetActive(false);
            isInstructionsVisible = false;

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            Debug.Log("PlayerUIManager: 隐藏移动提示");
        }
    }

    public void ToggleInstructions()
    {
        if (isInstructionsVisible)
        {
            HideInstructions();
        }
        else
        {
            ShowInstructions();
        }
    }
}