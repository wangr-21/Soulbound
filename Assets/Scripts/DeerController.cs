using UnityEngine;

public class DeerController : MonoBehaviour, IPossessable
{
    [Header("移动设置")]
    public float walkSpeed = 3f;
    public float runSpeed = 7f;
    public float rotationSpeed = 10f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.5f; // 增加检测距离
    public LayerMask groundLayer = -1;

    [Header("动画参数")]
    public string speedParam = "Speed";
    public string isGroundedParam = "IsGrounded";
    public string jumpParam = "Jump";

    [Header("能力描述")]
    public string abilityDescription = "可以在陆地上奔跑和跳跃的鹿";

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
    public float extraGravity = -5f; // 额外的向下的力

    [Header("调试")]
    public bool showDebugInfo = true;

    void Start()
    {
        // 获取组件引用
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            Debug.LogWarning("从子对象中未找到Animator，尝试从当前对象获取");
        }

        if (animator == null)
        {
            Debug.LogError("鹿控制器需要Animator组件！");
        }

        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            Debug.Log("自动添加CharacterController组件");

            // 设置Character Controller默认参数 - 重要！
            controller.height = 1.5f;      // 根据鹿的高度调整
            controller.radius = 0.3f;       // 根据鹿的宽度调整
            controller.center = new Vector3(0, 0.75f, 0); // 中心点高度是高度的一半
            controller.stepOffset = 0.3f;   // 可以跨越的高度
            controller.slopeLimit = 45f;    // 最大坡度
            controller.minMoveDistance = 0.001f; // 最小移动距离
        }
        else
        {
            // 检查现有Character Controller设置
            if (showDebugInfo)
            {
                Debug.Log($"Character Controller设置:");
                Debug.Log($"  Height: {controller.height}");
                Debug.Log($"  Radius: {controller.radius}");
                Debug.Log($"  Center: {controller.center}");
                Debug.Log($"  StepOffset: {controller.stepOffset}");
            }
        }

        // 初始设置为待机状态
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
        }

        // 强制调整到地面位置
        ForceGroundPosition();
    }

    void ForceGroundPosition()
    {
        // 使用射线检测找到地面位置
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out hit, 10f, groundLayer))
        {
            float targetY = hit.point.y + controller.height * 0.5f + controller.skinWidth;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

            if (showDebugInfo)
                Debug.Log($"强制调整鹿到地面: 从 {transform.position.y} 调整到 {targetY}");
        }
        else
        {
            // 如果没有检测到地面，向下移动直到碰撞
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f))
            {
                float targetY = hit.point.y + controller.height * 0.5f + controller.skinWidth;
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            }
        }

        // 确保垂直速度归零
        verticalVelocity = -0.5f; // 小值保持地面接触
    }

    // ===== 实现 IPossessable 接口 =====

    public void OnPossess()
    {
        // 记录附身前的位置和旋转
        Vector3 positionBefore = transform.position;
        Quaternion rotationBefore = transform.rotation;

        if (showDebugInfo)
            Debug.Log($"鹿被附身！位置: {positionBefore}, Y={positionBefore.y:F2}");

        isPossessed = true;

        // 重要：先禁用任何可能导致位置改变的组件
        if (animator != null)
        {
            // 禁用根运动，防止动画改变位置
            animator.applyRootMotion = false;
        }

        // 确保控制器启用
        if (controller != null)
        {
            // 记录启用前的状态
            bool wasEnabled = controller.enabled;

            if (!wasEnabled)
            {
                controller.enabled = true;

                // 检查启用后位置是否改变
                if (showDebugInfo && Vector3.Distance(transform.position, positionBefore) > 0.01f)
                {
                    Debug.LogWarning($"启用CharacterController后位置改变！从 {positionBefore.y:F2} 到 {transform.position.y:F2}");
                }
            }
        }

        // 设置动画状态 - 使用CrossFade避免突然的位置变化
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);

            // 使用CrossFade而不是Play，避免动画重置导致的位置变化
            animator.CrossFade("Idle", 0.2f);
        }

        // 重要：在附身后强制保持原位置和旋转
        transform.position = positionBefore;
        transform.rotation = rotationBefore;

        if (showDebugInfo)
            Debug.Log($"OnPossess完成，确保位置保持在Y={transform.position.y:F2}");
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
        verticalVelocity = -0.5f;
        isRunning = false;

        if (showDebugInfo) Debug.Log("鹿脱离附身！");
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

        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool jump = Input.GetButtonDown("Jump");

        // 改进的地面检测 - 每0.1秒检查一次，减少计算
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

            // 应用移动
            controller.Move(move.normalized * currentSpeed * Time.deltaTime);

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

                if (showDebugInfo) Debug.Log("鹿跳跃！");
            }
        }

        // 垂直移动
        Vector3 verticalMove = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;

        // 应用所有移动
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
        if (showDebugInfo && Time.frameCount % 60 == 0) // 每秒一次
        {
            Debug.Log($"鹿状态: 在地面={isGrounded}, 垂直速度={verticalVelocity:F2}, 控制器在地面={controller.isGrounded}");
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
                    Debug.Log($"调整鹿位置确保地面接触: {adjustY}");
            }
        }
    }

    // 调试信息
    void OnDrawGizmosSelected()
    {
        if (showDebugInfo)
        {
            // 绘制地面检测线
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(
                transform.position + Vector3.up * 0.1f,
                transform.position + Vector3.up * 0.1f + Vector3.down * groundCheckDistance
            );

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
        }
    }

    /*    // 临时调试UI
        void OnGUI()
        {
            if (showDebugInfo)
            {
                GUI.Label(new Rect(10, 50, 300, 20), $"鹿在地面: {isGrounded}");
                GUI.Label(new Rect(10, 70, 300, 20), $"垂直速度: {verticalVelocity:F2}");
                GUI.Label(new Rect(10, 90, 300, 20), $"控制器在地面: {controller?.isGrounded}");
                GUI.Label(new Rect(10, 110, 300, 20), $"位置Y: {transform.position.y:F2}");
            }
        }*/
}