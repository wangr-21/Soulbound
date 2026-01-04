using UnityEngine;
using System.Collections;

public class BirdController : MonoBehaviour, IPossessable
{
    [Header("移动设置")]
    public float flySpeed = 5f;
    public float glideSpeed = 8f;
    public float rotationSpeed = 2f;
    public float ascendSpeed = 4f;
    public float descendSpeed = 8f;
    public float gravity = 15f;
    public float idleDrag = 0.3f;
    public float groundCheckDistance = 0.4f;

    [Header("模型引用")]
    public Transform birdModel;
    public bool fixModelRotation = true;

    [Header("动画参数")]
    public string flyParam = "IsFlying";
    public string glideParam = "IsGliding";

    [Header("动画阈值")]
    public float flyThreshold = 0.1f;

    [Header("能力描述")]
    public string abilityDescription = "可以飞行和滑翔的鸟";

    [Header("AI 设置")]
    public BirdAI birdAI;
    private bool isAIControlled = false;

    [Header("附身时间限制")]
    public float maxPossessionTime = 100f; // 最大附身时间（鸟的附身时间较短）
    public float possessionTimeRemaining = 0f; // 剩余附身时间
    private bool isPossessionTimerActive = false; // 附身计时器是否激活
    private bool isTimeExhausted = false; // 时间是否已耗尽

    [Header("生命值设置")]
    public float maxHealth = 60f; // 鸟的生命值较低，比较脆弱
    public float currentHealth = 0f;

    [Header("死亡效果")]
    public GameObject deathEffect;
    public AudioClip deathSound;

    [Header("状态")]
    public bool isPossessed = false;
    private bool isGrounded = true;
    private bool isGliding = false;
    private float verticalVelocity = 0f;
    private Vector3 groundPosition = Vector3.zero;

    [Header("输入状态")]
    private bool hasHorizontalInput;
    private bool isAscending;
    private bool isDescending;

    [Header("组件引用")]
    private Animator animator;
    private CharacterController controller;
    private Vector3 horizontalMoveDirection = Vector3.zero;

    [Header("调试")]
    public bool showDebugInfo = true;
    public LayerMask groundLayer = -1;
    public bool drawDebugGizmos = true;
    public bool logAnimationState = true;
    public bool autoFixParameters = true;
    public bool showRealTimeState = true;

    // AI输入变量
    private Vector2 aiMoveInput = Vector2.zero;
    private bool aiAscending = false;
    private bool aiDescending = false;
    private bool aiGliding = false;

    // UI管理器引用
    private UIManager uiManager;

    // ===============================================================
    // Start
    // ===============================================================
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

        FindGroundPosition(); // ★ 初始化贴地

