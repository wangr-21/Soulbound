using UnityEngine;

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
    public Transform foxModel; // 指向实际显示模型的Transform
    public bool fixModelRotation = true; // 是否修复模型旋转

    [Header("动画参数")]
    public string speedParam = "Speed";
    public string isGroundedParam = "IsGrounded";
    public string jumpParam = "Jump";

    [Header("能力描述")]
    public string abilityDescription = "敏捷的狐狸，可以行走、奔跑和跳跃";

    [Header("状态")]
    public bool isPossessed = false;
    private bool isGrounded = true;
    private float currentSpeed = 0f;
    private Vector3 moveDirection = Vector3.zero;
    private bool jumpTriggered = false;

    [Header("组件引用")]
    private Animator animator;
    private CharacterController controller;

    [Header("调试")]
    public bool showDebugInfo = true;
    public bool drawDebugGizmos = true;

    // 记录附身前的状态
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Quaternion modelOriginalRotation;
    private bool controllerWasEnabled = true;

    void Start()
    {
        InitializeComponents();
        TestInterface();

        if (showDebugInfo)
        {
            Debug.Log($"狐狸({gameObject.name})初始化完成:");
            Debug.Log($"- 控制器位置: {transform.position}");
            Debug.Log($"- 控制器旋转: {transform.rotation.eulerAngles}");
            Debug.Log($"- 模型位置: {(foxModel != null ? foxModel.position.ToString() : "N/A")}");
            Debug.Log($"- 模型旋转: {(foxModel != null ? foxModel.rotation.eulerAngles.ToString() : "N/A")}");
            Debug.Log($"- 组件: Animator={animator != null}, Controller={controller != null}");
        }
    }

    void InitializeComponents()
    {
        // 自动找到子对象中的模型和Animator
        if (foxModel == null)
        {
            // 查找子对象中带MeshRenderer或SkinnedMeshRenderer的对象
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
            if (showDebugInfo) Debug.Log($"狐狸({gameObject.name}): 使用自身作为模型引用");
        }
        else
        {
            if (showDebugInfo) Debug.Log($"狐狸({gameObject.name}): 找到模型引用 - {foxModel.name}");
        }

        // 获取Animator（在模型对象上）
        animator = foxModel.GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError($"狐狸({gameObject.name}): 需要Animator组件！");
            }
            else
            {
                if (showDebugInfo) Debug.Log($"狐狸({gameObject.name}): 在子对象中找到Animator组件");
            }
        }

        // 获取或添加CharacterController（在控制器对象上）
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.center = new Vector3(0, 0.5f, 0);
            controller.height = 1f;
            controller.radius = 0.3f;
            if (showDebugInfo) Debug.Log($"狐狸({gameObject.name}): 自动添加CharacterController组件");
        }

        // 保存模型原始旋转（用于修复）
        if (foxModel != null)
        {
            modelOriginalRotation = foxModel.localRotation;
            if (showDebugInfo) Debug.Log($"狐狸模型原始旋转: {modelOriginalRotation.eulerAngles}");
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

        // 修复模型旋转（如果需要）
        if (fixModelRotation && foxModel != null)
        {
            FixModelRotation();
        }
    }

    // 修复模型旋转的方法
    void FixModelRotation()
    {
        if (foxModel == null) return;

        // 检查模型是否旋转了-90度
        Vector3 modelEuler = foxModel.localRotation.eulerAngles;
        if (Mathf.Abs(modelEuler.x + 90) < 1f || Mathf.Abs(modelEuler.x - 270) < 1f)
        {
            // 这是常见的FBX导入问题，需要修复
            if (showDebugInfo) Debug.Log($"检测到模型旋转问题: {modelEuler}");

            // 创建一个修复旋转
            Quaternion fixedRotation = Quaternion.Euler(0, modelEuler.y, modelEuler.z);
            foxModel.localRotation = fixedRotation;

            if (showDebugInfo) Debug.Log($"已修复模型旋转: {foxModel.localRotation.eulerAngles}");
        }
    }

    void Update()
    {
        // 检查跳跃输入
        if (Input.GetButtonDown("Jump"))
        {
            jumpTriggered = true;
        }

        // 非附身状态下的逻辑
        if (!isPossessed)
        {
            // 可以添加一些AI行为或空闲动画
            UpdateIdleBehavior();
            return;
        }
    }

    // ===== 实现 IPossessable 接口 =====
    public void OnPossess()
    {
        isPossessed = true;

        // 记录附身前的位置和旋转
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        controllerWasEnabled = controller != null && controller.enabled;

        if (showDebugInfo)
        {
            Debug.Log($"=== 狐狸({gameObject.name})被附身 ===");
            Debug.Log($"- 原始位置: {originalPosition}");
            Debug.Log($"- 原始旋转: {originalRotation.eulerAngles}");
            Debug.Log($"- 控制器启用状态: {controllerWasEnabled}");
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

        if (animator != null)
        {
            // 重置动画参数
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
            animator.ResetTrigger(jumpParam);
        }

        // 重置移动方向
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        jumpTriggered = false;

        // 恢复控制器状态
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
    }
    // ===== 接口实现结束 =====

    void HandleMovement()
    {
        if (controller == null)
        {
            Debug.LogError($"狐狸({gameObject.name}): CharacterController为空！");
            return;
        }

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

        // 计算移动方向（基于世界坐标）
        Vector3 move = new Vector3(horizontal, 0, vertical);

        // 获取相机方向
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

        // 判断是否有移动输入
        bool isMoving = move.magnitude > 0.1f;

        // 计算速度
        if (isMoving)
        {
            // 确定目标速度（行走或跑步）
            float targetSpeed = runInput ? runSpeed : walkSpeed;

            // 平滑过渡到目标速度
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

            // 标准化移动方向
            move.Normalize();

            // 应用水平移动
            moveDirection.x = move.x * currentSpeed;
            moveDirection.z = move.z * currentSpeed;

            // 重要：旋转控制器对象，而不是模型
            if (move != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(move.x, 0, move.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // 平滑停止
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 10f);
            moveDirection.x = 0;
            moveDirection.z = 0;
        }

        // 跳跃处理
        if (isGrounded && jumpTriggered)
        {
            moveDirection.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            if (animator != null)
            {
                animator.SetTrigger(jumpParam);
                if (showDebugInfo) Debug.Log($"狐狸({gameObject.name})触发跳跃动画");
            }
            jumpTriggered = false;
        }

        // 应用重力
        moveDirection.y += Physics.gravity.y * Time.deltaTime;

        // 应用移动
        if (controller.enabled)
        {
            controller.Move(moveDirection * Time.deltaTime);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 更新速度参数（归一化到0-1范围）
        float normalizedSpeed = currentSpeed / runSpeed;
        animator.SetFloat(speedParam, normalizedSpeed);

        // 更新是否在地面参数
        animator.SetBool(isGroundedParam, isGrounded);

        // 确保跳跃触发器被重置
        if (isGrounded && !jumpTriggered)
        {
            animator.ResetTrigger(jumpParam);
        }
    }

    void CheckGroundStatus()
    {
        if (controller == null) return;

        // 使用CharacterController的isGrounded和射线检测双重检查
        bool wasGroundedBefore = isGrounded;
        isGrounded = controller.isGrounded;

        // 额外的射线检测确保准确性
        if (!isGrounded)
        {
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayer))
            {
                isGrounded = true;
            }
        }

        // 更新动画参数
        if (animator != null && wasGroundedBefore != isGrounded)
        {
            animator.SetBool(isGroundedParam, isGrounded);
        }
    }

    void UpdateIdleBehavior()
    {
        // 非附身状态下的空闲行为
        if (animator != null)
        {
            // 确保在待机状态
            if (animator.GetFloat(speedParam) > 0.1f)
            {
                animator.SetFloat(speedParam, 0f);
            }

            // 确保在地面
            if (!isGrounded && controller != null)
            {
                isGrounded = controller.isGrounded;
                animator.SetBool(isGroundedParam, isGrounded);
            }
        }
    }

    // ===== 调试方法 =====
    [ContextMenu("测试IPossessable接口")]
    public void TestInterface()
    {
        Debug.Log($"=== 狐狸({gameObject.name})接口测试 ===");
        Debug.Log($"1. 脚本类型: {GetType().FullName}");
        Debug.Log($"2. 实现了IPossessable? {this is IPossessable}");
        Debug.Log($"3. 能力描述: {GetAbilityDescription()}");
        Debug.Log($"4. 游戏对象: {gameObject.name}");
        Debug.Log($"5. 对象路径: {GetTransformPath(transform)}");
        Debug.Log($"6. 层: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");
        Debug.Log($"7. 标签: {gameObject.tag}");
        Debug.Log($"8. 碰撞体: {GetComponent<Collider>() != null}");
        Debug.Log($"9. 当前是否被附身: {isPossessed}");
    }

    [ContextMenu("检查层级结构")]
    public void CheckHierarchy()
    {
        Debug.Log($"=== 狐狸层级结构检查 ===");
        Debug.Log($"根对象: {gameObject.name}");
        Debug.Log($"位置: {transform.position}");
        Debug.Log($"旋转: {transform.rotation.eulerAngles}");

        if (foxModel != null)
        {
            Debug.Log($"模型对象: {foxModel.name}");
            Debug.Log($"模型位置(本地): {foxModel.localPosition}");
            Debug.Log($"模型旋转(本地): {foxModel.localRotation.eulerAngles}");
            Debug.Log($"模型位置(世界): {foxModel.position}");
            Debug.Log($"模型旋转(世界): {foxModel.rotation.eulerAngles}");
        }

        Debug.Log($"Animator位置: {(animator != null ? animator.transform.name : "无")}");
        Debug.Log($"CharacterController: {controller != null}");
    }

    [ContextMenu("重置到原始位置")]
    public void ResetToOriginalPosition()
    {
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            controller.enabled = true;
            if (showDebugInfo) Debug.Log($"狐狸({gameObject.name})重置到原始位置: {originalPosition}");
        }
    }

    [ContextMenu("检查组件状态")]
    public void CheckComponentStatus()
    {
        Debug.Log($"=== 狐狸({gameObject.name})组件状态 ===");
        Debug.Log($"- Animator: {animator != null} {(animator != null ? animator.isActiveAndEnabled.ToString() : "N/A")}");
        Debug.Log($"- CharacterController: {controller != null} {(controller != null ? controller.enabled.ToString() : "N/A")}");
        Debug.Log($"- Collider: {GetComponent<Collider>() != null}");
        Debug.Log($"- 位置: {transform.position}");
        Debug.Log($"- 旋转: {transform.rotation.eulerAngles}");
        Debug.Log($"- 是否激活: {gameObject.activeInHierarchy}");
    }

    // 辅助方法：获取变换路径
    private string GetTransformPath(Transform tr)
    {
        if (tr.parent == null)
            return tr.name;
        return GetTransformPath(tr.parent) + "/" + tr.name;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        // 绘制控制器位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // 绘制模型位置
        if (foxModel != null && foxModel != transform)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(foxModel.position, 0.3f);
            Gizmos.DrawLine(transform.position, foxModel.position);
        }

        // 绘制地面检测线
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * (groundCheckDistance + 0.1f));
    }
}