using UnityEngine;

public class LeopardController : MonoBehaviour, IPossessable
{
    [Header("移动设置")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 8f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer = -1;

    [Header("动画参数")]
    public string speedParam = "Speed";
    public string isGroundedParam = "IsGrounded";
    public string jumpParam = "Jump";

    [Header("能力描述")]
    public string abilityDescription = "可以快速奔跑和跳跃的豹子";

    [Header("状态")]
    public bool isPossessed = false;
    private bool isGrounded = true;
    private bool isJumping = false;

    [Header("组件引用")]
    private Animator animator;
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;

    [Header("调试")]
    public bool showDebugInfo = true;

    // 移动相关
    private float currentSpeed = 0f;
    private Vector3 currentMovementInput = Vector3.zero;
    private float verticalVelocity = 0f;
    private const float GRAVITY = -9.81f;

    void Start()
    {
        // 获取组件引用
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            Debug.LogError("豹子控制器需要Animator组件！");
        }

        if (controller == null)
        {
            Debug.LogError("豹子控制器需要CharacterController组件！");
        }

        // 初始设置为待机状态
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
        }

        SetupCharacterController();
}

    void SetupCharacterController()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<CharacterController>();
            }
        }

        // 尝试找到模型渲染器来获取大小
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length > 0)
        {
            Bounds totalBounds = new Bounds(transform.position, Vector3.zero);

            foreach (MeshRenderer renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }

            // 将世界坐标转换为局部坐标
            Vector3 localCenter = transform.InverseTransformPoint(totalBounds.center);
            Vector3 localSize = totalBounds.size;

            // 调整CharacterController
            controller.center = new Vector3(0, localCenter.y, 0); // 保持X和Z为0
            controller.height = localSize.y * 0.8f; // 稍微小于模型高度
            controller.radius = Mathf.Max(localSize.x, localSize.z) * 0.5f * 0.7f; // 半径大约是宽度/深度的一半

            Debug.Log($"自动设置CharacterController:");
            Debug.Log($"  模型包围盒: 中心={localCenter}, 大小={localSize}");
            Debug.Log($"  Controller设置: Center={controller.center}, Height={controller.height}, Radius={controller.radius}");
        }
        else
        {
            // 如果没有渲染器，使用默认值
            controller.center = new Vector3(0, 1, 0);
            controller.height = 2f;
            controller.radius = 0.5f;
            Debug.LogWarning("没有找到MeshRenderer，使用默认CharacterController设置");
        }
    }

    void Update()
    {
        // 非附身状态下的逻辑（如果有的话）
        if (!isPossessed)
        {
            // 可以添加一些AI行为或空闲动画
            UpdateIdleAnimations();
            return;
        }
    }

    // ===== 实现 IPossessable 接口 =====

    public void OnPossess()
    {
        isPossessed = true;
        Debug.Log("豹子被附身了！");

        // 确保控制器启用
        if (controller != null)
        {
            controller.enabled = true;
        }
        else
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                Debug.LogError("豹子的CharacterController组件丢失！");
            }
        }

        // 设置初始动画状态
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
        }

        // 重置状态
        isJumping = false;
        currentSpeed = 0f;
        moveDirection = Vector3.zero;
    }

    public void OnRelease()
    {
        isPossessed = false;

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
        }

        // 重置移动方向
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        isJumping = false;

        Debug.Log("豹子脱离附身！");
    }

    public string GetAbilityDescription()
    {
        return abilityDescription;
    }

    public void PossessedUpdate()
    {
        if (!isPossessed) return;

        HandleMovement();
        UpdateAnimations();
        CheckGroundStatus();
    }
    // ===== 接口实现结束 =====

    void HandleMovement()
    {
        if (controller == null)
        {
            Debug.LogError("CharacterController为空！");
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetButtonDown("Jump");

        if (showDebugInfo)
        {
            Debug.Log($"豹子移动输入: H={horizontal:F2}, V={vertical:F2}, Sprint={sprint}, Jump={jumpPressed}");
        }

        // 基础移动方向（基于世界坐标）
        Vector3 move = new Vector3(horizontal, 0, vertical);

        // 获取相机方向（如果使用了相机控制器）
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 获取相机的前向和右向（忽略Y轴）
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;

            // 标准化
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 根据相机方向计算移动
            move = cameraForward * vertical + cameraRight * horizontal;
        }

        // 计算移动速度（考虑冲刺）
        float targetSpeed = 0f;
        if (move.magnitude > 0.1f)
        {
            if (sprint)
            {
                targetSpeed = runSpeed;
            }
            else
            {
                targetSpeed = walkSpeed;
            }
        }

        // 平滑速度变化
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        // 设置移动方向
        moveDirection = move * currentSpeed;

        // 处理跳跃
        if (jumpPressed && isGrounded && !isJumping)
        {
            Jump();
        }

        // 应用重力
        if (!isGrounded)
        {
            verticalVelocity += GRAVITY * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = -0.5f; // 轻微向下的力，确保贴地
        }

        // 应用移动
        Vector3 finalMove = new Vector3(moveDirection.x, verticalVelocity, moveDirection.z);
        controller.Move(finalMove * Time.deltaTime);

        // 旋转控制 - 只有在移动时才旋转
        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(move.x, 0, move.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Jump()
    {
        isJumping = true;
        verticalVelocity = Mathf.Sqrt(jumpForce * -2f * GRAVITY);

        if (animator != null)
        {
            animator.SetTrigger(jumpParam);
        }

        if (showDebugInfo) Debug.Log("豹子跳跃！");
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 更新速度参数
        animator.SetFloat(speedParam, currentSpeed);

        // 更新地面状态
        animator.SetBool(isGroundedParam, isGrounded);

        // 如果跳跃后落地，重置跳跃状态
        if (isGrounded && isJumping)
        {
            isJumping = false;
        }
    }

    void UpdateIdleAnimations()
    {
        // 非附身状态下的简单动画处理
        // 可以随机切换Idle1和Idle2，增加自然感
        if (animator != null && isGrounded)
        {
            // 这里可以添加一些随机的待机动画切换
        }
    }

    void CheckGroundStatus()
    {
        if (controller == null) return;

        // 使用CharacterController的isGrounded和射线检测双重检查
        bool wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        // 额外的射线检测确保准确性
        if (!isGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                isGrounded = true;
            }
        }

        // 状态变化时的额外处理
        if (wasGrounded != isGrounded)
        {
            if (isGrounded && showDebugInfo)
                Debug.Log("豹子已落地");
            else if (!isGrounded && showDebugInfo)
                Debug.Log("豹子已离地");
        }
    }

    // 添加一些辅助方法供外部调用
    public void ForceIdle()
    {
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
        }
        currentSpeed = 0f;
    }

    public void ForceRun()
    {
        if (animator != null)
        {
            animator.SetFloat(speedParam, runSpeed);
        }
        currentSpeed = runSpeed;
    }

    // 调试信息
    void OnDrawGizmosSelected()
    {
        if (showDebugInfo)
        {
            // 绘制地面检测线
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

            // 绘制移动方向
            if (Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + moveDirection.normalized * 2f);
            }
        }
    }

    // 确保接口实现正确
    void TestInterface()
    {
        Debug.Log($"这个对象实现了IPossessable: {this is IPossessable}");
        Debug.Log($"能力描述: {GetAbilityDescription()}");
    }
}