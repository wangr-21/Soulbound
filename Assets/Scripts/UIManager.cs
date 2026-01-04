using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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

    [Header("按钮反馈效果")]
    public AudioSource buttonClickSound;
    public float buttonClickDelay = 0.3f;

    [Header("调试")]
    public bool showDebugLogs = false;

    private PlayerSoulController playerSoulController;
    private bool isInitialized = false;
    private bool isGameOver = false;

    // 新增：防止重复点击
    private bool isRestarting = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("UIManager: 单例实例创建");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("UIManager: 检测到重复实例，销毁新实例");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 重新查找所有UI引用
        FindUIReferences();

        // 初始化UI状态
        ResetUIState();

        // 绑定重新开始按钮
        SetupRestartButton();

        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// 查找所有UI引用
    /// </summary>
    private void FindUIReferences()
    {
        // 如果引用丢失，尝试重新查找
        if (soulUI == null)
            soulUI = GameObject.Find("SoulUI");
        if (animalUI == null)
            animalUI = GameObject.Find("AnimalUI");
        if (gameOverPanel == null)
            gameOverPanel = GameObject.Find("GameOverPanel");

        // 查找玩家控制器
        playerSoulController = FindObjectOfType<PlayerSoulController>();
        if (playerSoulController == null)
        {
            Debug.LogWarning("UIManager: 未找到PlayerSoulController，将在Update中重试");
        }
        else
        {
            isInitialized = true;
            if (showDebugLogs) Debug.Log("UIManager: 玩家控制器找到");
        }
    }

    /// <summary>
    /// 重置UI状态
    /// </summary>
    private void ResetUIState()
    {
        ShowSoulUI();
        HideAnimalUI();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            isGameOver = false;
        }

        // 重置按钮状态
        if (restartButton != null)
        {
            restartButton.interactable = true;
        }

        // 重置重启状态
        isRestarting = false;
    }

    /// <summary>
    /// 设置重新开始按钮
    /// </summary>
    private void SetupRestartButton()
    {
        if (restartButton != null)
        {
            // 移除所有现有监听器
            restartButton.onClick.RemoveAllListeners();

            // 添加新的监听器
            restartButton.onClick.AddListener(OnRestartButtonClick);

            // 确保按钮可交互
            restartButton.interactable = true;

            Debug.Log("UIManager: 重新开始按钮设置完成");
        }
        else
        {
            Debug.LogError("UIManager: 重新开始按钮未找到！");

            // 尝试查找按钮
            Button[] buttons = FindObjectsOfType<Button>();
            foreach (Button btn in buttons)
            {
                if (btn.name.Contains("Restart") || btn.name.Contains("重新开始"))
                {
                    restartButton = btn;
                    SetupRestartButton();
                    break;
                }
            }
        }
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
            if (playerSoulController == null)
            {
                playerSoulController = FindObjectOfType<PlayerSoulController>();
            }

            if (playerSoulController != null)
            {
                isInitialized = true;
                if (showDebugLogs) Debug.Log("UIManager: 延迟找到玩家控制器");
            }
            return;
        }

        // 如果游戏结束，不更新游戏状态UI
        if (isGameOver) return;

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
            if (playerSoulController == null)
            {
                playerSoulController = FindObjectOfType<PlayerSoulController>();
            }

            if (showDebugLogs && playerSoulController == null)
                Debug.LogWarning("UIManager: 玩家控制器丢失");
        }
    }

    // 在UIManager的OnSceneLoaded方法中添加
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"UIManager: 场景加载完成 - {scene.name}");

        // 立即强制隐藏所有游戏结束相关的UI
        ForceHideAllGameOverUI();

        // 重新查找所有UI引用
        FindUIReferences();

        // 初始化UI状态
        ResetUIState();

        // 绑定重新开始按钮
        SetupRestartButton();

        // 重新查找玩家控制器
        StartCoroutine(DelayedFindPlayerController());

        // 确保时间正常
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 强制隐藏所有游戏结束UI
    /// </summary>
    private void ForceHideAllGameOverUI()
    {
        // 查找所有Canvas
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        int hiddenCount = 0;

        foreach (Canvas canvas in allCanvases)
        {
            // 查找所有子对象
            foreach (Transform child in canvas.transform)
            {
                // 隐藏名称包含"GameOver"、"游戏结束"或"Restart"的对象
                string childName = child.name.ToLower();
                if (childName.Contains("gameover") ||
                    childName.Contains("游戏结束") ||
                    childName.Contains("restart"))
                {
                    if (child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                        hiddenCount++;
                        Debug.Log($"强制隐藏: {child.name}");
                    }
                }
            }
        }

        if (hiddenCount > 0)
        {
            Debug.Log($"强制隐藏了 {hiddenCount} 个游戏结束相关UI");
        }
    }

    /// <summary>
    /// 延迟查找玩家控制器（避免场景未完全加载）
    /// </summary>
    private IEnumerator DelayedFindPlayerController()
    {
        // 等待一帧，让场景中的对象完全初始化
        yield return null;

        playerSoulController = FindObjectOfType<PlayerSoulController>();
        if (playerSoulController == null)
        {
            yield return new WaitForSeconds(0.5f);
            playerSoulController = FindObjectOfType<PlayerSoulController>();
        }

        if (playerSoulController != null)
        {
            isInitialized = true;
            Debug.Log("UIManager: 场景加载后找到玩家控制器");

            // 根据玩家状态显示正确的UI
            if (playerSoulController.isPossessing)
            {
                ShowAnimalUI();
            }
            else
            {
                ShowSoulUI();
            }
        }
        else
        {
            Debug.LogWarning("UIManager: 场景加载后未找到玩家控制器");
        }
    }

    /// <summary>
    /// 显示灵魂UI
    /// </summary>
    public void ShowSoulUI()
    {
        if (soulUI != null)
        {
            soulUI.SetActive(true);
            if (showDebugLogs) Debug.Log("UIManager: 显示灵魂UI");
        }

        if (animalUI != null)
        {
            animalUI.SetActive(false);
        }
    }

    /// <summary>
    /// 显示动物UI
    /// </summary>
    public void ShowAnimalUI()
    {
        if (animalUI != null)
        {
            animalUI.SetActive(true);
            if (showDebugLogs) Debug.Log("UIManager: 显示动物UI");
        }

        if (soulUI != null)
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
        isGameOver = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverText != null)
            {
                gameOverText.text = message;
            }

            if (showDebugLogs) Debug.Log($"UIManager: 显示游戏结束UI - {message}");
        }
        else
        {
            Debug.LogError("UIManager: 游戏结束面板引用丢失！");
            FindUIReferences();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                if (gameOverText != null)
                {
                    gameOverText.text = message;
                }
            }
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

        // 确保重新开始按钮可交互
        if (restartButton != null)
        {
            restartButton.interactable = true;
        }
        else
        {
            SetupRestartButton();
        }

        // 暂停游戏时间（可选）
        // Time.timeScale = 0f;
    }

    /// <summary>
    /// 隐藏游戏结束UI
    /// </summary>
    public void HideGameOver()
    {
        isGameOver = false;

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
        Debug.Log("UIManager: 重新开始按钮被点击");

        // 防止重复点击
        if (isRestarting)
        {
            Debug.LogWarning("UIManager: 已经在重新开始过程中，忽略点击");
            return;
        }

        isRestarting = true;

        // 播放点击音效
        if (buttonClickSound != null)
        {
            buttonClickSound.Play();
        }

        // 添加按钮点击动画效果
        StartCoroutine(ButtonClickEffect());

        // 使用完整的重置流程
        StartCoroutine(CompleteRestartRoutine());
    }

    private IEnumerator CompleteRestartRoutine()
    {
        // 立即隐藏游戏结束面板
        HideGameOver();

        // 显示重置提示
        if (gameOverText != null)
        {
            gameOverText.text = "重置中...";
            gameOverPanel.SetActive(true);
        }

        // 短暂延迟，让玩家看到提示
        yield return new WaitForSeconds(0.5f);

        // 执行重新开始
        RestartGame();
    }

    /// <summary>
    /// 按钮点击效果
    /// </summary>
    private IEnumerator ButtonClickEffect()
    {
        if (restartButton != null)
        {
            // 保存原始颜色
            Color originalColor = restartButton.image.color;

            // 按钮变暗
            restartButton.image.color = new Color(0.8f, 0.8f, 0.8f);

            // 短暂等待
            yield return new WaitForSeconds(0.1f);

            // 恢复颜色
            restartButton.image.color = originalColor;
        }
    }

    /// <summary>
    /// 重新开始游戏 - 确保可靠执行的版本
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("UIManager: 重新开始游戏被调用");

        // 显示加载提示
        ShowLoadingMessage();

        // 确保时间正常
        Time.timeScale = 1f;

        // 重置游戏状态
        isGameOver = false;

        // 重置所有控制器的状态
        ResetAllControllers();

        // 隐藏游戏结束UI
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(false);
        }

        // 获取当前场景信息
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        string currentSceneName = SceneManager.GetActiveScene().name;

        Debug.Log($"UIManager: 准备重新加载场景 - {currentSceneName} (索引: {currentSceneIndex})");

        // 检查场景索引是否有效
        if (currentSceneIndex < 0 || currentSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"UIManager: 无效的场景索引 {currentSceneIndex}");
            SceneManager.LoadScene(currentSceneName);
            return;
        }

        // 使用同步加载确保场景立即加载
        SceneManager.LoadScene(currentSceneIndex);
    }

    /// <summary>
    /// 重置所有控制器的状态
    /// </summary>
    private void ResetAllControllers()
    {
        // 重置玩家灵魂控制器
        if (playerSoulController != null)
        {
            playerSoulController.ResetPlayerState();
        }

        // 重置所有动物控制器
        ResetAnimalControllers();
    }

    /// <summary>
    /// 重置动物控制器
    /// </summary>
    private void ResetAnimalControllers()
    {
        DeerController[] deerControllers = FindObjectsOfType<DeerController>();
        foreach (DeerController deer in deerControllers)
        {
            deer.ResetState();
        }

        FoxController[] foxControllers = FindObjectsOfType<FoxController>();
        foreach (FoxController fox in foxControllers)
        {
            fox.ResetState();
        }

        SheepController[] sheepControllers = FindObjectsOfType<SheepController>();
        foreach (SheepController sheep in sheepControllers)
        {
            sheep.ResetState();
        }

        BirdController[] birdControllers = FindObjectsOfType<BirdController>();
        foreach (BirdController bird in birdControllers)
        {
            bird.ResetState();
        }
    }

    /// <summary>
    /// 显示加载消息
    /// </summary>
    private void ShowLoadingMessage()
    {
        if (gameOverText != null)
        {
            gameOverText.text = "重新开始游戏...";
        }
    }

    /// <summary>
    /// 公开方法，可以在Inspector中直接绑定
    /// </summary>
    public void OnRestartButtonClicked()
    {
        Debug.Log("UIManager: 通过Inspector绑定按钮被点击");
        RestartGame();
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
        Debug.Log($"游戏结束面板: {(gameOverPanel != null ? gameOverPanel.activeSelf.ToString() : "Null")}");
        Debug.Log($"重新开始按钮: {(restartButton != null ? "存在" : "Null")}");
        Debug.Log($"玩家控制器: {playerSoulController != null}");
        Debug.Log($"游戏是否结束: {isGameOver}");
        Debug.Log($"是否正在重新开始: {isRestarting}");
        if (playerSoulController != null)
        {
            Debug.Log($"玩家是否附身: {playerSoulController.isPossessing}");
            Debug.Log($"当前附身对象: {playerSoulController.currentPossessedObject}");
        }
    }

    [ContextMenu("测试：重新开始游戏")]
    public void TestRestartGame()
    {
        Debug.Log("测试：手动调用重新开始");
        RestartGame();
    }

    [ContextMenu("测试：检查按钮状态")]
    public void TestCheckButton()
    {
        if (restartButton == null)
        {
            Debug.LogError("重新开始按钮引用为空！");
            return;
        }

        Debug.Log("=== 按钮状态检查 ===");
        Debug.Log($"按钮名称: {restartButton.name}");
        Debug.Log($"按钮是否可交互: {restartButton.interactable}");
        Debug.Log($"按钮是否激活: {restartButton.gameObject.activeInHierarchy}");

        // 检查按钮的父对象
        if (restartButton.transform.parent != null)
        {
            Debug.Log($"按钮父对象: {restartButton.transform.parent.name}");
            Debug.Log($"父对象是否激活: {restartButton.transform.parent.gameObject.activeInHierarchy}");
        }

        // 检查按钮是否有碰撞器遮挡
        GraphicRaycaster raycaster = restartButton.GetComponentInParent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogWarning("未找到GraphicRaycaster组件，UI点击可能无效");
        }
    }

    [ContextMenu("调试：检查场景构建设置")]
    public void DebugCheckBuildSettings()
    {
        Debug.Log("=== 场景构建设置检查 ===");
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"构建设置中的场景总数: {sceneCount}");

        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"当前场景: {currentScene.name} (索引: {currentScene.buildIndex})");

        bool foundInBuild = false;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log($"场景[{i}]: {sceneName}");

            if (sceneName == currentScene.name)
            {
                foundInBuild = true;
                Debug.Log($"✓ 当前场景在构建设置中，索引: {i}");
            }
        }

        if (!foundInBuild)
        {
            Debug.LogError($"✗ 当前场景 '{currentScene.name}' 不在构建设置中！");
            Debug.LogError("请打开 File -> Build Settings -> Add Open Scenes 添加当前场景");
        }
    }

    [ContextMenu("调试：强制同步加载当前场景")]
    public void DebugForceLoadCurrentScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"强制同步加载场景索引: {sceneIndex}");
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"使用场景名称加载: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}