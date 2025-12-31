using UnityEngine;

public class SheepController : MonoBehaviour, IPossessable
{
    [Header("移动设置")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 8f;
    public float jumpForce = 5f;
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
    public float extraGravity = -2f;

    [Header("调试")]
    public bool showDebugInfo = true;

    // 动画过渡相关
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private bool isRunning = false;

    // 跳跃相关 - 修复版
    private bool isJumping = false;
    private float jumpCooldown = 0.2f;
    private float jumpCooldownTimer = 0f;
    private float lastJumpTime = -1f;
    private float coyoteTime = 0.15f; // Coyote Time缓冲时间
    private float coyoteTimeCounter = 0f;
    private float jumpBufferTime = 0.15f; // 跳跃输入缓冲时间
    private float jumpBufferTimer = 0f;
    private bool jumpRequested = false; // 标记是否有跳跃请求

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
            SetupCharacterController();
        }
        else
        {
            ValidateCharacterControllerSettings();
        }

        // 初始化动画状态
        InitializeAnimation();

        // 确保位置正确
        ForceGroundPosition();
    }

    void SetupCharacterController()
    {
        controller.height = 1.0f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0, 0.5f, 0);
        controller.stepOffset = 0.2f;
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

        animator.applyRootMotion = false;
        animator.SetFloat(speedParam, 0f);
        animator.SetBool(isGroundParam, true);

        // 检查动画参数是否存在
        CheckAnimatorParameters();
    }

    void CheckAnimatorParameters()
    {
        if (animator == null) return;

        bool hasSpeedParam = false;
        bool hasGroundParam = false;
        bool hasJumpParam = false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == speedParam) hasSpeedParam = true;
            if (param.name == isGroundParam) hasGroundParam = true;
            if (param.name == jumpParam) hasJumpParam = true;
        }

        if (showDebugInfo)
        {
            Debug.Log($"动画参数检查:");
            Debug.Log($"  {speedParam}: {hasSpeedParam}");
            Debug.Log($"  {isGroundParam}: {hasGroundParam}");
            Debug.Log($"  {jumpParam}: {hasJumpParam}");
        }
    }

    void ForceGroundPosition()
    {
        RaycastHit hit;
        float raycastDistance = 5f;

        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            float targetY = hit.point.y + controller.height * 0.5f + controller.skinWidth;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

            if (showDebugInfo)
                Debug.Log($"强制调整绵羊到地面: Y={targetY}");
        }

        verticalVelocity = -0.5f;
    }

    // ===== 实现 IPossessable 接口 =====

    public void OnPossess()
    {
        if (showDebugInfo)
            Debug.Log($"绵羊被附身！位置: Y={transform.position.y:F2}");

        isPossessed = true;

        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundParam, true);
        }

        // 重置状态
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        targetSpeed = 0f;
        isRunning = false;
        isJumping = false;
        jumpCooldownTimer = 0f;
        coyoteTimeCounter = coyoteTime;
        jumpBufferTimer = 0f;
        jumpRequested = false;
    }

    public void OnRelease()
    {
        isPossessed = false;

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundParam, true);
        }

        // 重置移动
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        targetSpeed = 0f;
        verticalVelocity = -0.5f;
        isRunning = false;
        isJumping = false;
        jumpCooldownTimer = 0f;
        jumpBufferTimer = 0f;
        jumpRequested = false;

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

        // 更新计时器
        UpdateTimers();

        // 检测跳跃输入
        CheckJumpInput();

        HandleMovement();
        UpdateAnimations();
        CheckGroundStatus();
    }
    // ===== 接口实现结束 =====

    void UpdateTimers()
    {
        // 更新跳跃冷却计时
        if (jumpCooldownTimer > 0)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        // 更新跳跃缓冲计时
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // 更新Coyote Time
        if (isGrounded && !isJumping)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else if (!isGrounded && coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    void CheckJumpInput()
    {
        // 检测跳跃输入，使用缓冲系统
        if (Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
            jumpBufferTimer = jumpBufferTime; // 开始缓冲计时
            if (showDebugInfo) Debug.Log("跳跃输入检测到，开始缓冲");
        }
    }

    void HandleMovement()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool runInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 地面检测
        groundCheckTimer -= Time.deltaTime;
        if (groundCheckTimer <= 0)
        {
            CheckGroundStatus();
            groundCheckTimer = 0.1f;
        }

        // 计算移动方向
        Vector3 move = new Vector3(horizontal, 0, vertical);

        // 基于相机方向转换
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
            }

            // 确定速度和是否跑步 - 修复跑步检测逻辑
            isRunning = runInput && move.magnitude > 0.1f; // 降低阈值
            targetSpeed = isRunning ? runSpeed : walkSpeed;

            // 应用移动
            controller.Move(move.normalized * targetSpeed * Time.deltaTime);

            // 旋转朝向移动方向
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

        // 跳跃处理 - 使用缓冲系统
        if (jumpRequested && jumpCooldownTimer <= 0 && CanJump())
        {
            PerformJump();
        }

        // 重力处理
        ApplyGravity();
    }

    bool CanJump()
    {
        // 检查是否可以跳跃：在地面、Coyote Time内或跳跃缓冲期内
        return (isGrounded || coyoteTimeCounter > 0) && jumpBufferTimer > 0;
    }

    void PerformJump()
    {
        verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        isJumping = true;
        jumpCooldownTimer = jumpCooldown;
        coyoteTimeCounter = 0f;
        jumpBufferTimer = 0f;
        jumpRequested = false;

        // 触发跳跃动画
        if (animator != null)
        {
            // 检查跳跃参数是否存在
            bool hasJumpParam = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == jumpParam)
                {
                    hasJumpParam = true;
                    break;
                }
            }

            if (hasJumpParam)
            {
                animator.SetTrigger(jumpParam);
                if (showDebugInfo) Debug.Log($"触发跳跃动画: {jumpParam}");
            }
            else
            {
                // 如果没有跳跃参数，通过Speed参数模拟
                animator.SetFloat(speedParam, 1.0f);
                if (showDebugInfo) Debug.Log("没有跳跃参数，使用Speed参数模拟跳跃");
            }
        }

        if (showDebugInfo)
            Debug.Log($"绵羊跳跃！垂直速度: {verticalVelocity:F2}, 缓冲时间: {jumpBufferTimer:F2}");
    }

    void ApplyGravity()
    {
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
            verticalVelocity = Mathf.Max(verticalVelocity, -0.5f);

            // 如果在地面，重置跳跃状态
            if (isJumping && verticalVelocity <= 0)
            {
                isJumping = false;
            }
        }

        // 应用垂直移动
        Vector3 verticalMove = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        controller.Move(verticalMove);
    }

    void UpdateAnimations()
    {
        if (animator == null)
            return;

        // 更新速度参数 - 修复跑步动画触发逻辑
        float animSpeedValue = 0f;

        if (currentSpeed > 0)
        {
            if (isRunning && currentSpeed >= walkSpeed * 1.1f) // 降低跑步触发阈值
            {
                // 跑步状态：直接映射到0.8-1.0
                animSpeedValue = Mathf.Clamp(currentSpeed / runSpeed, 0.8f, 1.0f);

                if (showDebugInfo && Time.frameCount % 120 == 0)
                    Debug.Log($"跑步状态: 速度={currentSpeed:F2}, 动画值={animSpeedValue:F2}, isRunning={isRunning}");
            }
            else
            {
                // 行走状态：映射到0.1-0.7
                animSpeedValue = Mathf.Clamp(currentSpeed / walkSpeed, 0.1f, 0.7f);
            }
        }

        animator.SetFloat(speedParam, animSpeedValue);

        // 更新地面状态
        animator.SetBool(isGroundParam, isGrounded);

        // 调试信息
        if (showDebugInfo && Time.frameCount % 120 == 0)
        {
            Debug.Log($"绵羊动画状态: Speed={animSpeedValue:F2}, isGrounded={isGrounded}, " +
                     $"isRunning={isRunning}, CoyoteTime={coyoteTimeCounter:F2}, " +
                     $"JumpBuff={jumpBufferTimer:F2}, JumpReq={jumpRequested}");
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
            controller.radius * 0.8f, // 稍微小一点
            Vector3.down,
            out hit,
            groundCheckDistance + 0.1f, // 增加检测距离
            groundLayer
        );

        // 综合判断
        bool newGrounded = controllerGrounded || raycastGrounded || sphereCastGrounded;

        // 处理落地逻辑
        if (newGrounded && !isGrounded)
        {
            isJumping = false;
            coyoteTimeCounter = coyoteTime;
            if (showDebugInfo) Debug.Log("绵羊落地");
        }

        // 处理离地逻辑
        if (!newGrounded && isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            if (showDebugInfo) Debug.Log("绵羊离地，开始Coyote Time");
        }

        isGrounded = newGrounded;

        // 如果检测到地面，确保正确接触
        if (isGrounded && hit.collider != null)
        {
            float groundHeight = hit.point.y;
            float currentBottom = transform.position.y - controller.height * 0.5f;

            if (currentBottom > groundHeight + 0.05f)
            {
                float adjustY = groundHeight + controller.height * 0.5f + controller.skinWidth;
                transform.position = new Vector3(transform.position.x, adjustY, transform.position.z);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || controller == null)
            return;

        // 地面检测线
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayStart = transform.position + controller.center;
        float rayLength = (controller.height * 0.5f) + groundCheckDistance;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * rayLength);

        // Coyote Time指示器
        if (coyoteTimeCounter > 0 && !isGrounded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        // CharacterController范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rayStart, controller.radius);
    }
}