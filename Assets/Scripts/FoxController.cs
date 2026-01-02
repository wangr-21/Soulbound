using UnityEngine;
using System.Collections;

public class FoxController : MonoBehaviour, IPossessable
{
    [Header("移动设置")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float jumpForce = 6f;
    public float rotationSpeed = 10f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer = -1;

    [Header("模型引用")]
    public Transform foxModel;
    public bool fixModelRotation = true;

    [Header("动画参数 - 必须与Animator中的参数名完全一致！")]
    public string speedParam = "Speed";
    public string isGroundedParam = "IsGround";
    public string jumpParam = "Jump";

    [Header("动画阈值")]
    public float walkThreshold = 0.1f;
    public float runThreshold = 0.5f;

    [Header("跳跃修复设置")]
    public float jumpAnimationCooldown = 0.5f; // 跳跃动画冷却时间，防止连续触发
    public bool forceJumpTransition = true; // 是否强制切换跳跃状态
    public float jumpTransitionDuration = 0.05f; // 跳跃过渡时间

    [Header("AI 设置")]
    public FoxAI foxAI;
    private bool isAIControlled = false;

    [Header("能力描述")]
    public string abilityDescription = "敏捷的狐狸，可以行走、奔跑和跳跃";

    [Header("附身时间限制")]
    public float maxPossessionTime = 120f; // 最大附身时间
    public float possessionTimeRemaining = 0f; // 剩余附身时间
    private bool isPossessionTimerActive = false; // 附身计时器是否激活
    private bool isTimeExhausted = false; // 时间是否已耗尽

    [Header("生命值设置")]
    public float maxHealth = 80f; // 狐狸生命值比鹿少一些
    public float currentHealth = 0f;

    [Header("死亡效果")]
    public GameObject deathEffect;
    public AudioClip deathSound;

    [Header("状态")]
    public bool isPossessed = false;
    private bool isGrounded = true;
    private float currentSpeed = 0f;
    private Vector3 moveDirection = Vector3.zero;
    private bool jumpTriggered = false;
    private bool isJumping = false;
    private bool wasJumping = false;
    private float lastJumpTime = 0f;

    [Header("组件引用")]
    private Animator animator;
    private CharacterController controller;

    [Header("调试")]
    public bool showDebugInfo = true;
    public bool drawDebugGizmos = true;
    public bool logAnimationState = true;
    public bool autoFixParameters = true;
    public bool showRealTimeState = true; // 实时显示状态

    // 记录附身前的状态
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool controllerWasEnabled = true;

    // UI管理器引用
    private UIManager uiManager;

    void Start()
    {
        InitializeComponents();

        // 初始化生命值和附身时间
        currentHealth = maxHealth;
        possessionTimeRemaining = maxPossessionTime;
        isTimeExhausted = false;

        // 获取UI管理器
        uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogWarning("未找到UIManager实例，UI可能无法正常工作");
        }

        if (showDebugInfo)
        {
            Debug.Log($"狐狸({gameObject.name})初始化完成:");
            Debug.Log($"- 控制器位置: {transform.position}");
            Debug.Log($"- 控制器旋转: {transform.rotation.eulerAngles}");
            Debug.Log($"- 模型位置: {(foxModel != null ? foxModel.position.ToString() : "N/A")}");
            Debug.Log($"- 模型旋转: {(foxModel != null ? foxModel.rotation.eulerAngles.ToString() : "N/A")}");
            Debug.Log($"- 组件: Animator={animator != null}, Controller={controller != null}");
            Debug.Log($"- 生命值: {currentHealth}/{maxHealth}");
            Debug.Log($"- 附身时间: {possessionTimeRemaining}/{maxPossessionTime}");
        }
    }

