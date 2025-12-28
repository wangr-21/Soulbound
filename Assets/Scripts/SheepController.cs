using UnityEngine;

public class SheepController : MonoBehaviour, IPossessable
{
    [Header("移动设置")]
    public float walkSpeed = 2f;           // 绵羊走得慢一点
    public float runSpeed = 4f;            // 绵羊跑得也慢一点
    public float rotationSpeed = 8f;       // 旋转速度
    public float jumpForce = 5f;           // 跳跃力
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer = -1;

    [Header("动画参数")]
    public string speedParam = "Speed";
    public string isGroundParam = "isGround";
    public string jumpParam = "Jump";

    [Header("动画状态名称")]
    public string idleStateName = "Idle";
    public string walkStateName = "Walk";
    public string runStateName = "Run";

    [Header("能力描述")]
    public string abilityDescription = "温顺的绵羊，可以行走和奔跑";

    [Header("状态")]
    public bool isPossessed = false;
    private bool isGrounded = true;

    [Header("组件引用")]
    private Animator animator;
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float verticalVelocity;
    private float groundCheckTimer = 0f;

    [Header("重力设置")]
    public float gravity = -9.81f;
    public float extraGravity = -2f; // 绵羊不需要太大的额外重力

    [Header("调试")]
    public bool showDebugInfo = true;

    // 动画过渡相关
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private bool isRunning = false;

    // 跳跃相关
    private bool isJumping = false;
    private float jumpCooldown = 0.2f;
    private float jumpCooldownTimer = 0f;
    private float lastJumpTime = -1f;
    private float coyoteTime = 0.1f; // 离地后还能跳跃的短暂时间
    private float coyoteTimeCounter = 0f;

