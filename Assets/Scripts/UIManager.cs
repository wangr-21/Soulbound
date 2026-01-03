using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("灵魂状态UI")]
    public GameObject soulUI;
    public Slider soulHealthSlider;
    public TextMeshProUGUI soulHealthText;
    public Slider soulTimeSlider;
    public TextMeshProUGUI soulTimeText;

    [Header("动物状态UI")]
    public GameObject animalUI;
    public Slider animalHealthSlider;
    public TextMeshProUGUI animalHealthText;
    public Slider animalTimeSlider;
    public TextMeshProUGUI animalTimeText;
    public TextMeshProUGUI animalNameText;

    [Header("游戏结束UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;

    [Header("调试")]
    public bool showDebugLogs = false;

    private PlayerSoulController playerSoulController;
    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 初始化UI状态
        ShowSoulUI();
        HideAnimalUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // 查找玩家控制器
        playerSoulController = PlayerSoulController.Instance;
        if (playerSoulController == null)
        {
            Debug.LogWarning("UIManager: 未找到PlayerSoulController，将在Update中重试");
        }
        else
        {
            isInitialized = true;
            if (showDebugLogs) Debug.Log("UIManager: 玩家控制器找到");
        }

        // 绑定重新开始按钮
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClick);
        }

        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // 移除场景事件监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        // 如果未初始化，尝试查找玩家控制器
        if (!isInitialized)
        {
            playerSoulController = PlayerSoulController.Instance;
            if (playerSoulController != null)
            {
                isInitialized = true;
                if (showDebugLogs) Debug.Log("UIManager: 延迟找到玩家控制器");
            }
            return;
        }

        // 根据玩家状态更新UI
        if (playerSoulController != null)
        {
            if (playerSoulController.isPossessing && playerSoulController.currentPossessedObject != null)
            {
                // 玩家附身在动物上，显示动物UI
                ShowAnimalUI();

                // 更新动物UI
                UpdateAnimalUI();
            }
            else
            {
                // 玩家处于灵魂状态，显示灵魂UI
                ShowSoulUI();

                // 更新灵魂UI
                UpdateSoulUI();
            }
        }
        else
        {
            // 如果玩家控制器丢失，尝试重新查找
            playerSoulController = PlayerSoulController.Instance;
            if (showDebugLogs && playerSoulController == null)
                Debug.LogWarning("UIManager: 玩家控制器丢失");
        }
    }

    /// <summary>
    /// 场景加载事件处理
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重新查找玩家控制器
        playerSoulController = PlayerSoulController.Instance;
        if (playerSoulController != null)
        {
            isInitialized = true;
            if (showDebugLogs) Debug.Log($"UIManager: 场景{scene.name}加载后重新找到玩家控制器");
        }

        // 确保UI状态正确
        if (playerSoulController != null && playerSoulController.isPossessing)
        {
            ShowAnimalUI();
        }
        else
        {
            ShowSoulUI();
        }

        // 隐藏游戏结束UI
        HideGameOver();
    }

    /// <summary>
    /// 显示灵魂UI
    /// </summary>
    public void ShowSoulUI()
    {
        if (soulUI != null && !soulUI.activeSelf)
        {
            soulUI.SetActive(true);
            if (showDebugLogs) Debug.Log("UIManager: 显示灵魂UI");
        }

        if (animalUI != null && animalUI.activeSelf)
        {
            animalUI.SetActive(false);
        }
    }

    /// <summary>
    /// 显示动物UI
    /// </summary>
    public void ShowAnimalUI()
    {
        if (animalUI != null && !animalUI.activeSelf)
        {
            animalUI.SetActive(true);
            if (showDebugLogs) Debug.Log("UIManager: 显示动物UI");
        }

        if (soulUI != null && soulUI.activeSelf)
        {
            soulUI.SetActive(false);
        }
    }

    /// <summary>
    /// 隐藏动物UI
    /// </summary>
    public void HideAnimalUI()
    {
        if (animalUI != null && animalUI.activeSelf)
        {
            animalUI.SetActive(false);
            if (showDebugLogs) Debug.Log("UIManager: 隐藏动物UI");
        }
    }

    /// <summary>
    /// 更新灵魂UI
    /// </summary>
    private void UpdateSoulUI()
    {
        if (playerSoulController == null) return;

        // 更新生命值
        if (soulHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(playerSoulController.currentHealth / playerSoulController.maxHealth);
            soulHealthSlider.value = healthPercentage;
        }

        if (soulHealthText != null)
        {
            soulHealthText.text = $"灵魂生命: {Mathf.Ceil(playerSoulController.currentHealth)}/{playerSoulController.maxHealth}";
        }

        // 更新时间
        if (soulTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(playerSoulController.soulTimeRemaining / playerSoulController.maxSoulTime);
            soulTimeSlider.value = timePercentage;
        }

        if (soulTimeText != null)
        {
            int remainingSeconds = Mathf.CeilToInt(playerSoulController.soulTimeRemaining);
            soulTimeText.text = $"灵魂时间: {remainingSeconds}秒";
        }
    }

    /// <summary>
    /// 更新动物UI
    /// </summary>
    private void UpdateAnimalUI()
    {
        if (playerSoulController == null || playerSoulController.currentPossessedObject == null)
        {
            if (showDebugLogs) Debug.LogWarning("UIManager: 无法更新动物UI - 玩家没有附身对象");
            return;
        }

        GameObject animal = playerSoulController.currentPossessedObject;

        // 尝试获取鹿控制器
        DeerController deer = animal.GetComponent<DeerController>();
        if (deer != null)
        {
            UpdateDeerUI(deer);
            return;
        }

        // 尝试获取狐狸控制器
        FoxController fox = animal.GetComponent<FoxController>();
        if (fox != null)
        {
            UpdateFoxUI(fox);
            return;
        }

        // 尝试获取绵羊控制器
        SheepController sheep = animal.GetComponent<SheepController>();
        if (sheep != null)
        {
            UpdateSheepUI(sheep);
            return;
        }

        // 尝试获取鸟控制器
        BirdController bird = animal.GetComponent<BirdController>();
        if (bird != null)
        {
            UpdateBirdUI(bird);
            return;
        }

        // 如果没有找到支持的动物控制器
        if (showDebugLogs) Debug.LogWarning($"UIManager: 不支持的对象类型: {animal.name}");
    }

    /// <summary>
    /// 更新鹿UI
    /// </summary>
    private void UpdateDeerUI(DeerController deer)
    {
        // 更新动物名称
        if (animalNameText != null)
        {
            animalNameText.text = "鹿";
        }

        // 更新生命值
        if (animalHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(deer.currentHealth / deer.maxHealth);
            animalHealthSlider.value = healthPercentage;

            // 颜色渐变（可选）：生命值低于30%时变红
            if (healthPercentage <= 0.3f)
            {
                animalHealthSlider.fillRect.GetComponent<Image>().color = Color.red;
            }
            else
            {
                animalHealthSlider.fillRect.GetComponent<Image>().color = Color.green;
            }
        }

        if (animalHealthText != null)
        {
            animalHealthText.text = $"生命值: {Mathf.Ceil(deer.currentHealth)}/{deer.maxHealth}";
        }

        // 更新时间
        if (animalTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(deer.possessionTimeRemaining / deer.maxPossessionTime);
            animalTimeSlider.value = timePercentage;

            // 颜色渐变（可选）：时间低于20%时变黄
            if (timePercentage <= 0.2f)
            {
                animalTimeSlider.fillRect.GetComponent<Image>().color = Color.yellow;
            }
            else
            {
                animalTimeSlider.fillRect.GetComponent<Image>().color = Color.blue;
            }
        }

        if (animalTimeText != null)
        {
            int remainingSeconds = Mathf.CeilToInt(deer.possessionTimeRemaining);
            animalTimeText.text = $"附身时间: {remainingSeconds}秒";
        }

        // 调试信息
        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"UIManager: 鹿状态 - 生命: {deer.currentHealth:F0}/{deer.maxHealth}, 时间: {deer.possessionTimeRemaining:F1}s");
        }
    }

    /// <summary>
    /// 更新狐狸UI
    /// </summary>
    private void UpdateFoxUI(FoxController fox)
    {
        // 更新动物名称
        if (animalNameText != null)
        {
            animalNameText.text = "狐狸";
        }

        // 更新生命值
        if (animalHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(fox.currentHealth / fox.maxHealth);
            animalHealthSlider.value = healthPercentage;

            // 颜色渐变（可选）：生命值低于30%时变红
            if (healthPercentage <= 0.3f)
            {
                animalHealthSlider.fillRect.GetComponent<Image>().color = Color.red;
            }
            else
            {
                animalHealthSlider.fillRect.GetComponent<Image>().color = Color.green;
            }
        }

        if (animalHealthText != null)
        {
            animalHealthText.text = $"生命值: {Mathf.Ceil(fox.currentHealth)}/{fox.maxHealth}";
        }

        // 更新时间
        if (animalTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(fox.possessionTimeRemaining / fox.maxPossessionTime);
            animalTimeSlider.value = timePercentage;

            // 颜色渐变（可选）：时间低于20%时变黄
            if (timePercentage <= 0.2f)
            {
                animalTimeSlider.fillRect.GetComponent<Image>().color = Color.yellow;
            }
            else
            {
                animalTimeSlider.fillRect.GetComponent<Image>().color = Color.blue;
            }
        }

        if (animalTimeText != null)
        {
            int remainingSeconds = Mathf.CeilToInt(fox.possessionTimeRemaining);
            animalTimeText.text = $"附身时间: {remainingSeconds}秒";
        }

        // 调试信息
        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"UIManager: 狐狸状态 - 生命: {fox.currentHealth:F0}/{fox.maxHealth}, 时间: {fox.possessionTimeRemaining:F1}s");
        }
    }

    /// <summary>
    /// 更新绵羊UI
    /// </summary>
    private void UpdateSheepUI(SheepController sheep)
    {
        // 更新动物名称
        if (animalNameText != null)
        {
            animalNameText.text = "绵羊";
        }

        // 更新生命值
        if (animalHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(sheep.currentHealth / sheep.maxHealth);
            animalHealthSlider.value = healthPercentage;

            // 颜色渐变（可选）：生命值低于30%时变红
            if (healthPercentage <= 0.3f)
            {
                animalHealthSlider.fillRect.GetComponent<Image>().color = Color.red;
            }
            else
            {
                animalHealthSlider.fillRect.GetComponent<Image>().color = Color.green;
            }
        }

        if (animalHealthText != null)
        {
            animalHealthText.text = $"生命值: {Mathf.Ceil(sheep.currentHealth)}/{sheep.maxHealth}";
        }

        // 更新时间
        if (animalTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(sheep.possessionTimeRemaining / sheep.maxPossessionTime);
            animalTimeSlider.value = timePercentage;

            // 颜色渐变（可选）：时间低于20%时变黄
            if (timePercentage <= 0.2f)
            {
                animalTimeSlider.fillRect.GetComponent<Image>().color = Color.yellow;
            }
            else
            {
                animalTimeSlider.fillRect.GetComponent<Image>().color = Color.blue;
            }
        }

        if (animalTimeText != null)
        {
            int remainingSeconds = Mathf.CeilToInt(sheep.possessionTimeRemaining);
            animalTimeText.text = $"附身时间: {remainingSeconds}秒";
        }

        // 调试信息
        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"UIManager: 绵羊状态 - 生命: {sheep.currentHealth:F0}/{sheep.maxHealth}, 时间: {sheep.possessionTimeRemaining:F1}s");
        }
    }

    /// <summary>
    /// 更新鸟UI
    /// </summary>
    private void UpdateBirdUI(BirdController bird)
    {
        // 更新动物名称
        if (animalNameText != null)
        {
            animalNameText.text = "鸟";
        }

        // 更新生命值
        if (animalHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(bird.currentHealth / bird.maxHealth);
            animalHealthSlider.value = healthPercentage;

            // 颜色渐变（可选）：生命值低于30%时变红
            if (healthPercentage <= 0.3f)
            {
                animalHealthSlider.fillRect.GetComponent<Image>().color = Color.red;
            }
            else
            {
                animalHealthSlider.fillRect.GetComponent<Image>().color = Color.green;
            }
        }

        if (animalHealthText != null)
        {
            animalHealthText.text = $"生命值: {Mathf.Ceil(bird.currentHealth)}/{bird.maxHealth}";
        }

        // 更新时间
        if (animalTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(bird.possessionTimeRemaining / bird.maxPossessionTime);
            animalTimeSlider.value = timePercentage;

            // 颜色渐变（可选）：时间低于20%时变黄
            if (timePercentage <= 0.2f)
            {
                animalTimeSlider.fillRect.GetComponent<Image>().color = Color.yellow;
            }
            else
            {
                animalTimeSlider.fillRect.GetComponent<Image>().color = Color.blue;
            }
        }

        if (animalTimeText != null)
        {
            int remainingSeconds = Mathf.CeilToInt(bird.possessionTimeRemaining);
            animalTimeText.text = $"附身时间: {remainingSeconds}秒";
        }

        // 调试信息
        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"UIManager: 鸟状态 - 生命: {bird.currentHealth:F0}/{bird.maxHealth}, 时间: {bird.possessionTimeRemaining:F1}s");
        }
    }

    /// <summary>
    /// 显示游戏结束UI
    /// </summary>
    public void ShowGameOver(string message)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverText != null)
            {
                gameOverText.text = message;
            }

            if (showDebugLogs) Debug.Log($"UIManager: 显示游戏结束UI - {message}");
        }

        // 隐藏其他UI
        if (soulUI != null && soulUI.activeSelf)
        {
            soulUI.SetActive(false);
        }
        if (animalUI != null && animalUI.activeSelf)
        {
            animalUI.SetActive(false);
        }

        // 暂停游戏时间（可选）
        // Time.timeScale = 0f;
    }

    /// <summary>
    /// 隐藏游戏结束UI
    /// </summary>
    public void HideGameOver()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(false);
            if (showDebugLogs) Debug.Log("UIManager: 隐藏游戏结束UI");
        }

        // 恢复游戏时间（如果之前暂停了）
        // Time.timeScale = 1f;
    }

    /// <summary>
    /// 重新开始按钮点击事件
    /// </summary>
    private void OnRestartButtonClick()
    {
        // 恢复游戏时间（如果之前暂停了）
        Time.timeScale = 1f;

        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (showDebugLogs) Debug.Log("UIManager: 重新开始游戏");
    }

    /// <summary>
    /// 显示伤害数字（可选）
    /// </summary>
    public void ShowDamageText(Vector3 worldPosition, float damage)
    {
        // 这里可以添加伤害数字显示逻辑
        // 比如实例化一个伤害数字预制体
        // 示例：
        // GameObject damageTextPrefab = Resources.Load<GameObject>("DamageText");
        // if (damageTextPrefab != null)
        // {
        //     GameObject damageText = Instantiate(damageTextPrefab, worldPosition, Quaternion.identity);
        //     damageText.GetComponent<TextMeshPro>().text = damage.ToString("F0");
        // }
    }

    /// <summary>
    /// 强制刷新UI（供外部调用）
    /// </summary>
    public void ForceRefreshUI()
    {
        if (playerSoulController == null) return;

        if (playerSoulController.isPossessing && playerSoulController.currentPossessedObject != null)
        {
            ShowAnimalUI();
            UpdateAnimalUI();
        }
        else
        {
            ShowSoulUI();
            UpdateSoulUI();
        }

        if (showDebugLogs) Debug.Log("UIManager: 强制刷新UI");
    }

    /// <summary>
    /// 设置调试模式
    /// </summary>
    public void SetDebugMode(bool enabled)
    {
        showDebugLogs = enabled;
        Debug.Log($"UIManager: 调试模式 {(enabled ? "启用" : "禁用")}");
    }

    // ===== 调试方法 =====
    [ContextMenu("测试：切换UI")]
    public void TestToggleUI()
    {
        if (soulUI != null && soulUI.activeSelf)
        {
            ShowAnimalUI();
            Debug.Log("测试：切换到动物UI");
        }
        else
        {
            ShowSoulUI();
            Debug.Log("测试：切换到灵魂UI");
        }
    }

    [ContextMenu("测试：显示游戏结束")]
    public void TestShowGameOver()
    {
        ShowGameOver("测试：游戏结束");
    }

    [ContextMenu("测试：隐藏游戏结束")]
    public void TestHideGameOver()
    {
        HideGameOver();
    }

    [ContextMenu("测试：检查UI状态")]
    public void TestCheckUIState()
    {
        Debug.Log($"=== UI状态检查 ===");
        Debug.Log($"灵魂UI: {(soulUI != null ? soulUI.activeSelf.ToString() : "Null")}");
        Debug.Log($"动物UI: {(animalUI != null ? animalUI.activeSelf.ToString() : "Null")}");
        Debug.Log($"玩家控制器: {playerSoulController != null}");
        if (playerSoulController != null)
        {
            Debug.Log($"玩家是否附身: {playerSoulController.isPossessing}");
            Debug.Log($"当前附身对象: {playerSoulController.currentPossessedObject}");
        }
    }
}