    void InitializeComponents()
    {
        // 自动找到子对象中的模型和Animator
        if (foxModel == null)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshRenderer>() != null ||
                    child.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    foxModel = child;
                    break;
                }
            }

            if (foxModel == null && transform.childCount > 0)
            {
                foxModel = transform.GetChild(0);
            }
        }

        if (foxModel == null)
        {
            foxModel = transform;
        }

        // 获取Animator
        animator = foxModel.GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError($"狐狸({gameObject.name}): 需要Animator组件！");
            }
        }

        // 获取或添加CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.center = new Vector3(0, 0.5f, 0);
            controller.height = 1f;
            controller.radius = 0.3f;
        }

        // 获取或添加FoxAI组件
        foxAI = GetComponent<FoxAI>();
        if (foxAI == null)
        {
            foxAI = gameObject.AddComponent<FoxAI>();
            Debug.Log("自动添加FoxAI组件");
        }

        // 初始状态由AI控制（如果没有被附身）
        isAIControlled = !isPossessed;

        // 自动修复参数名
        if (autoFixParameters && animator != null)
        {
            AutoFixParameterNames();
        }

        // 确保有地面层
        if (groundLayer.value == 0 || groundLayer.value == -1)
        {
            groundLayer = LayerMask.GetMask("Default");
        }

        // 初始化位置
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        controllerWasEnabled = controller != null && controller.enabled;

        // 修复模型旋转
        if (fixModelRotation && foxModel != null)
        {
            FixModelRotation();
        }
    }

    void Update()
    {
        // 更新附身计时器
        if (isPossessionTimerActive && isPossessed)
        {
            UpdatePossessionTimer();
        }

        // 检查跳跃输入 - 冷却时间检查
        if (Input.GetButtonDown("Jump") && !jumpTriggered && (Time.time - lastJumpTime) > jumpAnimationCooldown)
        {
            jumpTriggered = true;
            if (showDebugInfo) Debug.Log($"跳跃触发 - 时间: {Time.time:F2}, 上次跳跃: {lastJumpTime:F2}");
        }

        // 非附身状态下的逻辑
        if (!isPossessed)
        {
            UpdateIdleBehavior();
            return;
        }
    }

    /// <summary>
    /// 更新附身计时器
    /// </summary>
    private void UpdatePossessionTimer()
    {
        // 如果还有剩余时间，减少时间
        if (possessionTimeRemaining > 0 && !isTimeExhausted)
        {
            possessionTimeRemaining -= Time.deltaTime;

            // 确保时间不会变成负数
            if (possessionTimeRemaining < 0)
            {
                possessionTimeRemaining = 0;
            }

            // 添加调试信息
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"狐狸附身剩余时间: {possessionTimeRemaining:F1}s, 生命值: {currentHealth:F1}");
            }

            // 检查时间是否耗尽
            if (possessionTimeRemaining <= 0)
            {
                isTimeExhausted = true;
                Debug.Log("狐狸的附身时间耗尽！开始持续扣血");
            }
        }

        // 时间耗尽，持续扣血
        if (isTimeExhausted)
        {
            // 每秒扣15点血
            float damagePerSecond = 15f;
            float damageThisFrame = damagePerSecond * Time.deltaTime;
            TakeDamage(damageThisFrame);

            // 添加调试信息
            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log($"狐狸时间耗尽持续扣血中，当前生命值: {currentHealth:F1}, 本帧伤害: {damageThisFrame:F2}");
            }
        }
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // 如果已经死亡，不再扣血

        currentHealth -= damage;

        // 确保生命值不低于0
        if (currentHealth < 0) currentHealth = 0;

        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} 死亡！当前生命值: {currentHealth}");

        // 播放死亡效果
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // 如果当前被附身，强制玩家灵魂脱离
        if (isPossessed)
        {
            PlayerSoulController.Instance.ForceReleasePossession();

            // 如果玩家在动物死亡时没有足够生命值，游戏结束
            if (PlayerSoulController.Instance.currentHealth <= 0)
            {
                StartCoroutine(DelayedGameOver(1f));
            }
        }

        // 销毁动物
        Destroy(gameObject);
    }

    private IEnumerator DelayedGameOver(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 调用玩家灵魂的游戏结束方法
        PlayerSoulController.Instance.OnSoulDissipate();
    }

    /// <summary>
    /// 被净化者攻击
    /// </summary>
    public void TakePurifierDamage(float damage)
    {
        TakeDamage(damage);
    }

    // ===== 实现 IPossessable 接口 =====
    public void OnPossess()
    {
        isPossessed = true;
        isAIControlled = false; // 禁用AI控制

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        controllerWasEnabled = controller != null && controller.enabled;

        // 启动附身计时器
        isPossessionTimerActive = true;
        isTimeExhausted = false;
        possessionTimeRemaining = maxPossessionTime;

        if (showDebugInfo)
        {
            Debug.Log($"=== 狐狸({gameObject.name})被附身 ===");
            Debug.Log($"- 控制器启用状态: {controllerWasEnabled}");
            Debug.Log($"- 当前动画参数: Speed={speedParam}, Grounded={isGroundedParam}, Jump={jumpParam}");
            Debug.Log($"- 附身时间重置为: {possessionTimeRemaining}s");
        }

        // 如果存在AI组件，禁用AI
        if (foxAI != null)
        {
            foxAI.enabled = false;
        }

        // 确保控制器启用
        if (controller != null)
        {
            controller.enabled = true;
        }

        // 重置动画状态
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
            animator.ResetTrigger(jumpParam);
        }

        // 重置移动状态
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        jumpTriggered = false;
        isJumping = false;
        wasJumping = false;
        lastJumpTime = 0f;

        // 确保在地面
        if (controller != null)
        {
            isGrounded = controller.isGrounded;
        }

        if (showDebugInfo) Debug.Log($"狐狸({gameObject.name})准备接受控制");
    }

    public void OnRelease()
    {
        isPossessed = false;
        isAIControlled = true; // 启用AI控制

        // 停止附身计时器
        isPossessionTimerActive = false;
        isTimeExhausted = false;
        possessionTimeRemaining = maxPossessionTime;

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
            animator.ResetTrigger(jumpParam);
        }

        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        jumpTriggered = false;
        isJumping = false;
        wasJumping = false;

        // 如果存在AI组件，启用AI
        if (foxAI != null)
        {
            foxAI.enabled = true;
        }

        if (controller != null)
        {
            controller.enabled = controllerWasEnabled;
        }

        if (showDebugInfo) Debug.Log($"狐狸({gameObject.name})脱离附身！");
    }

    public string GetAbilityDescription()
    {
        return abilityDescription;
    }

    public void PossessedUpdate()
    {
        if (!isPossessed) return;

        HandleMovement();
        CheckGroundStatus();
        UpdateAnimations();
        DebugAnimations();
        CheckJumpState();
    }
    // ===== 接口实现结束 =====

    void UpdateIdleBehavior()
    {
        // 非附身状态下的空闲行为
        if (isAIControlled && foxAI != null && foxAI.enabled)
        {
            // AI会通过FoxAI.Update()自动控制
            // 但我们仍然需要处理一些基本逻辑
            CheckGroundStatus();

            // 确保在地面且下落速度小于0，重置Y速度
            if (controller != null)
            {
                isGrounded = controller.isGrounded;
                if (isGrounded && moveDirection.y < 0)
                {
                    moveDirection.y = -2f;
                }

                // 应用重力
                if (!isGrounded)
                {
                    moveDirection.y += Physics.gravity.y * Time.deltaTime;
                }

                // 应用垂直移动
                controller.Move(new Vector3(0, moveDirection.y, 0) * Time.deltaTime);
            }
        }
        else
        {
            if (animator != null)
            {
                if (animator.GetFloat(speedParam) > 0.1f)
                {
                    animator.SetFloat(speedParam, 0f);
                }

                if (!isGrounded && controller != null)
                {
                    isGrounded = controller.isGrounded;
                    animator.SetBool(isGroundedParam, isGrounded);
                }
            }
        }
    }

    // 新增方法：供AI调用移动
    public void AIMove(Vector3 direction, bool isRunning = false)
    {
        if (controller == null || !isAIControlled)
        {
            if (showDebugInfo)
                Debug.LogWarning($"AIMove调用失败: controller={controller != null}, isAIControlled={isAIControlled}");
            return;
        }

        // 检查是否在地面
        isGrounded = controller.isGrounded;

        // 如果在地面且下落速度小于0，重置Y速度
        if (isGrounded && moveDirection.y < 0)
        {
            moveDirection.y = -2f;
        }

        // AI移动时，根据isRunning参数决定速度
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        // 应用水平移动
        if (direction.magnitude > 0.1f)
        {
            // 计算移动向量
            Vector3 move = direction.normalized * currentSpeed;
            moveDirection.x = move.x;
            moveDirection.z = move.z;

            // 旋转控制器对象
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // 设置动画速度 - 狐狸的Speed参数是归一化的
            if (animator != null)
            {
                float normalizedSpeed = Mathf.Clamp01(currentSpeed / runSpeed);
                animator.SetFloat(speedParam, normalizedSpeed);
            }

            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"AI移动: 速度={currentSpeed:F1}, 方向={direction}, 移动向量={move}");
            }
        }
        else
        {
            // 没有方向，停止移动
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 10f);
            moveDirection.x = 0;
            moveDirection.z = 0;

            if (animator != null)
            {
                float normalizedSpeed = Mathf.Clamp01(currentSpeed / runSpeed);
                animator.SetFloat(speedParam, normalizedSpeed);
            }
        }

        // 应用重力
        if (!isGrounded)
        {
            moveDirection.y += Physics.gravity.y * Time.deltaTime;
        }

        // 应用所有移动 - 这是关键！
        if (controller.enabled)
        {
            controller.Move(moveDirection * Time.deltaTime);
        }
    }

    void HandleMovement()
    {
        if (controller == null) return;

        // 检查是否在地面
        isGrounded = controller.isGrounded;

        // 如果在地面且下落速度小于0，重置Y速度
        if (isGrounded && moveDirection.y < 0)
        {
            moveDirection.y = -2f;
        }

        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool runInput = Input.GetKey(KeyCode.LeftShift);

        // 计算移动方向
        Vector3 move = new Vector3(horizontal, 0, vertical);

        // 获取相机方向
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;

            cameraForward.Normalize();
            cameraRight.Normalize();

            move = cameraForward * vertical + cameraRight * horizontal;
        }

        // 判断是否有移动输入
        bool isMoving = move.magnitude > 0.1f;

        // 计算速度 - 跳跃期间保持当前速度
        if (!isJumping)
        {
            if (isMoving)
            {
                float targetSpeed = runInput ? runSpeed : walkSpeed;
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

                move.Normalize();
                moveDirection.x = move.x * currentSpeed;
                moveDirection.z = move.z * currentSpeed;

                // 旋转控制器对象
                if (move != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(new Vector3(move.x, 0, move.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 10f);
                moveDirection.x = 0;
                moveDirection.z = 0;
            }
        }
        else
        {
            // 跳跃期间保持水平移动动量
            moveDirection.x = moveDirection.x * 0.99f; // 轻微减速
            moveDirection.z = moveDirection.z * 0.99f;
        }

        // 跳跃处理 - 修复：确保在地面且未跳跃时触发
        if (isGrounded && jumpTriggered && !isJumping)
        {
            moveDirection.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            isJumping = true;
            wasJumping = true;
            lastJumpTime = Time.time;

            // 触发跳跃动画 - 使用多种方法确保触发
            if (animator != null)
            {
                // 方法1: 使用触发器
                animator.ResetTrigger(jumpParam);
                animator.SetTrigger(jumpParam);

                // 方法2: 强制切换状态（如果触发器不起作用）
                if (forceJumpTransition)
                {
                    StartCoroutine(ForceJumpState());
                }

                if (showDebugInfo)
                {
                    Debug.Log($"触发跳跃动画，触发器: {jumpParam}");
                    Debug.Log($"跳跃速度: {moveDirection.y:F2}");
                    Debug.Log($"当前状态: {GetCurrentStateName()}");
                }
            }
            jumpTriggered = false;
        }

        // 应用重力
        if (!isGrounded)
        {
            moveDirection.y += Physics.gravity.y * Time.deltaTime;
        }

        // 应用移动
        if (controller.enabled)
        {
            controller.Move(moveDirection * Time.deltaTime);
        }
    }

    // 强制切换跳跃状态的协程
    IEnumerator ForceJumpState()
    {
        if (animator == null) yield break;

        // 等待一帧确保触发器被处理
        yield return null;

        // 检查当前状态
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName("Jump"))
        {
            Debug.LogWarning("触发器未切换到Jump状态，尝试强制切换...");

            // 方法1: 直接播放Jump状态
            animator.Play("Jump", 0, 0f);

            // 等待一帧检查结果
            yield return null;

            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jump"))
            {
                Debug.Log("✓ 成功强制切换到Jump状态");
            }
            else
            {
                Debug.LogError("✗ 强制切换失败，当前状态: " + GetCurrentStateName());

                // 方法2: 尝试其他可能的状态名
                string[] possibleJumpStateNames = { "Jump", "JUMP", "jump", "Jumping", "JUMPING", "jumping" };
                foreach (string stateName in possibleJumpStateNames)
                {
                    if (animator.HasState(0, Animator.StringToHash("Base Layer." + stateName)))
                    {
                        animator.Play(stateName, 0, 0f);
                        Debug.Log($"尝试切换到状态: {stateName}");
                        break;
                    }
                }
            }
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 计算归一化速度 - 跳跃期间不更新速度参数
        if (!isJumping)
        {
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / runSpeed);
            animator.SetFloat(speedParam, normalizedSpeed);
        }
        else
        {
            // 跳跃期间将速度参数设为0，避免速度参数影响跳跃状态
            animator.SetFloat(speedParam, 0f);
        }

        // 设置地面参数
        animator.SetBool(isGroundedParam, isGrounded);

        // 确保跳跃触发器被重置（在地面时）
        if (isGrounded && wasJumping)
        {
            animator.ResetTrigger(jumpParam);
            wasJumping = false;

            // 强制回到Idle状态，防止卡在Jump状态
            if (isJumping)
            {
                animator.Play("Idle", 0, 0f);
            }
        }
    }

    void CheckJumpState()
    {
        if (isJumping && isGrounded)
        {
            isJumping = false;
            if (showDebugInfo) Debug.Log("跳跃结束，回到地面");
        }
    }

    void CheckGroundStatus()
    {
        if (controller == null) return;

        bool wasGroundedBefore = isGrounded;
        isGrounded = controller.isGrounded;

        // 额外的射线检测
        if (!isGrounded)
        {
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayer))
            {
                isGrounded = true;
            }
        }

        // 状态变化时的处理
        if (wasGroundedBefore != isGrounded)
        {
            if (isGrounded && isJumping)
            {
                isJumping = false;
                if (showDebugInfo) Debug.Log("跳跃结束，回到地面");
            }
        }
    }

    void DebugAnimations()
    {
        if (!logAnimationState || animator == null) return;

        if (Time.frameCount % 30 == 0 || showRealTimeState) // 每30帧输出一次，或实时输出
        {
            float speedValue = animator.GetFloat(speedParam);
            bool groundedValue = animator.GetBool(isGroundedParam);
            string currentState = GetCurrentStateName();

            Debug.Log($"动画状态: {currentState}, Speed={speedValue:F2}, Grounded={groundedValue}, Jumping={isJumping}, Triggered={jumpTriggered}");
        }
    }

    // ===== 以下是原脚本的辅助方法，保持不变 =====

    string GetCurrentStateName()
    {
        if (animator == null) return "No Animator";

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Idle")) return "Idle";
        if (stateInfo.IsName("Walk")) return "Walk";
        if (stateInfo.IsName("Run")) return "Run";
        if (stateInfo.IsName("Jump")) return "Jump";

        // 尝试通过哈希值判断
        if (stateInfo.fullPathHash == Animator.StringToHash("Base Layer.Idle")) return "Idle";
        if (stateInfo.fullPathHash == Animator.StringToHash("Base Layer.Walk")) return "Walk";
        if (stateInfo.fullPathHash == Animator.StringToHash("Base Layer.Run")) return "Run";
        if (stateInfo.fullPathHash == Animator.StringToHash("Base Layer.Jump")) return "Jump";

        return $"Unknown ({stateInfo.fullPathHash})";
    }

    void AutoFixParameterNames()
    {
        if (animator == null) return;

        Debug.Log("=== 自动检查Animator参数 ===");

        // 检查所有参数
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"找到参数: {param.name} ({param.type})");
        }

        // 检查并修复参数名
        CheckAndFixParameter(speedParam, "Speed", "speed", "MoveSpeed");
        CheckAndFixParameter(isGroundedParam, "IsGround", "IsGrounded", "Grounded");
        CheckAndFixParameter(jumpParam, "Jump", "jump", "JumpTrigger");

        Debug.Log($"最终使用的参数: Speed={speedParam}, Grounded={isGroundedParam}, Jump={jumpParam}");
    }

    void CheckAndFixParameter(string currentParam, params string[] possibleNames)
    {
        if (HasParameter(currentParam)) return;

        foreach (string name in possibleNames)
        {
            if (HasParameter(name))
            {
                Debug.Log($"参数 '{currentParam}' 不存在，自动改为 '{name}'");

                // 根据参数类型设置正确的字段
                if (possibleNames[0].Contains("Speed"))
                    speedParam = name;
                else if (possibleNames[0].Contains("Ground"))
                    isGroundedParam = name;
                else if (possibleNames[0].Contains("Jump"))
                    jumpParam = name;

                return;
            }
        }

        Debug.LogWarning($"未找到参数 '{currentParam}' 的任何变体！");
    }

    bool HasParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    void FixModelRotation()
    {
        if (foxModel == null) return;

        Vector3 modelEuler = foxModel.localRotation.eulerAngles;
        if (Mathf.Abs(modelEuler.x + 90) < 1f || Mathf.Abs(modelEuler.x - 270) < 1f)
        {
            if (showDebugInfo) Debug.Log($"检测到模型旋转问题: {modelEuler}");

            Quaternion fixedRotation = Quaternion.Euler(0, modelEuler.y, modelEuler.z);
            foxModel.localRotation = fixedRotation;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        // 绘制控制器位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // 绘制地面检测线
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * (groundCheckDistance + 0.1f));

        // 绘制移动方向
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Vector3 horizontalMove = new Vector3(moveDirection.x, 0, moveDirection.z);
            if (horizontalMove.magnitude > 0.1f)
            {
                Gizmos.DrawLine(transform.position, transform.position + horizontalMove.normalized * 2f);
            }
        }
    }

    // ===== 调试方法 =====
    [ContextMenu("测试：立即时间耗尽")]
    public void TestTimeExhausted()
    {
        possessionTimeRemaining = 0.1f; // 设置很少的时间
        Debug.Log("测试：狐狸的附身时间设置为0.1秒");
    }

    [ContextMenu("检查当前状态")]
    public void ShowCurrentStateDetails()
    {
        if (animator == null)
        {
            Debug.LogError("没有Animator组件！");
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        Debug.Log("=== 当前状态详情 ===");
        Debug.Log($"状态名称: {GetCurrentStateName()}");
        Debug.Log($"状态哈希: {stateInfo.fullPathHash}");
        Debug.Log($"状态长度: {stateInfo.length:F2}秒");
        Debug.Log($"标准化时间: {stateInfo.normalizedTime:F2}");
        Debug.Log($"是否循环: {stateInfo.loop}");
        Debug.Log($"速度倍数: {stateInfo.speed}");
        Debug.Log($"是否在过渡: {animator.IsInTransition(0)}");

        if (animator.IsInTransition(0))
        {
            AnimatorTransitionInfo transInfo = animator.GetAnimatorTransitionInfo(0);
            Debug.Log($"过渡持续时间: {transInfo.duration:F2}");
            Debug.Log($"过渡标准化时间: {transInfo.normalizedTime:F2}");
        }
    }

    // 其他调试方法保持不变...
    [ContextMenu("测试动画状态")]
    public void TestAnimationStates()
    {
        if (animator == null)
        {
            Debug.LogError("没有Animator组件！");
            return;
        }

        StartCoroutine(TestAnimations());
    }

    IEnumerator TestAnimations()
    {
        Debug.Log("=== 开始测试动画状态 ===");

        // 测试Idle
        Debug.Log("1. 测试Idle状态");
        animator.SetFloat(speedParam, 0f);
        animator.SetBool(isGroundedParam, true);
        yield return new WaitForSeconds(1f);

        // 测试Walk
        Debug.Log("2. 测试Walk状态");
        animator.SetFloat(speedParam, 0.3f);
        yield return new WaitForSeconds(1f);

        // 测试Run
        Debug.Log("3. 测试Run状态");
        animator.SetFloat(speedParam, 0.8f);
        yield return new WaitForSeconds(1f);

        // 测试Jump
        Debug.Log("4. 测试Jump状态 - 使用触发器");
        animator.ResetTrigger(jumpParam);
        animator.SetTrigger(jumpParam);
        animator.SetBool(isGroundedParam, false);

        // 等待并检查状态
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"跳跃后状态: {GetCurrentStateName()}");

        yield return new WaitForSeconds(0.7f);

        // 回到Idle
        Debug.Log("5. 回到Idle状态");
        animator.SetFloat(speedParam, 0f);
        animator.SetBool(isGroundedParam, true);

        Debug.Log("=== 测试完成 ===");
    }
}