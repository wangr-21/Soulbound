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

    [Header("简化游戏结束UI（血条为0专用）")]
    public Image gameOverBlackPanel; // 半透明黑面板
    public TextMeshProUGUI simpleGameOverText; // 提示文本

    [Header("调试")]
    public bool showDebugLogs = false;

    private PlayerSoulController playerSoulController;
    private bool isInitialized = false;
    private bool isGameOver = false;

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
        FindUIReferences();
        ResetUIState();
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 初始化简化游戏结束UI状态
        InitSimpleGameOverUI();

        // 初始鼠标状态设置
        SetInitialMouseState();
    }

    private void SetInitialMouseState()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name == "StartScene" ||
            currentScene.name == "DowlScene" ||
            currentScene.name == "SoldierScene")
        {
            // UI场景：显示鼠标
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (showDebugLogs) Debug.Log($"UIManager初始鼠标状态：{currentScene.name} 场景中鼠标已显示和解锁");
        }
        else
        {
            // 游戏场景：隐藏鼠标
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (showDebugLogs) Debug.Log($"UIManager初始鼠标状态：{currentScene.name} 场景中鼠标已隐藏和锁定");
        }
    }

    // 初始化简化游戏结束UI
    private void InitSimpleGameOverUI()
    {
        if (gameOverBlackPanel != null)
        {
            gameOverBlackPanel.gameObject.SetActive(false);
            gameOverBlackPanel.color = new Color(0, 0, 0, 0.7f); // 半透明黑
        }
        if (simpleGameOverText != null)
        {
            simpleGameOverText.gameObject.SetActive(false);
        }
    }

    private void FindUIReferences()
    {
        if (soulUI == null)
            soulUI = GameObject.Find("SoulUI");
        if (animalUI == null)
            animalUI = GameObject.Find("AnimalUI");

        // 查找简化游戏结束UI
        if (gameOverBlackPanel == null)
            gameOverBlackPanel = GameObject.Find("GameOverBlackPanel")?.GetComponent<Image>();
        if (simpleGameOverText == null)
            simpleGameOverText = GameObject.Find("SimpleGameOverText")?.GetComponent<TextMeshProUGUI>();

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

    private void ResetUIState()
    {
        ShowSoulUI();
        HideAnimalUI();

        // 隐藏简化游戏结束UI
        HideSimpleGameOverUI();

        isGameOver = false;
    }

    // 隐藏简化游戏结束UI
    private void HideSimpleGameOverUI()
    {
        if (gameOverBlackPanel != null)
            gameOverBlackPanel.gameObject.SetActive(false);
        if (simpleGameOverText != null)
            simpleGameOverText.gameObject.SetActive(false);
    }

    // 显示简化游戏结束UI
    public void ShowSimpleGameOver(string message)
    {
        if (isGameOver) return;
        isGameOver = true;

        // 显示半透明黑面板
        if (gameOverBlackPanel != null)
        {
            gameOverBlackPanel.gameObject.SetActive(true);
            gameOverBlackPanel.color = new Color(0, 0, 0, 0.7f);
            gameOverBlackPanel.raycastTarget = true;
            // 确保在最上层
            gameOverBlackPanel.transform.SetAsLastSibling();
        }

        // 显示提示文本
        if (simpleGameOverText != null)
        {
            simpleGameOverText.gameObject.SetActive(true);
            simpleGameOverText.text = message;
            simpleGameOverText.alignment = TextAlignmentOptions.Center;
            simpleGameOverText.fontSize = 36;
            simpleGameOverText.color = Color.white;
            // 确保在面板上层
            simpleGameOverText.transform.SetAsLastSibling();
        }

        Debug.Log($"游戏结束UI显示: {message}");

        // 在显示游戏结束UI时解锁鼠标
        UnlockMouseForUI();
    }

    // 专门为UI解锁鼠标的方法
    private void UnlockMouseForUI()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (showDebugLogs) Debug.Log("UIManager: 游戏结束时UI鼠标已解锁");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        // 如果游戏结束，使用特殊更新逻辑
        if (isGameOver)
        {
            // 游戏结束时的特殊UI更新逻辑
            // 使用Time.unscaledDeltaTime确保即使Time.timeScale=0也能工作
            UpdateGameOverUI();
            return;
        }

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

        if (playerSoulController != null)
        {
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
        }
        else
        {
            playerSoulController = PlayerSoulController.Instance;
            if (playerSoulController == null)
            {
                playerSoulController = FindObjectOfType<PlayerSoulController>();
            }

            if (showDebugLogs && playerSoulController == null)
                Debug.LogWarning("UIManager: 玩家控制器丢失");
        }
    }

    // 新增：专门处理游戏结束时的UI更新
    private void UpdateGameOverUI()
    {
        // 确保鼠标状态正确（即使Time.timeScale=0也要保持）
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"UIManager: 场景加载完成 - {scene.name}");

        ForceHideAllGameOverUI();
        HideSimpleGameOverUI();

        // 关键：确保时间缩放被重置为1
        Time.timeScale = 1f;
        Debug.Log($"UIManager: 场景加载后重置Time.timeScale为1");

        // 根据场景设置鼠标状态
        if (scene.name == "StartScene" ||
            scene.name == "DowlScene" ||
            scene.name == "SoldierScene")
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log($"UIManager: {scene.name} 中鼠标已显示和解锁");
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Debug.Log($"UIManager: {scene.name} 中鼠标已隐藏和锁定");
        }

        FindUIReferences();
        ResetUIState();
        StartCoroutine(DelayedFindPlayerController());
    }

    private void ForceHideAllGameOverUI()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        int hiddenCount = 0;

        foreach (Canvas canvas in allCanvases)
        {
            foreach (Transform child in canvas.transform)
            {
                string childName = child.name.ToLower();
                if (childName.Contains("gameover") || childName.Contains("游戏结束") || childName.Contains("restart"))
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

    private IEnumerator DelayedFindPlayerController()
    {
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

    public void HideAnimalUI()
    {
        if (animalUI != null && animalUI.activeSelf)
        {
            animalUI.SetActive(false);
            if (showDebugLogs) Debug.Log("UIManager: 隐藏动物UI");
        }
    }

    private void UpdateSoulUI()
    {
        if (playerSoulController == null) return;

        if (soulHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(playerSoulController.currentHealth / playerSoulController.maxHealth);
            soulHealthSlider.value = healthPercentage;
        }

        if (soulHealthText != null)
        {
            soulHealthText.text = $"灵魂生命: {Mathf.Ceil(playerSoulController.currentHealth)}/{playerSoulController.maxHealth}";
        }

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

    private void UpdateAnimalUI()
    {
        if (playerSoulController == null || playerSoulController.currentPossessedObject == null)
        {
            if (showDebugLogs) Debug.LogWarning("UIManager: 无法更新动物UI - 玩家没有附身对象");
            return;
        }

        GameObject animal = playerSoulController.currentPossessedObject;

        DeerController deer = animal.GetComponent<DeerController>();
        if (deer != null)
        {
            UpdateDeerUI(deer);
            return;
        }

        FoxController fox = animal.GetComponent<FoxController>();
        if (fox != null)
        {
            UpdateFoxUI(fox);
            return;
        }

        SheepController sheep = animal.GetComponent<SheepController>();
        if (sheep != null)
        {
            UpdateSheepUI(sheep);
            return;
        }

        BirdController bird = animal.GetComponent<BirdController>();
        if (bird != null)
        {
            UpdateBirdUI(bird);
            return;
        }

        if (showDebugLogs) Debug.LogWarning($"UIManager: 不支持的对象类型: {animal.name}");
    }

    private void UpdateDeerUI(DeerController deer)
    {
        if (animalNameText != null)
        {
            animalNameText.text = "鹿";
        }

        if (animalHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(deer.currentHealth / deer.maxHealth);
            animalHealthSlider.value = healthPercentage;

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

        if (animalTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(deer.possessionTimeRemaining / deer.maxPossessionTime);
            animalTimeSlider.value = timePercentage;

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

        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"UIManager: 鹿状态 - 生命: {deer.currentHealth:F0}/{deer.maxHealth}, 时间: {deer.possessionTimeRemaining:F1}s");
        }
    }

    private void UpdateFoxUI(FoxController fox)
    {
        if (animalNameText != null)
        {
            animalNameText.text = "狐狸";
        }

        if (animalHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(fox.currentHealth / fox.maxHealth);
            animalHealthSlider.value = healthPercentage;

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

        if (animalTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(fox.possessionTimeRemaining / fox.maxPossessionTime);
            animalTimeSlider.value = timePercentage;

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

        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"UIManager: 狐狸状态 - 生命: {fox.currentHealth:F0}/{fox.maxHealth}, 时间: {fox.possessionTimeRemaining:F1}s");
        }
    }

    private void UpdateSheepUI(SheepController sheep)
    {
        if (animalNameText != null)
        {
            animalNameText.text = "绵羊";
        }

        if (animalHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(sheep.currentHealth / sheep.maxHealth);
            animalHealthSlider.value = healthPercentage;

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

        if (animalTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(sheep.possessionTimeRemaining / sheep.maxPossessionTime);
            animalTimeSlider.value = timePercentage;

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

        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"UIManager: 绵羊状态 - 生命: {sheep.currentHealth:F0}/{sheep.maxHealth}, 时间: {sheep.possessionTimeRemaining:F1}s");
        }
    }

    private void UpdateBirdUI(BirdController bird)
    {
        if (animalNameText != null)
        {
            animalNameText.text = "鸟";
        }

        if (animalHealthSlider != null)
        {
            float healthPercentage = Mathf.Clamp01(bird.currentHealth / bird.maxHealth);
            animalHealthSlider.value = healthPercentage;

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

        if (animalTimeSlider != null)
        {
            float timePercentage = Mathf.Clamp01(bird.possessionTimeRemaining / bird.maxPossessionTime);
            animalTimeSlider.value = timePercentage;

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

        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"UIManager: 鸟状态 - 生命: {bird.currentHealth:F0}/{bird.maxHealth}, 时间: {bird.possessionTimeRemaining:F1}s");
        }
    }

    // 旧版ShowGameOver方法（保留但不再使用，仅作兼容）
    public void ShowGameOver(string message)
    {
        // 改为调用简化版本
        ShowSimpleGameOver(message);
    }
}