    void Start()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        // 获取组件引用
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            Debug.LogWarning("从子对象中未找到Animator，尝试从当前对象获取");
        }

        // 添加或获取CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            Debug.Log("自动添加CharacterController组件");

            // 根据绵羊大小调整CharacterController参数
            SetupCharacterController();
        }
        else
        {
            // 验证现有设置
            ValidateCharacterControllerSettings();
        }

        // 初始化动画状态
        InitializeAnimation();

        // 确保位置正确
        ForceGroundPosition();
    }

    void SetupCharacterController()
    {
        // 绵羊的CharacterController设置（根据模型大小调整）
        // 建议：先运行一次游戏，查看绵羊大小，然后调整这些值
        controller.height = 1.0f;      // 绵羊高度约1米
        controller.radius = 0.3f;       // 绵羊宽度
        controller.center = new Vector3(0, 0.5f, 0); // 中心点高度是高度的一半
        controller.stepOffset = 0.2f;   // 可以跨越的高度
        controller.slopeLimit = 45f;
        controller.minMoveDistance = 0.001f;
        controller.skinWidth = 0.01f;
    }

    void ValidateCharacterControllerSettings()
    {
        if (showDebugInfo)
        {
            Debug.Log($"绵羊Character Controller设置:");
            Debug.Log($"  Height: {controller.height}");
            Debug.Log($"  Radius: {controller.radius}");
            Debug.Log($"  Center: {controller.center}");
            Debug.Log($"  StepOffset: {controller.stepOffset}");
            Debug.Log($"  SkinWidth: {controller.skinWidth}");
        }

        // 确保有足够的skinWidth
        if (controller.skinWidth < 0.01f)
        {
            controller.skinWidth = 0.01f;
            Debug.Log("调整CharacterController的skinWidth为0.01f");
        }
    }

    void InitializeAnimation()
    {
        if (animator == null)
        {
            Debug.LogError("绵羊控制器需要Animator组件！");
            return;
        }

        // 确保应用根运动被禁用
        animator.applyRootMotion = false;

        // 设置初始动画参数
        animator.SetFloat(speedParam, 0f);
        animator.SetBool(isGroundParam, true);
    }

    void ForceGroundPosition()
    {
        // 使用射线检测找到地面位置
        RaycastHit hit;
        float raycastDistance = 5f;

        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            float targetY = hit.point.y + controller.height * 0.5f + controller.skinWidth;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

            if (showDebugInfo)
                Debug.Log($"强制调整绵羊到地面: Y={targetY}");
        }
        else
        {
            // 如果没检测到地面，尝试向下移动
            Debug.LogWarning("没有检测到地面，尝试向下寻找");
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f))
            {
                float targetY = hit.point.y + controller.height * 0.5f + controller.skinWidth;
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            }
        }

        // 重置垂直速度
        verticalVelocity = -0.5f;
    }

    // ===== 实现 IPossessable 接口 =====

    public void OnPossess()
    {
        if (showDebugInfo)
            Debug.Log($"绵羊被附身！位置: Y={transform.position.y:F2}");

        isPossessed = true;

        // 确保组件启用
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }

        // 确保禁用根运动
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // 设置动画到待机状态
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundParam, true);

            // 如果有特定的待机动画，使用CrossFade平滑过渡
            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.CrossFade(idleStateName, 0.2f);
            }
        }

        // 重置状态
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        targetSpeed = 0f;
        isRunning = false;
        isJumping = false;
        jumpCooldownTimer = 0f;
    }

    public void OnRelease()
    {
        isPossessed = false;

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundParam, true);

            // 回到待机状态
            if (!string.IsNullOrEmpty(idleStateName))
            {
                animator.CrossFade(idleStateName, 0.3f);
            }
        }

        // 重置移动
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        targetSpeed = 0f;
        verticalVelocity = -0.5f;
        isRunning = false;
        isJumping = false;
        jumpCooldownTimer = 0f;

        if (showDebugInfo)
            Debug.Log("绵羊脱离附身！");
    }

    public string GetAbilityDescription()
    {
        return abilityDescription;
    }

    public void PossessedUpdate()
    {
        if (!isPossessed || controller == null)
            return;

        HandleMovement();
        UpdateAnimations();
        CheckGroundStatus();

        // 更新Coyote Time
        UpdateCoyoteTime();
    }
    // ===== 接口实现结束 =====

    void HandleMovement()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool jumpInput = Input.GetButtonDown("Jump");
        bool runInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 地面检测（优化性能）
        groundCheckTimer -= Time.deltaTime;
        if (groundCheckTimer <= 0)
        {
            CheckGroundStatus();
            groundCheckTimer = 0.1f;
        }

        // 计算移动方向（基于相机）
        Vector3 move = new Vector3(horizontal, 0, vertical).normalized;

        // 如果有输入，则基于相机方向转换
        if (move.magnitude > 0.1f)
        {
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
                move = move.normalized;
            }

            // 确定速度和是否跑步
            isRunning = runInput && move.magnitude > 0.3f;
            targetSpeed = isRunning ? runSpeed : walkSpeed;

            // 应用移动
            Vector3 horizontalMove = move * targetSpeed;
            controller.Move(horizontalMove * Time.deltaTime);

            // 平滑旋转朝向移动方向
            if (move != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // 没有输入时减速
            isRunning = false;
            targetSpeed = 0f;
        }

        // 平滑速度变化（用于动画）
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        // 重力处理
        ApplyGravity(jumpInput);
    }

    void ApplyGravity(bool jumpInput)
    {
        // 跳跃冷却计时
        if (jumpCooldownTimer > 0)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        if (!isGrounded)
        {
            // 在空中时应用重力
            verticalVelocity += gravity * Time.deltaTime;

            // 额外的向下力
            if (verticalVelocity < 0)
            {
                verticalVelocity += extraGravity * Time.deltaTime;
            }
        }
        else
        {
            // 在地面时，施加一个小的向下力保持地面接触
            verticalVelocity = -0.5f;

            // 跳跃 - 允许Coyote Time内跳跃
            if (jumpInput && jumpForce > 0 && jumpCooldownTimer <= 0 &&
                (isGrounded || coyoteTimeCounter > 0))
            {
                verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
                isJumping = true;
                jumpCooldownTimer = jumpCooldown;
                lastJumpTime = Time.time;
                coyoteTimeCounter = 0f; // 使用Coyote Time后重置

                if (animator != null && !string.IsNullOrEmpty(jumpParam))
                {
                    animator.SetTrigger(jumpParam);
                }

                if (showDebugInfo)
                    Debug.Log("绵羊跳跃！垂直速度: " + verticalVelocity);
            }
        }

        // 应用垂直移动
        Vector3 verticalMove = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        controller.Move(verticalMove);
    }

    void UpdateCoyoteTime()
    {
        // 更新Coyote Time计数器
        if (isGrounded && !isJumping)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else if (!isGrounded && coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 如果刚刚离开地面一小段时间，仍可以跳跃
        if (coyoteTimeCounter <= 0 && !isGrounded)
        {
            isJumping = true;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null)
            return;

        // 更新速度参数 - 修复跑步动画问题
        // 这里需要根据当前速度映射到动画参数
        float animSpeedValue = 0f;

        if (currentSpeed > 0)
        {
            if (isRunning && currentSpeed >= runSpeed * 0.8f)
            {
                // 跑步状态：映射到0.7-1.0范围
                animSpeedValue = Mathf.Lerp(0.7f, 1.0f, (currentSpeed - walkSpeed) / (runSpeed - walkSpeed));
                animSpeedValue = Mathf.Clamp(animSpeedValue, 0.7f, 1.0f);

                if (showDebugInfo && Time.frameCount % 60 == 0)
                    Debug.Log($"跑步状态: 速度={currentSpeed:F2}, 动画值={animSpeedValue:F2}");
            }
            else
            {
                // 行走状态：映射到0.1-0.6范围
                animSpeedValue = Mathf.Lerp(0.1f, 0.6f, currentSpeed / walkSpeed);
                animSpeedValue = Mathf.Clamp(animSpeedValue, 0.1f, 0.6f);
            }
        }

        animator.SetFloat(speedParam, animSpeedValue);

        // 更新地面状态
        animator.SetBool(isGroundParam, isGrounded);

        // 调试信息
        if (showDebugInfo && Time.frameCount % 120 == 0)
        {
            Debug.Log($"绵羊状态: Speed={currentSpeed:F2}, isGrounded={isGrounded}, isRunning={isRunning}, CoyoteTime={coyoteTimeCounter:F2}");
        }
    }

    void CheckGroundStatus()
    {
        if (controller == null)
            return;

        // 方法1: 使用CharacterController的isGrounded
        bool controllerGrounded = controller.isGrounded;

        // 方法2: 使用射线检测
        RaycastHit hit;
        Vector3 rayStart = transform.position + controller.center;
        float rayLength = (controller.height * 0.5f) + groundCheckDistance;

        bool raycastGrounded = Physics.Raycast(
            rayStart,
            Vector3.down,
            out hit,
            rayLength,
            groundLayer
        );

        // 方法3: 使用球体检测（更可靠）
        bool sphereCastGrounded = Physics.SphereCast(
            transform.position + Vector3.up * 0.2f,
            controller.radius * 0.9f,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer
        );

        // 综合判断 - 只要有一种方法检测到地面就算在地面
        bool newGrounded = controllerGrounded || raycastGrounded || sphereCastGrounded;

        // 如果从空中落地，重置跳跃状态
        if (newGrounded && !isGrounded)
        {
            isJumping = false;
            coyoteTimeCounter = coyoteTime; // 重置Coyote Time
        }

        isGrounded = newGrounded;

        // 如果检测到地面，确保正确接触
        if (isGrounded && hit.collider != null)
        {
            float groundHeight = hit.point.y;
            float currentBottom = transform.position.y - controller.height * 0.5f;

            // 如果底部高于地面一点，调整位置
            if (currentBottom > groundHeight + 0.1f) // 增加容差
            {
                float adjustY = groundHeight + controller.height * 0.5f + controller.skinWidth;
                transform.position = new Vector3(transform.position.x, adjustY, transform.position.z);

                if (showDebugInfo)
                    Debug.Log($"调整绵羊位置确保地面接触: {adjustY}");
            }
        }
    }

    // 调试可视化
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || controller == null)
            return;

        // 地面检测线
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayStart = transform.position + controller.center;
        float rayLength = (controller.height * 0.5f) + groundCheckDistance;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * rayLength);

        // CharacterController范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rayStart, controller.radius);

        // 移动方向
        if (Application.isPlaying && moveDirection.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + moveDirection.normalized * 2f);
        }
    }
}