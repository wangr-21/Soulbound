using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSoulController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float possessionRange = 5f; // 增加范围

    [Header("跳跃设置")]
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    [Header("粒子系统引用")]
    public ParticleSystem soulParticles;
    public SoulAppearanceController soulAppearance;

    [Header("附身设置")]
    public LayerMask possessableLayerMask = -1; // 改为public，可在Inspector设置

    // 移动和跳跃相关变量
    private Vector3 playerVelocity;
    private bool isGrounded;
    private CharacterController characterController;
    private PlayerInputActions playerInputActions;

    // 输入相关变量
    private Vector2 currentMovementInput;
    private bool jumpTriggered = false;

    // 附身相关变量
    private GameObject currentPossessedObject;
    private bool isPossessing = false;
    private IPossessable currentPossessable;

    // 视角切换
    private CameraController cameraController;

    // 新增：调试信息
    [Header("调试")]
    public bool debugMode = true;

    private void Awake()
    {
        // 确保有CharacterController组件
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            Debug.Log("灵魂: 自动添加CharacterController组件");
        }

        playerInputActions = new PlayerInputActions();
    }

    private void Start()
    {
        // 获取相机控制器
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
            if (debugMode) Debug.Log("相机控制器找到并设置目标");
        }
        else
        {
            Debug.LogError("未找到相机控制器！请确保主相机上有 CameraController 组件");
        }

        // 粒子系统
        if (soulParticles == null)
        {
            soulParticles = GetComponentInChildren<ParticleSystem>();
        }

        // 外观控制器
        if (soulAppearance == null)
        {
            soulAppearance = GetComponent<SoulAppearanceController>();
        }

        // 如果LayerMask未设置，使用默认值
        if (possessableLayerMask.value == 0)
        {
            possessableLayerMask = LayerMask.GetMask("Default");
            if (debugMode) Debug.Log($"使用默认LayerMask: {possessableLayerMask.value}");
        }

        if (debugMode) Debug.Log($"灵魂控制器初始化完成 - 位置: {transform.position}");
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        playerInputActions.Player.Move.performed += OnMove;
        playerInputActions.Player.Move.canceled += OnMove;
        playerInputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Move.performed -= OnMove;
        playerInputActions.Player.Move.canceled -= OnMove;
        playerInputActions.Player.Jump.performed -= OnJump;
        playerInputActions.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpTriggered = true;
        }
    }

    void Update()
    {
        // 附身/脱离输入检测
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isPossessing)
            {
                AttemptPossession();
            }
            else
            {
                ReleasePossession();
            }
        }

        // 如果正在附身，控制权交给被附身对象
        if (isPossessing && currentPossessable != null)
        {
            currentPossessable.PossessedUpdate();
        }
        else
        {
            // 如果没有附身，控制灵魂移动和跳跃
            HandleMovementAndJump();
        }
    }

    private void HandleMovementAndJump()
    {
        if (characterController == null) return;

        isGrounded = characterController.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.5f;
        }

        // 获取相机旋转
        float cameraYRotation = 0f;
        if (cameraController != null)
        {
            cameraYRotation = cameraController.GetCurrentYRotation();
        }

        // 将输入方向转换为相对于相机视角的方向
        Vector3 moveDirection = new Vector3(currentMovementInput.x, 0, currentMovementInput.y);

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, cameraYRotation, 0);
            moveDirection = cameraRotation * moveDirection;

            // 应用移动
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
        }

        // 跳跃
        if (jumpTriggered && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpTriggered = false;
        }

        // 应用重力
        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    void AttemptPossession()
    {
        if (debugMode) Debug.Log("=== 尝试附身 ===");

        // 可视化附身范围
        Debug.DrawRay(transform.position, Vector3.up * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.down * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.left * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.right * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.forward * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.back * possessionRange, Color.red, 2f);

        // 检测范围内的所有碰撞体
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, possessionRange, possessableLayerMask);

        if (debugMode) Debug.Log($"检测到 {hitColliders.Length} 个碰撞体（在指定层中）");

        if (hitColliders.Length == 0)
        {
            if (debugMode)
            {
                Debug.LogWarning("没有检测到任何可附身的碰撞体！");
                Debug.LogWarning($"检测范围: 半径={possessionRange}, 层={LayerMask.LayerToName(possessableLayerMask.value)}");
                Debug.LogWarning($"灵魂位置: {transform.position}");
            }
            return;
        }

        GameObject closestObject = null;
        IPossessable closestPossessable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            GameObject obj = hitCollider.gameObject;
            float distance = Vector3.Distance(transform.position, obj.transform.position);

            if (debugMode)
            {
                Debug.Log($"--- 检测到: {obj.name} ---");
                Debug.Log($"  距离: {distance:F2}");
                Debug.Log($"  位置: {obj.transform.position}");
                Debug.Log($"  层级: {LayerMask.LayerToName(obj.layer)}");
            }

            // 方法1: 检查IPossessable接口（应该能同时检测BirdController和LeopardController）
            IPossessable possessable = obj.GetComponent<IPossessable>();
            if (possessable != null)
            {
                if (debugMode) Debug.Log($"  ✓ 找到IPossessable接口！类型: {possessable.GetType().Name}");

                // 特殊检查：如果是LeopardController，确保它有必要的组件
                if (possessable is LeopardController leopard)
                {
                    if (debugMode) Debug.Log($"  ✓ 这是LeopardController，检查组件状态...");

                    // 检查LeopardController的必要组件
                    CharacterController cc = obj.GetComponent<CharacterController>();
                    Animator anim = obj.GetComponent<Animator>();

                    if (cc == null)
                    {
                        if (debugMode) Debug.Log($"  ✗ LeopardController缺少CharacterController组件");
                        continue;
                    }

                    if (!cc.enabled && !leopard.isPossessed)
                    {
                        if (debugMode) Debug.Log($"  ✓ CharacterController已禁用（符合预期）");
                    }
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                    if (debugMode) Debug.Log($"  ✓ 选择这个对象作为目标");
                }
                continue;
            }

            // 方法2: 在子对象中查找IPossessable
            possessable = obj.GetComponentInChildren<IPossessable>();
            if (possessable != null)
            {
                if (debugMode) Debug.Log($"  ✓ 在子对象中找到IPossessable接口！类型: {possessable.GetType().Name}");
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                }
                continue;
            }

            // 方法3: 在父对象中查找IPossessable
            possessable = obj.GetComponentInParent<IPossessable>();
            if (possessable != null)
            {
                if (debugMode) Debug.Log($"  ✓ 在父对象中找到IPossessable接口！类型: {possessable.GetType().Name}");
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                }
                continue;
            }

            if (debugMode) Debug.Log($"  ✗ 没有找到IPossessable组件");
        }

        if (closestObject != null && closestPossessable != null)
        {
            if (debugMode) Debug.Log($"最终选择附身对象: {closestObject.name} (距离: {closestDistance:F2})");
            PossessObject(closestObject, closestPossessable);
        }
        else
        {
            if (debugMode) Debug.LogWarning("没有找到可附身的对象（有碰撞体但没有IPossessable接口）");
        }
    }

    void PossessObject(GameObject target, IPossessable possessable)
    {
        currentPossessedObject = target;
        currentPossessable = possessable;

        if (debugMode) Debug.Log($"准备附身到: {target.name} (类型: {possessable.GetType().Name})");

        // 调用附身方法
        currentPossessable.OnPossess();
        isPossessing = true;

        // 隐藏灵魂
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        if (characterController != null) characterController.enabled = false;

        // 隐藏粒子系统
        HideSoulParticles();

        // 切换相机目标到被附身的对象
        if (cameraController != null)
        {
            cameraController.SetTarget(currentPossessedObject.transform);
            if (debugMode) Debug.Log("相机目标切换到: " + currentPossessedObject.name);
        }

        if (debugMode) Debug.Log("成功附身！");
    }

    void ReleasePossession()
    {
        if (currentPossessable != null)
        {
            currentPossessable.OnRelease();

            // 显示灵魂并移动到被附身对象的位置
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            // 将灵魂放在被附身对象上方
            if (currentPossessedObject != null)
            {
                transform.position = currentPossessedObject.transform.position + Vector3.up * 2f;
            }

            if (characterController != null) characterController.enabled = true;

            // 显示粒子系统
            ShowSoulParticles();

            // 切换相机目标回灵魂
            if (cameraController != null)
            {
                cameraController.SetTarget(transform);
                if (debugMode) Debug.Log("相机目标切换回灵魂");
            }

            currentPossessedObject = null;
            currentPossessable = null;
            isPossessing = false;

            if (debugMode) Debug.Log("已脱离附身");
        }
    }

    void HideSoulParticles()
    {
        if (soulParticles != null)
        {
            soulParticles.gameObject.SetActive(false);
        }
        else if (soulAppearance != null)
        {
            soulAppearance.HideSoul();
        }
        else
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                ps.gameObject.SetActive(false);
            }
        }
    }

    void ShowSoulParticles()
    {
        if (soulParticles != null)
        {
            soulParticles.transform.position = transform.position;
            soulParticles.gameObject.SetActive(true);
        }
        else if (soulAppearance != null)
        {
            soulAppearance.ShowSoul();
        }
        else
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                ps.gameObject.SetActive(true);
                ps.transform.position = transform.position;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, possessionRange);

        // 绘制检测范围的可视化
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, possessionRange);
    }

    public void ForceReleasePossession()
    {
        if (isPossessing)
        {
            ReleasePossession();
        }
    }
}