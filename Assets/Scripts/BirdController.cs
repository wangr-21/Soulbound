using UnityEngine;

public class BirdController : MonoBehaviour, IPossessable  // 添加接口实现
{
    [Header("移动设置")]
    public float flySpeed = 5f;
    public float glideSpeed = 8f;
    public float rotationSpeed = 2f;
    public float ascendSpeed = 3f;
    public float descendSpeed = 2f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer = -1; // 默认所有层

    [Header("动画参数")]
    public string flyParam = "IsFlying";
    public string glideParam = "IsGliding";

    [Header("能力描述")]
    public string abilityDescription = "可以飞行和滑翔的鸟";

    [Header("状态")]
    public bool isPossessed = false;  // 是否被附身
    private bool isGrounded = true;
    private bool isGliding = false;
    private bool wasGrounded = true;

    [Header("组件引用")]
    private Animator animator;
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;

    [Header("调试")]
    public bool showDebugInfo = true;

    void Start()
    {
        // 获取组件引用
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            Debug.LogError("鸟控制器需要Animator组件！");
        }

        if (controller == null)
        {
            Debug.LogError("鸟控制器需要CharacterController组件！");
        }

        // 初始设置为待机状态
        if (animator != null)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }
    }

    void Update()
    {
        // 非附身状态下的逻辑（如果有的话）
        if (!isPossessed)
        {
            // 可以添加一些AI行为或空闲动画
            UpdateAnimations();
            return;
        }
    }

    // ===== 实现 IPossessable 接口 =====

    public void OnPossess()
    {
        isPossessed = true;
        Debug.Log("鸟被附身了！");

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
                Debug.LogError("鸟的CharacterController组件丢失！");
            }
        }

        // 设置初始动画状态
        if (animator != null)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }
    }

    public void OnRelease()
    {
        isPossessed = false;

        if (animator != null)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }

        // 重置移动方向
        moveDirection = Vector3.zero;
        isGliding = false;

        Debug.Log("鸟脱离附身！");
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
        bool ascend = Input.GetKey(KeyCode.Space);
        bool descend = Input.GetKey(KeyCode.LeftControl);

        if (showDebugInfo)
        {
            Debug.Log($"鸟移动输入: H={horizontal:F2}, V={vertical:F2}, Ascend={ascend}, Descend={descend}");
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

        // 垂直移动（上升/下降）
        if (ascend)
            moveDirection.y = ascendSpeed;
        else if (descend)
            moveDirection.y = -descendSpeed;
        else if (!controller.isGrounded)
            moveDirection.y += Physics.gravity.y * Time.deltaTime;
        else
            moveDirection.y = 0;

        // 应用移动
        if (isGliding)
            controller.Move((move * glideSpeed + moveDirection) * Time.deltaTime);
        else
            controller.Move((move * flySpeed + moveDirection) * Time.deltaTime);

        // 旋转控制
        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(move.x, 0, move.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 检查是否开始飞行
        if (!isGrounded && !animator.GetBool(flyParam))
        {
            animator.SetBool(flyParam, true);
            if (showDebugInfo) Debug.Log("切换到飞行状态");
        }

        // 滑翔控制（按Shift键滑翔）
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isGrounded)
        {
            isGliding = true;
            animator.SetBool(glideParam, true);
            if (showDebugInfo) Debug.Log("切换到滑翔状态");
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isGliding = false;
            animator.SetBool(glideParam, false);
            if (showDebugInfo) Debug.Log("退出滑翔状态");
        }

        // 落地检测
        if (isGrounded && animator.GetBool(flyParam))
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
            isGliding = false;
            if (showDebugInfo) Debug.Log("切换到待机状态");
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
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                isGrounded = true;
            }
        }

        // 状态变化时的额外处理
        if (wasGroundedBefore != isGrounded)
        {
            if (isGrounded && showDebugInfo)
                Debug.Log("鸟已落地");
            else if (!isGrounded && showDebugInfo)
                Debug.Log("鸟已起飞");
        }
    }

    // 添加一些辅助方法供外部调用
    public void ForceLand()
    {
        if (animator != null)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }
        isGliding = false;
    }

    public void ForceTakeoff()
    {
        if (animator != null)
        {
            animator.SetBool(flyParam, true);
        }
        isGrounded = false;
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

    // 添加一个测试方法，确保接口正常工作
    void TestInterface()
    {
        Debug.Log($"这个对象实现了IPossessable: {this is IPossessable}");
        Debug.Log($"能力描述: {GetAbilityDescription()}");
    }
}