        if (showDebugInfo)
        {
            Debug.Log($"鸟({gameObject.name})初始化完成:");
            Debug.Log($"- 控制器位置: {transform.position}");
            Debug.Log($"- 控制器旋转: {transform.rotation.eulerAngles}");
            Debug.Log($"- 模型位置: {(birdModel != null ? birdModel.position.ToString() : "N/A")}");
            Debug.Log($"- 模型旋转: {(birdModel != null ? birdModel.rotation.eulerAngles.ToString() : "N/A")}");
            Debug.Log($"- 组件: Animator={animator != null}, Controller={controller != null}");
            Debug.Log($"- 生命值: {currentHealth}/{maxHealth}");
            Debug.Log($"- 附身时间: {possessionTimeRemaining}/{maxPossessionTime}");
        }
    }

    void InitializeComponents()
    {
        // 自动找到子对象中的模型和Animator
        if (birdModel == null)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshRenderer>() != null ||
                    child.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    birdModel = child;
                    break;
                }
            }

            if (birdModel == null && transform.childCount > 0)
            {
                birdModel = transform.GetChild(0);
            }
        }

        if (birdModel == null)
        {
            birdModel = transform;
        }

        // 获取Animator
        animator = birdModel.GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError($"鸟({gameObject.name}): 需要Animator组件！");
            }
        }

        // 获取或添加CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.center = new Vector3(0, 0.5f, 0);
            controller.height = 0.5f; // 鸟的控制器较矮
            controller.radius = 0.2f;
        }

        // 获取或添加BirdAI组件
        birdAI = GetComponent<BirdAI>();
        if (birdAI == null)
        {
            birdAI = gameObject.AddComponent<BirdAI>();
            Debug.Log("自动添加BirdAI组件");
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

        // 修复模型旋转
        if (fixModelRotation && birdModel != null)
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

        // 如果没有被附身且AI控制，执行AI更新
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
                Debug.Log($"鸟附身剩余时间: {possessionTimeRemaining:F1}s, 生命值: {currentHealth:F1}");
            }

            // 检查时间是否耗尽
            if (possessionTimeRemaining <= 0)
            {
                isTimeExhausted = true;
                Debug.Log("鸟的附身时间耗尽！开始持续扣血");
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
                Debug.Log($"鸟时间耗尽持续扣血中，当前生命值: {currentHealth:F1}, 本帧伤害: {damageThisFrame:F2}");
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

    // 在DeerController中添加以下方法
    public void ResetState()
    {
        // 重置血量
        currentHealth = maxHealth;

        // 重置附身时间
        possessionTimeRemaining = maxPossessionTime;

        // 重置其他状态
        // isDead = false;
        // 其他需要重置的变量...

        Debug.Log("DeerController: 状态已重置");
    }

    // ===============================================================
    // IPossessable 接口实现
    // ===============================================================
    public void OnPossess()
    {
        isPossessed = true;
        isAIControlled = false; // 禁用AI控制

        // 启动附身计时器
        isPossessionTimerActive = true;
        isTimeExhausted = false;
        possessionTimeRemaining = maxPossessionTime;

        if (controller) controller.enabled = true;

        // 如果存在AI组件，禁用AI
        if (birdAI != null)
        {
            birdAI.enabled = false;
        }

        verticalVelocity = 0f;
        isGliding = false;

        // 重置AI输入
        aiMoveInput = Vector2.zero;
        aiAscending = false;
        aiDescending = false;
        aiGliding = false;

        // 重置动画状态
        if (animator)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }

        // ★ ★ ★ 附身瞬间强制校准地面对齐
        FindGroundPosition();
        SnapToGround();
        isGrounded = true;

        if (showDebugInfo)
        {
            Debug.Log($"=== 鸟({gameObject.name})被附身 ===");
            Debug.Log($"- 当前动画参数: Fly={flyParam}, Glide={glideParam}");
            Debug.Log($"- 附身时间重置为: {possessionTimeRemaining}s");
        }

        Debug.Log("鸟被附身并校正位置！");
    }

    public void OnRelease()
    {
        isPossessed = false;
        isAIControlled = true; // 启用AI控制

        // 停止附身计时器
        isPossessionTimerActive = false;
        isTimeExhausted = false;
        possessionTimeRemaining = maxPossessionTime;

        // 重置移动状态
        verticalVelocity = 0;
        isGliding = false;
        hasHorizontalInput = false;
        isAscending = false;
        isDescending = false;

        // 重置AI输入
        aiMoveInput = Vector2.zero;
        aiAscending = false;
        aiDescending = false;
        aiGliding = false;

        // 如果存在AI组件，启用AI
        if (birdAI != null)
        {
            birdAI.enabled = true;
        }

        // 设置动画
        if (animator)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }

        if (showDebugInfo) Debug.Log("鸟脱离附身！");
    }

    public string GetAbilityDescription() => abilityDescription;

    public void PossessedUpdate()
    {
        if (!isPossessed) return;

        GetInput();
        HandleMovement();
        UpdateAnimations();
        CheckGroundStatus();
        DebugAnimations();
    }
    // ===============================================================
    // 接口实现结束
    // ===============================================================

    void UpdateIdleBehavior()
    {
        // 非附身状态下的空闲行为
        if (isAIControlled && birdAI != null && birdAI.enabled)
        {
            HandleAIMovement();
            UpdateAnimations();
            CheckGroundStatus();
        }
        else
        {
            if (animator != null)
            {
                if (animator.GetBool(flyParam))
                {
                    animator.SetBool(flyParam, false);
                }
                if (animator.GetBool(glideParam))
                {
                    animator.SetBool(glideParam, false);
                }
            }
        }
    }

    // 新增方法：供AI调用设置输入
    public void SetAIInput(Vector2 moveInput, bool ascending, bool descending, bool gliding)
    {
        if (!isAIControlled) return;

        aiMoveInput = moveInput;
        aiAscending = ascending;
        aiDescending = descending;
        aiGliding = gliding;
    }

    // ===============================================================
    // AI移动逻辑
    // ===============================================================
    void HandleAIMovement()
    {
        if (!controller) return;

        // 使用AI输入
        hasHorizontalInput = aiMoveInput.magnitude > 0.1f;
        isAscending = aiAscending;
        isDescending = aiDescending;

        // AI控制的滑翔
        if (aiGliding && !isGrounded)
        {
            isGliding = true;
            if (animator) animator.SetBool(glideParam, true);
        }
        else
        {
            isGliding = false;
            if (animator) animator.SetBool(glideParam, false);
        }

        // 直接从AI输入构建移动方向（不使用相机转换）
        Vector3 move = new Vector3(aiMoveInput.x, 0, aiMoveInput.y);

        // 水平平滑
        if (!hasHorizontalInput)
            horizontalMoveDirection = Vector3.Lerp(horizontalMoveDirection, Vector3.zero, idleDrag * Time.deltaTime);
        else
            horizontalMoveDirection = move.normalized;

        // ⭐ 重力/飞行垂直速度控制
        if (isGrounded)
        {
            verticalVelocity = 0;
            // 在地面时，如果有上升输入，起飞
            if (isAscending)
            {
                verticalVelocity = ascendSpeed;
                isGrounded = false;
            }
        }
        else if (isAscending)
        {
            verticalVelocity = Mathf.Lerp(verticalVelocity, ascendSpeed, 8f * Time.deltaTime);
        }
        else if (isDescending)
        {
            verticalVelocity = Mathf.Lerp(verticalVelocity, -descendSpeed, 10f * Time.deltaTime);
        }
        else if (isGliding)
        {
            verticalVelocity = Mathf.Lerp(verticalVelocity, -descendSpeed * 0.3f, 5f * Time.deltaTime);
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -descendSpeed * 2f);
        }

        // 最终移动
        Vector3 finalMove = horizontalMoveDirection * (isGliding ? glideSpeed : flySpeed);
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);

        // 旋转（只有移动才转）
        if (horizontalMoveDirection.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(horizontalMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"鸟AI状态: 位置={transform.position:F2}, 垂直速度={verticalVelocity:F2}, " +
                     $"在地面={isGrounded}, 滑翔={isGliding}, AI输入={aiMoveInput}");
        }
    }

    // ===============================================================
    // 输入检测（玩家控制）
    // ===============================================================
    void GetInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        hasHorizontalInput = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;
        isAscending = Input.GetKey(KeyCode.Space);
        isDescending = Input.GetKey(KeyCode.LeftControl);
    }

    // ===============================================================
    // 核心移动逻辑（玩家控制）
    // ===============================================================
    void HandleMovement()
    {
        if (!controller) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 获取相机方向 → 转为世界空间运动向量
        Vector3 move = new Vector3(h, 0, v);
        if (Camera.main)
        {
            Vector3 fwd = Camera.main.transform.forward;
            fwd.y = 0;
            fwd.Normalize();
            Vector3 right = Camera.main.transform.right;
            right.y = 0;
            right.Normalize();
            move = fwd * v + right * h;
        }

        // 水平平滑
        if (!hasHorizontalInput)
            horizontalMoveDirection = Vector3.Lerp(horizontalMoveDirection, Vector3.zero, idleDrag * Time.deltaTime);
        else
            horizontalMoveDirection = move.normalized;

        // ⭐ 重力/飞行垂直速度控制
        if (isGrounded)
        {
            verticalVelocity = 0;
        }
        else if (isAscending)
        {
            verticalVelocity = Mathf.Lerp(verticalVelocity, ascendSpeed, 8f * Time.deltaTime);
        }
        else if (isDescending)
        {
            verticalVelocity = Mathf.Lerp(verticalVelocity, -descendSpeed, 10f * Time.deltaTime);
        }
        else if (isGliding)
        {
            verticalVelocity = Mathf.Lerp(verticalVelocity, -descendSpeed * 0.3f, 5f * Time.deltaTime);
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -descendSpeed * 2f);
        }

        // 最终移动
        Vector3 finalMove = horizontalMoveDirection * (isGliding ? glideSpeed : flySpeed);
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);

        // 旋转（只有移动才转）
        if (horizontalMoveDirection.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(horizontalMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
            Debug.Log($"鸟玩家状态: pos={transform.position:F2}, vY={verticalVelocity:F2}, grounded={isGrounded}");
    }

    // ===============================================================
    // 地面检测（已修复 → 基于CharacterController底部）
    // ===============================================================
    void CheckGroundStatus()
    {
        if (!controller) return;

        float controllerBottom = controller.height / 2f - controller.radius;
        Vector3 rayStart = transform.position + Vector3.down * controllerBottom + Vector3.up * 0.05f;

        if (Physics.Raycast(rayStart, Vector3.down, out var hit, groundCheckDistance + 0.2f, groundLayer))
        {
            if (hit.distance < 0.3f && verticalVelocity <= 0)
            {
                isGrounded = true;
                groundPosition = hit.point;
                SnapToGround();
                return;
            }
        }

        isGrounded = false;
    }

    // ===============================================================
    // 保证脚贴地
    // ===============================================================
    void SnapToGround()
    {
        float controllerBottom = controller.height / 2f - controller.radius;
        Vector3 pos = transform.position;
        pos.y = groundPosition.y + controllerBottom;
        transform.position = pos;
    }

    // ===============================================================
    // 寻找地面（初始化/强制校准用）
    // ===============================================================
    void FindGroundPosition()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out var hit, 50f, groundLayer))
        {
            groundPosition = hit.point;
        }
        else
        {
            groundPosition = transform.position;
        }
    }

    // ===============================================================
    // 动画
    // ===============================================================
    void UpdateAnimations()
    {
        if (!animator) return;

        // 玩家控制的滑翔
        if (!isAIControlled)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && !isGrounded)
            {
                isGliding = true;
                animator.SetBool(glideParam, true);
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                isGliding = false;
                animator.SetBool(glideParam, false);
            }
        }

        if (isGrounded)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }
        else
        {
            animator.SetBool(flyParam, hasHorizontalInput || isAscending);
        }
    }

    void DebugAnimations()
    {
        if (!logAnimationState || animator == null) return;

        if (Time.frameCount % 30 == 0 || showRealTimeState) // 每30帧输出一次，或实时输出
        {
            bool flyingValue = animator.GetBool(flyParam);
            bool glidingValue = animator.GetBool(glideParam);
            string currentState = GetCurrentStateName();

            Debug.Log($"鸟动画状态: {currentState}, Flying={flyingValue}, Gliding={glidingValue}, Grounded={isGrounded}");
        }
    }

    // ===== 以下是辅助方法 =====

    string GetCurrentStateName()
    {
        if (animator == null) return "No Animator";

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Idle")) return "Idle";
        if (stateInfo.IsName("Fly")) return "Fly";
        if (stateInfo.IsName("Glide")) return "Glide";

        // 尝试通过哈希值判断
        if (stateInfo.fullPathHash == Animator.StringToHash("Base Layer.Idle")) return "Idle";
        if (stateInfo.fullPathHash == Animator.StringToHash("Base Layer.Fly")) return "Fly";
        if (stateInfo.fullPathHash == Animator.StringToHash("Base Layer.Glide")) return "Glide";

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
        CheckAndFixParameter(flyParam, "IsFlying", "Flying", "Fly", "isFlying");
        CheckAndFixParameter(glideParam, "IsGliding", "Gliding", "Glide", "isGliding");

        Debug.Log($"最终使用的参数: Fly={flyParam}, Glide={glideParam}");
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
                if (possibleNames[0].Contains("Fly"))
                    flyParam = name;
                else if (possibleNames[0].Contains("Glide"))
                    glideParam = name;

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
        if (birdModel == null) return;

        Vector3 modelEuler = birdModel.localRotation.eulerAngles;
        if (Mathf.Abs(modelEuler.x + 90) < 1f || Mathf.Abs(modelEuler.x - 270) < 1f)
        {
            if (showDebugInfo) Debug.Log($"检测到模型旋转问题: {modelEuler}");

            Quaternion fixedRotation = Quaternion.Euler(0, modelEuler.y, modelEuler.z);
            birdModel.localRotation = fixedRotation;
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
        float controllerBottom = controller != null ? controller.height / 2f - controller.radius : 0;
        Vector3 rayStart = transform.position + Vector3.down * controllerBottom + Vector3.up * 0.05f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * (groundCheckDistance + 0.2f));

        // 绘制移动方向
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            if (horizontalMoveDirection.magnitude > 0.1f)
            {
                Gizmos.DrawLine(transform.position, transform.position + horizontalMoveDirection.normalized * 2f);
            }
        }
    }

    // ===== 调试方法 =====
    [ContextMenu("测试：立即时间耗尽")]
    public void TestTimeExhausted()
    {
        possessionTimeRemaining = 0.1f; // 设置很少的时间
        Debug.Log("测试：鸟的附身时间设置为0.1秒");
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
}