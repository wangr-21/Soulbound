using UnityEngine;
using TMPro;
using System.Collections;

public class BirdUIManager : MonoBehaviour
{
    [Header("UI组件引用")]
    public Canvas birdCanvas;
    public TextMeshProUGUI possessionPrompt;
    public TextMeshProUGUI controlInstructions;

    [Header("显示距离")]
    public float showDistance = 5f;

    [Header("提示文本")]
    [TextArea] public string possessPromptText = "按E进行附身";
    [TextArea] public string controlPromptText = "按住左Shift进行滑翔\n按下E可以脱离附身\n按\"空格\"向上飞\n按Ctrl向下飞";

    [Header("提示显示时间")]
    public float controlPromptShowTime = 5f; // 控制提示显示时间

    private Transform player;
    private BirdController birdController;
    private bool isPlayerNearby = false;

    private Coroutine hideControlPromptCoroutine; // 用于存储隐藏控制提示的协程
    private bool isControlPromptVisible = false; // 控制提示是否可见

    void Start()
    {
        Debug.Log("=== BirdUIManager 开始初始化 ===");

        // 获取组件
        birdController = GetComponent<BirdController>();
        if (birdController == null)
        {
            Debug.LogError("BirdUIManager: 未找到BirdController组件！");
            enabled = false;
            return;
        }
        else
        {
            Debug.Log("BirdUIManager: 找到BirdController组件");
        }

        // 获取玩家
        Debug.Log("查找标签为'Player'的对象...");
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"BirdUIManager: 找到玩家 - {playerObj.name}");
        }
        else
        {
            Debug.LogWarning("BirdUIManager: 未找到标签为Player的对象，尝试其他方法...");

            // 备用方案：查找PlayerSoulController
            PlayerSoulController soulController = FindObjectOfType<PlayerSoulController>();
            if (soulController != null)
            {
                player = soulController.transform;
                Debug.Log($"BirdUIManager: 找到PlayerSoulController - {soulController.gameObject.name}");
            }
            else
            {
                // 查找任何包含"Player"或"Soul"的对象
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Player") || obj.name.Contains("player") ||
                        obj.name.Contains("Soul") || obj.name.Contains("soul"))
                    {
                        player = obj.transform;
                        Debug.Log($"BirdUIManager: 找到玩家相关对象 - {obj.name}");
                        break;
                    }
                }

                if (player == null)
                {
                    Debug.LogError("BirdUIManager: 未找到任何玩家对象！UI将不会显示");
                    enabled = false;
                    return;
                }
            }
        }

        // 确保UI组件存在
        Debug.Log("查找Canvas组件...");
        if (birdCanvas == null)
        {
            birdCanvas = GetComponentInChildren<Canvas>(true);
            if (birdCanvas == null)
            {
                Debug.LogError("BirdUIManager: 未找到Canvas组件！");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log($"BirdUIManager: 找到Canvas - {birdCanvas.name}");
            }
        }

        // 查找TextMeshPro组件
        Debug.Log("查找TextMeshPro组件...");
        TextMeshProUGUI[] texts = birdCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        Debug.Log($"找到 {texts.Length} 个TextMeshPro组件");

        if (texts.Length >= 2)
        {
            possessionPrompt = texts[0];
            controlInstructions = texts[1];
            Debug.Log($"BirdUIManager: 已分配TextMeshPro组件");
        }
        else
        {
            Debug.LogError($"BirdUIManager: TextMeshPro组件不足: {texts.Length}个，需要至少2个");
            enabled = false;
            return;
        }

        // 设置文本
        if (possessionPrompt != null)
        {
            possessionPrompt.text = possessPromptText;
        }

        if (controlInstructions != null)
        {
            controlInstructions.text = controlPromptText;
        }

        // 初始隐藏所有UI
        HideAllUI();
        Debug.Log("BirdUIManager: 初始化完成");
    }

    void Update()
    {
        if (player == null || birdController == null)
        {
            // 尝试重新查找玩家
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
            return;
        }

        // 只在未被附身时才检查距离并显示附身提示
        if (!birdController.isPossessed)
        {
            // 计算距离
            float distance = Vector3.Distance(transform.position, player.position);

            // 如果玩家靠近且未被附身，显示附身提示
            if (distance <= showDistance && !isPlayerNearby)
            {
                ShowPossessionPrompt();
                isPlayerNearby = true;
            }
            // 如果玩家超出显示距离，隐藏附身提示
            else if (distance > showDistance && isPlayerNearby)
            {
                HidePossessionPrompt();
                isPlayerNearby = false;
            }
        }
    }

    public void ShowPossessionPrompt()
    {
        if (possessionPrompt != null && !possessionPrompt.gameObject.activeSelf)
        {
            possessionPrompt.gameObject.SetActive(true);
            Debug.Log("BirdUIManager: 显示附身提示");
        }
    }

    public void ShowControlInstructions()
    {
        if (controlInstructions != null && !isControlPromptVisible)
        {
            // 先取消之前的协程（如果有）
            if (hideControlPromptCoroutine != null)
            {
                StopCoroutine(hideControlPromptCoroutine);
                hideControlPromptCoroutine = null;
            }

            // 显示控制提示
            controlInstructions.gameObject.SetActive(true);
            isControlPromptVisible = true;
            Debug.Log("BirdUIManager: 显示控制提示");

            // 启动协程，5秒后自动隐藏
            hideControlPromptCoroutine = StartCoroutine(HideControlPromptAfterDelay(controlPromptShowTime));
        }

        // 隐藏附身提示
        HidePossessionPrompt();
    }

    private IEnumerator HideControlPromptAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 只隐藏控制提示，不隐藏附身提示
        HideControlInstructions();

        Debug.Log($"BirdUIManager: 控制提示已显示 {delay} 秒，自动隐藏");
    }

    public void HidePossessionPrompt()
    {
        if (possessionPrompt != null && possessionPrompt.gameObject.activeSelf)
        {
            possessionPrompt.gameObject.SetActive(false);
        }
    }

    public void HideControlInstructions()
    {
        if (controlInstructions != null && controlInstructions.gameObject.activeSelf)
        {
            controlInstructions.gameObject.SetActive(false);
            isControlPromptVisible = false;

            // 停止协程
            if (hideControlPromptCoroutine != null)
            {
                StopCoroutine(hideControlPromptCoroutine);
                hideControlPromptCoroutine = null;
            }
        }
    }

    public void HideAllUI()
    {
        HidePossessionPrompt();
        HideControlInstructions();
        isPlayerNearby = false;
    }

    void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示检测范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, showDistance);
    }
}