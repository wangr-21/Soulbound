using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerSoulController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float possessionRange = 5f;

    [Header("跳跃设置")]
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    [Header("粒子系统引用")]
    public ParticleSystem soulParticles;
    public SoulAppearanceController soulAppearance;

    [Header("附身设置")]
    public LayerMask possessableLayerMask = -1;

    [Header("灵魂时间限制")]
    public float maxSoulTime = 60f; // 灵魂最大存活时间
    public float soulTimeRemaining = 0f; // 剩余灵魂时间
    private bool isSoulTimerActive = true; // 灵魂计时器是否激活

    [Header("生命值系统")]
    public float maxHealth = 100f; // 最大生命值
    public float currentHealth; // 当前生命值

    [Header("净化者伤害")]
    public float purifierDamageAmount = 20f; // 每次被净化者攻击的伤害
    public float purifierAttackCooldown = 2f; // 净化者攻击冷却时间
    private float lastPurifierAttackTime = 0f; // 上次被净化者攻击的时间

    [Header("UI管理")]
    public PlayerUIManager playerUIManager;

    // 移动和跳跃相关变量
    private Vector3 playerVelocity;
    private bool isGrounded;
    private CharacterController characterController;
    private PlayerInputActions playerInputActions;

    // 输入相关变量
    private Vector2 currentMovementInput;
    private bool jumpTriggered = false;

    // 附身相关变量
    public GameObject currentPossessedObject;
    public bool isPossessing = false;
    public IPossessable currentPossessable;

    // 视角切换
    private CameraController cameraController;

    // 新增：调试信息
    [Header("调试")]
    public bool debugMode = true;

    // 新增：位置锁定
    private Vector3 deerPositionBeforePossession;
    private Quaternion deerRotationBeforePossession;

    // 修改：简化回忆场景标志
    private bool isInMemoryScene = false;

    // 新增：位置保存变量
    private Vector3 positionBeforeMemoryScene;
    private Quaternion rotationBeforeMemoryScene;
    private bool hasMemoryPositionSaved = false;

    // 单例模式
    public static PlayerSoulController Instance;

    // UI管理器引用
    private UIManager uiManager;

    // 新增：游戏状态
    private bool isGameOver = false;

    // 新增：用于重置的初始值
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        // 设置单例 - 修改：不设置为DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            // 不设置DontDestroyOnLoad，让场景重新加载时自动重置
            // DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            Debug.Log("灵魂: 自动添加CharacterController组件");
        }

        playerInputActions = new PlayerInputActions();
    }

    private void Start()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
            if (debugMode) Debug.Log("相机控制器找到并设置目标");
        }
        else
        {
            Debug.LogError("未找到相机控制器！请确保主相机上有 CameraController 组件");
        }

        if (soulParticles == null)
        {
            soulParticles = GetComponentInChildren<ParticleSystem>();
        }

        if (soulAppearance == null)
        {
            soulAppearance = GetComponent<SoulAppearanceController>();
        }

        if (possessableLayerMask.value == 0)
        {
            possessableLayerMask = LayerMask.GetMask("Default");
            if (debugMode) Debug.Log($"使用默认LayerMask: {possessableLayerMask.value}");
        }

        // 保存初始位置和旋转
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // 初始化灵魂状态
        ResetPlayerState();

        // 获取UI管理器
        uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogWarning("未找到UIManager实例，UI可能无法正常工作");
        }

        // 移除场景加载监听（简化回忆场景机制）
        // SceneManager.sceneLoaded -= OnSceneLoaded;
        // SceneManager.sceneUnloaded -= OnSceneUnloaded;

        // 检查当前场景类型
        CheckCurrentSceneType();

        if (debugMode) Debug.Log($"灵魂控制器初始化完成 - 位置: {transform.position}");


        playerUIManager = GetComponent<PlayerUIManager>();
        if (playerUIManager != null)
        {
            playerUIManager.ShowInstructions();
        }
        else
        {
            Debug.LogWarning("未找到PlayerUIManager组件，玩家UI提示可能无法正常工作");
        }

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
            isInMemoryScene = (currentScene.name == "DowlScene" || currentScene.name == "SoldierScene");
            if (debugMode) Debug.Log($"初始鼠标状态：{currentScene.name} 场景中鼠标已显示和解锁，isInMemoryScene: {isInMemoryScene}");
        }
        else
        {
            // 游戏场景：隐藏鼠标
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (debugMode) Debug.Log($"初始鼠标状态：{currentScene.name} 场景中鼠标已隐藏和锁定");
        }
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        playerInputActions.Player.Move.performed += OnMove;
        playerInputActions.Player.Move.canceled += OnMove;
        playerInputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Move.performed -= OnMove;
        playerInputActions.Player.Move.canceled -= OnMove;
        playerInputActions.Player.Jump.performed -= OnJump;
        playerInputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        // 移除场景事件监听
        // SceneManager.sceneLoaded -= OnSceneLoaded;
        // SceneManager.sceneUnloaded -= OnSceneUnloaded;

        // 确保单例实例被清理
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (isGameOver) return; // 游戏结束时不处理输入
        currentMovementInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGameOver) return; // 游戏结束时不处理输入

        if (context.performed)
        {
            jumpTriggered = true;
        }
    }

    void Update()
    {
        // 如果游戏结束，暂停所有游戏逻辑
        if (isGameOver) return;

        // 如果正在回忆场景中，不执行任何更新逻辑
        if (isInMemoryScene) return;

        // 检查输入
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isPossessing)
            {
                AttemptPossession();
            }
            else
            {
                ReleasePossession();
            }
        }

        // 更新灵魂计时器
        if (isSoulTimerActive && !isPossessing)
        {
            UpdateSoulTimer();
        }

        // 更新附身逻辑
        if (isPossessing && currentPossessable != null)
        {
            currentPossessable.PossessedUpdate();
        }
        else if (!isInMemoryScene)  // 不在回忆场景中才处理移动
        {
            HandleMovementAndJump();
        }
    }

    /// <summary>
    /// 重置玩家状态（用于重新开始游戏）
    /// </summary>
    public void ResetPlayerState()
    {
        if (debugMode) Debug.Log("重置玩家状态");

        // 重置位置保存标记
        hasMemoryPositionSaved = false;

        // 重置生命值
        currentHealth = maxHealth;

        // 重置灵魂时间
        soulTimeRemaining = maxSoulTime;
        isSoulTimerActive = true;

        // 重置游戏结束状态
        isGameOver = false;

        // 重置附身状态
        if (isPossessing)
        {
            ForceReleasePossession();
        }

        // 重置位置和旋转到初始位置
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // 重置移动状态
        currentMovementInput = Vector2.zero;
        playerVelocity = Vector3.zero;
        jumpTriggered = false;

        // 重置攻击冷却
        lastPurifierAttackTime = 0f;

        // 确保角色控制器启用
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 确保渲染器和碰撞器启用
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = true;

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;

        // 显示灵魂粒子效果
        ShowSoulParticles();

        // 切换相机目标回灵魂
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
        }

        // 启用输入
        playerInputActions.Player.Enable();

        if (debugMode) Debug.Log($"玩家状态已重置 - 生命值: {currentHealth}/{maxHealth}, 灵魂时间: {soulTimeRemaining}/{maxSoulTime}");

        if (playerUIManager != null)
        {
            playerUIManager.ShowInstructions();
        }
    }

    /// <summary>
    /// 保存玩家进入回忆场景前的位置
    /// </summary>
    public void SavePlayerPositionBeforeMemory()
    {
        positionBeforeMemoryScene = transform.position;
        rotationBeforeMemoryScene = transform.rotation;
        hasMemoryPositionSaved = true;

        if (debugMode) Debug.Log($"保存玩家位置: {positionBeforeMemoryScene}, 旋转: {rotationBeforeMemoryScene.eulerAngles}");
    }

    /// <summary>
    /// 从回忆场景返回后恢复玩家位置
    /// </summary>
    public void RestorePlayerPositionAfterMemory()
    {
        if (hasMemoryPositionSaved)
        {
            transform.position = positionBeforeMemoryScene;
            transform.rotation = rotationBeforeMemoryScene;

            if (debugMode) Debug.Log($"恢复玩家位置: {positionBeforeMemoryScene}, 旋转: {rotationBeforeMemoryScene.eulerAngles}");

            // 重置标记
            hasMemoryPositionSaved = false;
        }
    }

    /// <summary>
    /// 从回忆场景返回后的重置（不完全重置）
    /// </summary>
    public void ResetPlayerStateForMemoryReturn()
    {
        if (debugMode) Debug.Log("从回忆场景返回，进行部分重置");

        // 保持生命值和灵魂时间不变（从回忆场景返回时，这些状态应该保持不变）
        // 只重置必要的状态

        // 重置附身状态
        if (isPossessing)
        {
            ForceReleasePossession();
        }

        // 重置移动状态
        currentMovementInput = Vector2.zero;
        playerVelocity = Vector3.zero;
        jumpTriggered = false;

        // 确保角色控制器启用
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 确保渲染器和碰撞器启用
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = true;

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;

        // 显示灵魂粒子效果
        ShowSoulParticles();

        // 切换相机目标回灵魂
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
        }

        // 检查灵魂计时器状态
        if (!isPossessing && !isInMemoryScene)
        {
            isSoulTimerActive = true;
            if (debugMode) Debug.Log("从回忆场景返回，恢复灵魂计时器");
        }

        // 启用输入
        playerInputActions.Player.Enable();

        if (debugMode) Debug.Log($"从回忆场景返回完成 - 生命值: {currentHealth}/{maxHealth}, 灵魂时间: {soulTimeRemaining}/{maxSoulTime}");
    }

    /// <summary>
    /// 检查当前场景类型
    /// </summary>
    private void CheckCurrentSceneType()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name == "DowlScene" || currentScene.name == "SoldierScene")
        {
            isInMemoryScene = true;
            PauseSoulTimer();
            if (debugMode) Debug.Log($"当前在回忆场景 {currentScene.name} 中，暂停计时器");
        }
    }

    /// <summary>
    /// 进入回忆场景（供外部调用）
    /// </summary>
    public void EnterMemoryScene(string sceneName)
    {
        if (debugMode) Debug.Log($"准备进入回忆场景: {sceneName}");

        // 保存当前位置
        SavePlayerPositionBeforeMemory();

        // 设置回忆场景标志
        isInMemoryScene = true;

        // 暂停灵魂计时器
        PauseSoulTimer();

        // 暂停所有输入和移动
        playerInputActions.Player.Disable();

        // 显示鼠标
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (debugMode) Debug.Log($"已进入回忆场景 {sceneName}，位置已保存");
    }

    /// <summary>
    /// 退出回忆场景（供外部调用）
    /// </summary>
    public void ExitMemoryScene()
    {
        if (debugMode) Debug.Log("准备退出回忆场景");

        // 清除回忆场景标志
        isInMemoryScene = false;

        // 恢复位置
        RestorePlayerPositionAfterMemory();

        // 部分重置状态
        ResetPlayerStateForMemoryReturn();

        // 隐藏鼠标
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (debugMode) Debug.Log("已退出回忆场景，位置和状态已恢复");
    }

    /// <summary>
    /// 更新灵魂状态计时器
    /// </summary>
    private void UpdateSoulTimer()
    {
        // 如果游戏结束，不更新计时器
        if (isGameOver) return;

        // 如果正在回忆场景中，不更新计时器
        if (isInMemoryScene) return;

        // 如果时间已经耗尽，持续扣血
        if (soulTimeRemaining <= 0)
        {
            // 时间耗尽，持续扣血（每秒10点）
            TakeDamage(Time.deltaTime * 10f);
            return;
        }

        soulTimeRemaining -= Time.deltaTime;

        // 检查灵魂是否消散（时间耗尽扣血）
        if (soulTimeRemaining <= 0)
        {
            // 时间耗尽，开始扣血
            TakeDamage(Time.deltaTime * 10f);
        }
    }

    /// <summary>
    /// 灵魂消散（游戏结束）- 修复版本
    /// </summary>
    public void OnSoulDissipate()
    {
        if (isGameOver) return; // 防止重复调用

        Debug.Log("灵魂消散！游戏结束，3秒后返回StartScene");

        // 设置游戏结束状态
        isGameOver = true;

        // 暂停游戏时间！这样所有Update方法都会停止 - 场景完全静止
        Time.timeScale = 0f;

        // 停止所有移动和输入
        isSoulTimerActive = false;
        currentMovementInput = Vector2.zero;
        playerVelocity = Vector3.zero;

        // 禁用玩家输入
        playerInputActions.Player.Disable();

        // 停止粒子效果
        if (soulParticles != null)
        {
            soulParticles.Stop();
        }

        // 隐藏灵魂渲染
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        if (characterController != null) characterController.enabled = false;

        // 立即解锁鼠标
        UnlockMouse();

        // 显示游戏结束UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSimpleGameOver("over！");
        }
        else
        {
            Debug.LogWarning("UIManager实例未找到，无法显示游戏结束UI");
        }

        // 使用不受时间缩放影响的延迟
        StartCoroutine(LoadStartSceneAfterDelayUnscaled(3f));
    }

    private void UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (debugMode) Debug.Log("游戏结束，鼠标已解锁");
    }

    /// <summary>
    /// 使用不受时间缩放影响的延迟
    /// </summary>
    private IEnumerator LoadStartSceneAfterDelayUnscaled(float delay)
    {
        float elapsedTime = 0f;
        while (elapsedTime < delay)
        {
            // 使用 Time.unscaledDeltaTime，不受 Time.timeScale 影响
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // 恢复时间缩放
        Time.timeScale = 1f;

        // 确保鼠标在加载前解锁
        UnlockMouse();

        // 加载开始场景
        SceneManager.LoadScene("StartScene");
    }

    /// <summary>
    /// 重置灵魂计时器（当退出附身时调用）
    /// </summary>
    public void ResetSoulTimer()
    {
        soulTimeRemaining = maxSoulTime;
        isSoulTimerActive = true;
        if (debugMode) Debug.Log("灵魂计时器已重置");
    }

    /// <summary>
    /// 暂停灵魂计时器（当开始附身或进入回忆场景时调用）
    /// </summary>
    public void PauseSoulTimer()
    {
        isSoulTimerActive = false;
        if (debugMode) Debug.Log("灵魂计时器已暂停");
    }

    /// <summary>
    /// 检查是否在回忆场景中（供外部调用）
    /// </summary>
    public bool IsInMemoryScene()
    {
        return isInMemoryScene;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0 || isGameOver) return; // 如果已经死亡或游戏结束，不再扣血

        currentHealth -= damage;

        // 确保生命值不低于0
        if (currentHealth < 0) currentHealth = 0;

        // 检查是否死亡
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnSoulDissipate();
        }
    }

    /// <summary>
    /// 被净化者攻击
    /// </summary>
    public void TakePurifierDamage()
    {
        if (Time.time - lastPurifierAttackTime >= purifierAttackCooldown)
        {
            TakeDamage(purifierDamageAmount);
            lastPurifierAttackTime = Time.time;

            if (debugMode) Debug.Log($"被净化者攻击，受到{purifierDamageAmount}点伤害，当前生命值: {currentHealth}");
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    /// <param name="amount">恢复量</param>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void HandleMovementAndJump()
    {
        if (characterController == null) return;

        isGrounded = characterController.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.5f;
        }

        float cameraYRotation = 0f;
        if (cameraController != null)
        {
            cameraYRotation = cameraController.GetCurrentYRotation();
        }

        Vector3 moveDirection = new Vector3(currentMovementInput.x, 0, currentMovementInput.y);

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, cameraYRotation, 0);
            moveDirection = cameraRotation * moveDirection;

            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
        }

        if (jumpTriggered && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpTriggered = false;
        }

        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    void AttemptPossession()
    {
        if (debugMode) Debug.Log("=== 尝试附身 ===");

        // 可视化附身范围
        Debug.DrawRay(transform.position, Vector3.up * possessionRange, Color.red, 2f);

        // 修复：使用正确的方法查找IPossessable对象
        if (debugMode)
        {
            FindAndLogAllPossessables();
        }

        Debug.DrawRay(transform.position, Vector3.up * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.down * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.left * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.right * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.forward * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.back * possessionRange, Color.red, 2f);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, possessionRange, possessableLayerMask);

        if (debugMode) Debug.Log($"检测到 {hitColliders.Length} 个碰撞体（在指定层中）");

        if (hitColliders.Length == 0)
        {
            if (debugMode)
            {
                Debug.LogWarning("没有检测到任何可附身的碰撞体！");
                Debug.LogWarning($"检测范围: 半径={possessionRange}, 层={LayerMask.LayerToName(possessableLayerMask.value)}");
                Debug.LogWarning($"灵魂位置: {transform.position}");
            }
            return;
        }

        GameObject closestObject = null;
        IPossessable closestPossessable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            GameObject obj = hitCollider.gameObject;

            // 重要：排除灵魂自己！
            if (obj == this.gameObject)
            {
                if (debugMode) Debug.Log($"跳过检测：这是灵魂自己 ({obj.name})");
                continue;
            }

            float distance = Vector3.Distance(transform.position, obj.transform.position);

            if (debugMode)
            {
                Debug.Log($"--- 检测到对象: {obj.name} ---");
            }

            // 方法1: 使用GetComponent直接获取IPossessable接口
            IPossessable possessable = obj.GetComponent<IPossessable>();
            if (possessable != null)
            {
                if (debugMode)
                {
                    Debug.Log($"  ✓ 直接找到IPossessable组件！类型: {possessable.GetType().Name}");
                    Debug.Log($"  调用GetAbilityDescription: {possessable.GetAbilityDescription()}");
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                    if (debugMode) Debug.Log($"  ✓ 选择这个对象作为目标");
                }
                continue;
            }

            // 方法2: 尝试从子对象中获取
            possessable = obj.GetComponentInChildren<IPossessable>();
            if (possessable != null)
            {
                if (debugMode)
                {
                    Debug.Log($"  ✓ 在子对象中找到IPossessable！类型: {possessable.GetType().Name}");
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                }
                continue;
            }

            // 方法3: 尝试从父对象中获取
            possessable = obj.GetComponentInParent<IPossessable>();
            if (possessable != null)
            {
                if (debugMode)
                {
                    Debug.Log($"  ✓ 在父对象中找到IPossessable！类型: {possessable.GetType().Name}");
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                }
                continue;
            }

            if (debugMode)
            {
                Debug.Log($"  ✗ 没有找到IPossessable组件");

                Component[] allComps = obj.GetComponents<Component>();
                Debug.Log($"  对象上的所有组件 ({allComps.Length}):");
                foreach (Component comp in allComps)
                {
                    Debug.Log($"    - {comp.GetType().FullName}");
                }
            }
        }

        if (closestObject != null && closestPossessable != null)
        {
            if (debugMode) Debug.Log($"最终选择附身对象: {closestObject.name} (距离: {closestDistance:F2})");
            PossessObject(closestObject, closestPossessable);
        }
        else
        {
            if (debugMode) Debug.LogWarning("没有找到可附身的对象（有碰撞体但没有IPossessable接口）");
        }
    }

    // 新增方法：查找并记录所有实现了IPossessable的对象
    void FindAndLogAllPossessables()
    {
        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        List<IPossessable> allPossessables = new List<IPossessable>();

        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb is IPossessable)
            {
                allPossessables.Add(mb as IPossessable);
            }
        }

        Debug.Log($"场景中所有IPossessable对象: {allPossessables.Count}");
        foreach (IPossessable p in allPossessables)
        {
            MonoBehaviour mb = p as MonoBehaviour;
            if (mb != null)
            {
                Debug.Log($"  - {p.GetType().Name} on {mb.gameObject.name}");
            }
        }
    }

    void PossessObject(GameObject target, IPossessable possessable)
    {
        currentPossessedObject = target;
        currentPossessable = possessable;

        if (debugMode) Debug.Log($"准备附身到: {target.name} (类型: {possessable.GetType().Name})");

        // 重要：在调用OnPossess之前，先记录鹿的当前位置和旋转
        deerPositionBeforePossession = target.transform.position;
        deerRotationBeforePossession = target.transform.rotation;

        if (debugMode)
            Debug.Log($"鹿的原始位置: {deerPositionBeforePossession}, Y={deerPositionBeforePossession.y:F2}, 旋转: {deerRotationBeforePossession.eulerAngles}");

        // 调用鹿的OnPossess方法
        currentPossessable.OnPossess();
        isPossessing = true;

        // 暂停灵魂计时器（因为现在处于附身状态）
        PauseSoulTimer();

        // 隐藏灵魂
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        if (characterController != null) characterController.enabled = false;

        HideSoulParticles();

        // 重要：检查并确保鹿的位置没有因为任何原因改变
        // 如果鹿的位置改变了，立即纠正回原始位置
        if (currentPossessedObject != null)
        {
            // 检查位置是否改变
            float positionChange = Vector3.Distance(currentPossessedObject.transform.position, deerPositionBeforePossession);
            if (positionChange > 0.01f)
            {
                if (debugMode) Debug.LogWarning($"检测到鹿位置改变: {positionChange:F2} 单位，正在纠正...");

                // 记录改变后的位置用于调试
                Vector3 changedPosition = currentPossessedObject.transform.position;

                // 纠正位置和旋转
                currentPossessedObject.transform.position = deerPositionBeforePossession;
                currentPossessedObject.transform.rotation = deerRotationBeforePossession;

                if (debugMode)
                    Debug.Log($"纠正位置: 从 {changedPosition} (Y={changedPosition.y:F2}) 到 {deerPositionBeforePossession} (Y={deerPositionBeforePossession.y:F2})");
            }

            if (debugMode)
                Debug.Log($"附身后鹿的位置: {currentPossessedObject.transform.position}, Y={currentPossessedObject.transform.position.y:F2}");
        }

        if (cameraController != null)
        {
            cameraController.SetTarget(currentPossessedObject.transform);
            if (debugMode) Debug.Log("相机目标切换到: " + currentPossessedObject.name);
        }

        if (debugMode) Debug.Log("成功附身！");

        // 启动位置监控协程
        StartCoroutine(MonitorDeerPosition());
    }

    // 新增协程：监控鹿的位置
    IEnumerator MonitorDeerPosition()
    {
        // 连续监控几帧，确保位置稳定
        for (int i = 0; i < 5; i++)
        {
            yield return null; // 等待一帧

            if (currentPossessedObject != null)
            {
                // 检查Y值是否异常
                if (Mathf.Abs(currentPossessedObject.transform.position.y - deerPositionBeforePossession.y) > 0.5f)
                {
                    Debug.LogError($"第{i + 1}帧: 鹿的Y值异常！期望: {deerPositionBeforePossession.y:F2}, 实际: {currentPossessedObject.transform.position.y:F2}");

                    // 强制纠正Y值
                    Vector3 correctedPosition = new Vector3(
                        currentPossessedObject.transform.position.x,
                        deerPositionBeforePossession.y,
                        currentPossessedObject.transform.position.z
                    );

                    currentPossessedObject.transform.position = correctedPosition;
                    Debug.Log($"已纠正Y值到: {correctedPosition.y:F2}");
                }
            }
        }

        if (debugMode) Debug.Log("位置监控完成");

        if (playerUIManager != null)
        {
            playerUIManager.HideInstructions();
        }
    }

    void ReleasePossession()
    {
        if (currentPossessable != null)
        {
            // 调用鹿的释放方法
            currentPossessable.OnRelease();

            // 恢复灵魂的渲染和碰撞
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            // 将灵魂放在鹿的旁边（不要改变鹿的位置）
            if (currentPossessedObject != null)
            {
                transform.position = currentPossessedObject.transform.position + Vector3.up * 2f;
                if (debugMode) Debug.Log($"灵魂出现在鹿旁边，位置: {transform.position}");
            }

            // 启用灵魂的控制器
            if (characterController != null)
            {
                characterController.enabled = true;
            }

            // 显示灵魂粒子效果
            ShowSoulParticles();

            // 重置灵魂计时器（因为重新进入灵魂状态）
            // 只有在不在回忆场景中时才恢复计时器
            if (!isInMemoryScene)
            {
                ResetSoulTimer();
            }
            else
            {
                // 在回忆场景中，只重置时间但不激活计时器
                soulTimeRemaining = maxSoulTime;
                isSoulTimerActive = false;
                if (debugMode) Debug.Log("在回忆场景中脱离附身，计时器保持暂停");
            }

            // 切换相机目标回灵魂
            if (cameraController != null)
            {
                cameraController.SetTarget(transform);
                if (debugMode) Debug.Log("相机目标切换回灵魂");
            }

            // 重置附身状态
            currentPossessedObject = null;
            currentPossessable = null;
            isPossessing = false;

            if (debugMode) Debug.Log("已脱离附身");
        }
    }

    void HideSoulParticles()
    {
        if (soulParticles != null)
        {
            soulParticles.gameObject.SetActive(false);
        }
        else if (soulAppearance != null)
        {
            soulAppearance.HideSoul();
        }
        else
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                ps.gameObject.SetActive(false);
            }
        }
    }

    void ShowSoulParticles()
    {
        if (soulParticles != null)
        {
            soulParticles.transform.position = transform.position;
            soulParticles.gameObject.SetActive(true);
            if (!soulParticles.isPlaying)
            {
                soulParticles.Play();
            }
        }
        else if (soulAppearance != null)
        {
            soulAppearance.ShowSoul();
        }
        else
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                ps.gameObject.SetActive(true);
                ps.transform.position = transform.position;
                if (!ps.isPlaying)
                {
                    ps.Play();
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, possessionRange);

        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, possessionRange);
    }

    public void ForceReleasePossession()
    {
        if (isPossessing)
        {
            ReleasePossession();
        }
    }

    // ========== 兼容旧代码的方法 ==========

    /// <summary>
    /// 从 DowlScene 返回时调用（兼容旧代码）
    /// </summary>
    public void OnReturnFromDowlScene()
    {
        if (debugMode) Debug.Log("OnReturnFromDowlScene 被调用");
        ExitMemoryScene();
    }

    /// <summary>
    /// 从 SoldierScene 返回时调用（兼容旧代码）
    /// </summary>
    public void OnReturnFromSoldierScene()
    {
        if (debugMode) Debug.Log("OnReturnFromSoldierScene 被调用");
        ExitMemoryScene();
    }

    /// <summary>
    /// 检查是否在 DowlScene 中（兼容旧代码）
    /// </summary>
    public bool IsInDowlScene()
    {
        return isInMemoryScene && SceneManager.GetActiveScene().name == "DowlScene";
    }

    /// <summary>
    /// 检查是否在 SoldierScene 中（兼容旧代码）
    /// </summary>
    public bool IsInSoldierScene()
    {
        return isInMemoryScene && SceneManager.GetActiveScene().name == "SoldierScene";
    }

    /// <summary>
    /// 检查是否在任何回忆场景中（兼容旧代码）
    /// </summary>
    public bool IsInAnyMemorySceneMethod()
    {
        return isInMemoryScene;
    }
}