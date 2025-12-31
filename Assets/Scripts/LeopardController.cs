using UnityEngine;

public class LeopardController : MonoBehaviour, IPossessable
{
    [Header("移动设置")]
    public float walkSpeed = 3f;
    public float runSpeed = 7f;
    public float rotationSpeed = 10f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.5f;
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
    private bool isRunning = false;

    [Header("组件引用")]
    private Animator animator;
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float verticalVelocity;
    private float groundCheckTimer = 0f;

    [Header("重力设置")]
    public float gravity = -9.81f;
    public float extraGravity = -5f;

    [Header("碰撞体设置")]
    public bool addCapsuleCollider = true; // 是否添加用于检测的碰撞体
    private CapsuleCollider detectionCollider;

    [Header("调试")]
    public bool showDebugInfo = true;

    void Start()
    {
        if (showDebugInfo)
            Debug.Log($"=== 豹子控制器初始化开始: {gameObject.name} ===");

        // 获取组件引用
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (showDebugInfo) Debug.LogWarning("从子对象中未找到Animator，尝试从当前对象获取");
        }

        if (animator == null)
        {
            Debug.LogError("豹子控制器需要Animator组件！");
        }

        // 确保有CharacterController
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            if (showDebugInfo) Debug.Log("自动添加CharacterController组件");

