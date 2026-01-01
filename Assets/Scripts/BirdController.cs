using UnityEngine;

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

    [Header("动画参数")]
    public string flyParam = "IsFlying";
    public string glideParam = "IsGliding";

    [Header("能力描述")]
    public string abilityDescription = "可以飞行和滑翔的鸟";

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

    // ===============================================================
    // Start
    // ===============================================================
    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (!animator) Debug.LogError("缺少 Animator !");
        if (!controller) Debug.LogError("缺少 CharacterController !");

        if (animator)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }

        FindGroundPosition(); // ★ 初始化贴地
    }

    // ===============================================================
    // IPossessable
    // ===============================================================
    public void OnPossess()
    {
        isPossessed = true;
        if (controller) controller.enabled = true;

        verticalVelocity = 0f;
        isGliding = false;

        // ★ ★ ★ 附身瞬间强制校准地面对齐
        FindGroundPosition();
        SnapToGround();
        isGrounded = true;

        Debug.Log("鸟被附身并校正位置！");
    }

    public void OnRelease()
    {
        isPossessed = false;
        verticalVelocity = 0;
        isGliding = false;

        if (animator)
        {
            animator.SetBool(flyParam, false);
            animator.SetBool(glideParam, false);
        }

        Debug.Log("鸟脱离附身！");
    }

    public string GetAbilityDescription() => abilityDescription;

    public void PossessedUpdate()
    {
        if (!isPossessed) return;

        GetInput();
        HandleMovement();
        UpdateAnimations();
        CheckGroundStatus();
    }

    // ===============================================================
    // 输入检测
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
    // 核心移动逻辑（已修复）
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
            Vector3 fwd = Camera.main.transform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = Camera.main.transform.right; right.y = 0; right.Normalize();
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

        if (showDebugInfo && Time.frameCount % 30 == 0)
            Debug.Log($"鸟状态: pos={transform.position:F2}, vY={verticalVelocity:F2}, grounded={isGrounded}");
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

        // Shift控制滑翔
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
}