            // 设置Character Controller默认参数
            controller.height = 1.5f;
            controller.radius = 0.4f; // 豹子比鹿宽一点
            controller.center = new Vector3(0, 0.75f, 0);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;
            controller.minMoveDistance = 0.001f;
            controller.skinWidth = 0.08f;
        }
        else
        {
            // 确保CharacterController设置正确
            controller.height = Mathf.Max(controller.height, 1.0f);
            controller.radius = Mathf.Max(controller.radius, 0.3f);
            controller.center = new Vector3(0, controller.height * 0.5f, 0);
        }

        // 添加或配置用于检测的碰撞体
        SetupDetectionCollider();

        // 初始动画状态
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
        }

        // 强制调整到地面位置
        ForceGroundPosition();

        if (showDebugInfo)
        {
            Debug.Log($"豹子初始化完成:");
            Debug.Log($"  CharacterController: {controller != null}");
            Debug.Log($"  Animator: {animator != null}");
            Debug.Log($"  检测碰撞体: {detectionCollider != null}");
            Debug.Log($"  位置Y: {transform.position.y:F2}");
        }
    }

    void SetupDetectionCollider()
    {
        // 检查是否已经有合适的碰撞体
        detectionCollider = GetComponent<CapsuleCollider>();

        if (detectionCollider == null && addCapsuleCollider)
        {
            // 添加用于检测的碰撞体
            detectionCollider = gameObject.AddComponent<CapsuleCollider>();

            // 设置碰撞体参数
            if (controller != null)
            {
                // 使用与CharacterController相似的参数
                detectionCollider.center = controller.center;
                detectionCollider.height = controller.height;
                detectionCollider.radius = controller.radius + 0.1f; // 稍微大一点，便于检测
            }
            else
            {
                // 默认参数
                detectionCollider.center = new Vector3(0, 1f, 0);
                detectionCollider.height = 2f;
                detectionCollider.radius = 0.5f;
            }

            detectionCollider.isTrigger = false; // 必须是非触发器才能被Physics检测到

            if (showDebugInfo)
                Debug.Log("添加用于检测的CapsuleCollider");
        }
        else if (detectionCollider != null)
        {
            // 确保现有的碰撞体不是触发器
            detectionCollider.isTrigger = false;

            if (showDebugInfo)
                Debug.Log("使用现有的CapsuleCollider进行检测");
        }
    }

    void ForceGroundPosition()
    {
        if (controller == null) return;

        // 使用射线检测找到地面位置
        RaycastHit hit;
        Vector3 raycastStart = transform.position + Vector3.up * 2f; // 从较高的位置开始

        if (Physics.Raycast(raycastStart, Vector3.down, out hit, 10f, groundLayer))
        {
            float targetY = hit.point.y + controller.height * 0.5f + controller.skinWidth;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

            if (showDebugInfo)
                Debug.Log($"强制调整豹子到地面: 从 {transform.position.y} 调整到 {targetY}");
        }
        else
        {
            // 如果没有检测到地面，使用CharacterController的碰撞检测
            if (showDebugInfo)
                Debug.LogWarning("无法检测到地面，尝试使用CharacterController碰撞");

            // 向下移动直到碰撞
            Vector3 downMove = Vector3.down * 20f;
            if (controller.Move(downMove * Time.deltaTime) != CollisionFlags.None)
            {
                // 碰撞后稍微向上调整
                transform.position += Vector3.up * 0.1f;
            }
        }

        // 确保垂直速度归零
        verticalVelocity = -0.5f; // 小值保持地面接触
    }

    // ===== 实现 IPossessable 接口 =====

    public void OnPossess()
    {
        if (showDebugInfo)
            Debug.Log($"豹子OnPossess开始 - 当前Y位置: {transform.position.y:F2}");

        // 记录附身前的位置和旋转
        Vector3 positionBefore = transform.position;
        Quaternion rotationBefore = transform.rotation;

        isPossessed = true;

        // 重要：确保控制器启用
        if (controller != null)
        {
            // 记录启用前的状态
            bool wasEnabled = controller.enabled;

            if (!wasEnabled)
            {
                controller.enabled = true;

                if (showDebugInfo)
                    Debug.Log($"启用CharacterController，之前状态: {wasEnabled}");
            }
        }

        // 禁用动画根运动
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // 设置动画状态
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
            animator.CrossFade("Idle", 0.2f); // 使用CrossFade避免突然的位置变化
        }

        // 重要：在附身后强制保持原位置和旋转
        transform.position = positionBefore;
        transform.rotation = rotationBefore;

        if (showDebugInfo)
        {
            Debug.Log($"豹子被附身！");
            Debug.Log($"  最终位置Y: {transform.position.y:F2}");
            Debug.Log($"  控制器启用: {controller != null && controller.enabled}");
            Debug.Log($"  动画器: {animator != null}");
        }
    }

    public void OnRelease()
    {
        isPossessed = false;

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
        }

        // 重置移动状态
        moveDirection = Vector3.zero;
        verticalVelocity = -0.5f;
        isRunning = false;

        if (showDebugInfo)
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
        if (controller == null || !controller.enabled)
        {
            Debug.LogError("CharacterController为空或未启用！");
            return;
        }

        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool jump = Input.GetButtonDown("Jump");

        // 改进的地面检测 - 每0.1秒检查一次
        groundCheckTimer -= Time.deltaTime;
        if (groundCheckTimer <= 0)
        {
            CheckGroundStatus();
            groundCheckTimer = 0.1f;
        }

        // 基础移动方向
        Vector3 move = new Vector3(horizontal, 0, vertical);

        // 获取相机方向（第三人称视角）
        Camera mainCamera = Camera.main;
        if (mainCamera != null && move.magnitude > 0.1f)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            move = cameraForward * vertical + cameraRight * horizontal;
        }

        // 计算移动速度
        float currentSpeed = 0f;

        if (move.magnitude > 0.1f)
        {
            // 检查是否在跑步
            isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            // 水平移动
            Vector3 horizontalMove = move.normalized * currentSpeed * Time.deltaTime;
            controller.Move(horizontalMove);

            // 旋转朝向移动方向
            if (move != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            isRunning = false;
            currentSpeed = 0f;
        }

        // 重力计算
        if (!isGrounded)
        {
            // 在空中时应用重力
            verticalVelocity += gravity * Time.deltaTime;

            // 额外的向下力，确保尽快落地
            if (verticalVelocity < 0)
            {
                verticalVelocity += extraGravity * Time.deltaTime;
            }
        }
        else
        {
            // 在地面时，施加一个小的向下力保持地面接触
            verticalVelocity = -0.5f;

            // 跳跃
            if (jump)
            {
                verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
                if (animator != null && !string.IsNullOrEmpty(jumpParam))
                {
                    animator.SetTrigger(jumpParam);
                }

                if (showDebugInfo)
                    Debug.Log("豹子跳跃！");
            }
        }

        // 垂直移动
        Vector3 verticalMove = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        controller.Move(verticalMove);

        // 更新动画速度参数
        if (animator != null)
        {
            float animSpeed = Mathf.Lerp(animator.GetFloat(speedParam), currentSpeed, Time.deltaTime * 5f);
            animator.SetFloat(speedParam, animSpeed);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 更新地面状态
        animator.SetBool(isGroundedParam, isGrounded);

        // 调试信息
        if (showDebugInfo && Time.frameCount % 120 == 0) // 每2秒一次
        {
            Debug.Log($"豹子状态: 在地面={isGrounded}, 垂直速度={verticalVelocity:F2}, 速度={animator.GetFloat(speedParam):F2}");
        }
    }

    void CheckGroundStatus()
    {
        if (controller == null) return;

        // 方法1: 使用CharacterController的isGrounded
        bool controllerGrounded = controller.isGrounded;

        // 方法2: 使用射线检测
        RaycastHit hit;
        bool raycastGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            out hit,
            groundCheckDistance,
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

        // 综合判断
        isGrounded = controllerGrounded || raycastGrounded || sphereCastGrounded;

        // 如果检测到地面，调整位置确保接触
        if (isGrounded && hit.collider != null)
        {
            // 轻微调整位置确保接触
            float groundHeight = hit.point.y;
            float currentBottom = transform.position.y - controller.height * 0.5f;

            if (currentBottom > groundHeight + 0.05f) // 如果底部高于地面
            {
                float adjustY = groundHeight + controller.height * 0.5f + controller.skinWidth;
                transform.position = new Vector3(transform.position.x, adjustY, transform.position.z);

                if (showDebugInfo)
                    Debug.Log($"调整豹子位置确保地面接触: {adjustY}");
            }
        }
    }

    void Update()
    {
        // 如果不是被附身状态，可以添加一些AI行为
        if (!isPossessed)
        {
            UpdateIdleBehavior();
        }
    }

    void UpdateIdleBehavior()
    {
        // 这里可以添加一些空闲时的行为，比如随机移动或动画
        if (animator != null && isGrounded)
        {
            // 可以随机播放一些空闲动画
            // 例如：animator.SetTrigger("IdleBlink");
        }
    }

    // 调试信息
    void OnDrawGizmosSelected()
    {
        if (showDebugInfo)
        {
            // 绘制地面检测线
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            Vector3 rayEnd = rayStart + Vector3.down * groundCheckDistance;
            Gizmos.DrawLine(rayStart, rayEnd);
            Gizmos.DrawSphere(rayEnd, 0.1f);

            // 绘制Character Controller范围
            Gizmos.color = Color.cyan;
            if (controller != null)
            {
                Vector3 center = transform.position + controller.center;
                Gizmos.DrawWireSphere(center, controller.radius);
                Gizmos.DrawLine(
                    center + Vector3.up * (controller.height * 0.5f - controller.radius),
                    center + Vector3.down * (controller.height * 0.5f - controller.radius)
                );
            }

            // 绘制移动方向
            if (Application.isPlaying && moveDirection.magnitude > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + moveDirection.normalized * 2f);
            }

            // 绘制检测碰撞体
            if (detectionCollider != null)
            {
                Gizmos.color = new Color(0, 1, 1, 0.3f);
                Vector3 colliderCenter = transform.TransformPoint(detectionCollider.center);
                Gizmos.DrawWireSphere(colliderCenter + Vector3.up * (detectionCollider.height * 0.5f - detectionCollider.radius), detectionCollider.radius);
                Gizmos.DrawWireSphere(colliderCenter - Vector3.up * (detectionCollider.height * 0.5f - detectionCollider.radius), detectionCollider.radius);
            }
        }
    }
}