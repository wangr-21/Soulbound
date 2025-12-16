using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSoulController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float possessionRange = 3f;

    [Header("跳跃设置")]
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    [Header("粒子系统引用")]
    public ParticleSystem soulParticles; // 添加粒子系统引用
    public SoulAppearanceController soulAppearance; // 可选的外观控制器

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

    private int possessableLayerMask;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInputActions = new PlayerInputActions();
    }

    private void Start()
    {
        // 获取相机控制器
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
            Debug.Log("相机控制器找到并设置目标");
        }
        else
        {
            Debug.LogError("未找到相机控制器！请确保主相机上有 CameraController 组件");
        }

        // 如果没有手动指定粒子系统，尝试自动获取
        if (soulParticles == null)
        {
            soulParticles = GetComponentInChildren<ParticleSystem>();
            if (soulParticles == null)
            {
                Debug.LogWarning("未找到粒子系统引用，请手动指定");
            }
        }

        // 如果没有手动指定外观控制器，尝试自动获取
        if (soulAppearance == null)
        {
            soulAppearance = GetComponent<SoulAppearanceController>();
        }

        possessableLayerMask = LayerMask.GetMask("Default", "Possessable");
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

    // 移动输入回调
    private void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
    }

    // 跳跃输入回调
    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpTriggered = true;
        }
    }

    void Update()
    {
        // 附身/脱离输入检测 - 移到最前面，确保任何状态下都能检测
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
        isGrounded = characterController.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.5f;
        }

        // 获取相机的当前水平旋转角度
        float cameraYRotation = 0f;
        if (cameraController != null)
        {
            cameraYRotation = cameraController.GetCurrentYRotation();
        }

        // 将输入方向转换为相对于相机视角的方向
        Vector3 moveDirection = new Vector3(currentMovementInput.x, 0, currentMovementInput.y);

        // 创建基于相机Y轴旋转的旋转四元数
        Quaternion cameraRotation = Quaternion.Euler(0, cameraYRotation, 0);

        // 将移动方向转换为世界空间，相对于相机视角
        moveDirection = cameraRotation * moveDirection;

        // 应用移动
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        if (jumpTriggered && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpTriggered = false;
        }

        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    void AttemptPossession()
    {
        Debug.Log("=== 尝试附身 ===");
        Debug.Log($"灵魂位置: {transform.position}");
        Debug.Log($"附身范围: {possessionRange}");

        // 可视化附身范围
        Debug.DrawRay(transform.position, Vector3.up * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.down * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.left * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.right * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.forward * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.back * possessionRange, Color.red, 2f);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, possessionRange);

        Debug.Log($"检测到 {hitColliders.Length} 个碰撞体");

        if (hitColliders.Length == 0)
        {
            Debug.LogWarning("没有检测到任何碰撞体！可能原因：");
            Debug.LogWarning("1. 鸟没有Collider组件");
            Debug.LogWarning("2. possessionRange太小");
            Debug.LogWarning("3. 鸟在另一个Layer上（被忽略）");
            Debug.LogWarning("4. 鸟太远（检查Y轴高度差）");
            return;
        }

        GameObject closestObject = null;
        IPossessable closestPossessable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            GameObject obj = hitCollider.gameObject;
            float distance = Vector3.Distance(transform.position, obj.transform.position);

            Debug.Log($"--- 检测到: {obj.name} ---");
            Debug.Log($"  距离: {distance:F2}");
            Debug.Log($"  位置: {obj.transform.position}");
            Debug.Log($"  层级: {LayerMask.LayerToName(obj.layer)}");
            Debug.Log($"  是否有Collider: {obj.GetComponent<Collider>() != null}");

            // 打印所有组件
            Component[] components = obj.GetComponents<Component>();
            Debug.Log($"  组件数量: {components.Length}");
            foreach (Component comp in components)
            {
                Debug.Log($"    - {comp.GetType().Name}");
            }

            // 检查CharacterController组件
            CharacterController charController = obj.GetComponent<CharacterController>();
            if (charController != null)
            {
                Debug.Log($"  ✓ 有CharacterController组件");
            }

            // 方法1: 直接获取BirdController
            BirdController birdController = obj.GetComponent<BirdController>();
            if (birdController != null)
            {
                Debug.Log($"  ✓ 直接找到BirdController组件！");
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = birdController; // BirdController实现了IPossessable
                    Debug.Log($"  ✓ 选择这个鸟作为目标");
                }
                continue;
            }

            // 方法2: 尝试获取IPossessable接口
            IPossessable possessable = obj.GetComponent<IPossessable>();
            if (possessable != null)
            {
                Debug.Log($"  ✓ 找到IPossessable接口！类型: {possessable.GetType().Name}");
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                    Debug.Log($"  ✓ 选择这个可附身对象");
                }
                continue;
            }

            // 方法3: 在子对象中查找
            birdController = obj.GetComponentInChildren<BirdController>();
            if (birdController != null)
            {
                Debug.Log($"  ✓ 在子对象中找到BirdController组件！");
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = birdController;
                }
                continue;
            }

            // 方法4: 在父对象中查找
            birdController = obj.GetComponentInParent<BirdController>();
            if (birdController != null)
            {
                Debug.Log($"  ✓ 在父对象中找到BirdController组件！");
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = birdController;
                }
                continue;
            }

            Debug.Log($"  ✗ 没有找到BirdController或IPossessable组件");
        }

        if (closestObject != null && closestPossessable != null)
        {
            Debug.Log($"最终选择附身对象: {closestObject.name}");
            PossessObject(closestObject, closestPossessable);
        }
        else
        {
            Debug.LogWarning("没有找到可附身的对象");
        }
    }

    void PossessObject(GameObject target, IPossessable possessable)
    {
        currentPossessedObject = target;
        currentPossessable = possessable;

        Debug.Log($"准备附身到: {target.name}");

        // 调用附身方法
        currentPossessable.OnPossess();
        isPossessing = true;

        // 隐藏灵魂
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        characterController.enabled = false;

        // 隐藏粒子系统
        HideSoulParticles();

        // 切换相机目标到被附身的对象
        if (cameraController != null)
        {
            cameraController.SetTarget(currentPossessedObject.transform);
            Debug.Log("相机目标切换到: " + currentPossessedObject.name);
        }

        Debug.Log("成功附身！");
    }

    void ReleasePossession()
    {
        if (currentPossessable != null)
        {
            currentPossessable.OnRelease();

            // 显示灵魂并移动到被附身对象的位置
            GetComponent<Renderer>().enabled = true;
            GetComponent<Collider>().enabled = true;
            transform.position = currentPossessedObject.transform.position + Vector3.up * 1f; // 稍微上方一点
            characterController.enabled = true;

            // 新增：显示粒子系统并确保位置正确
            ShowSoulParticles();

            // 切换相机目标回灵魂
            if (cameraController != null)
            {
                cameraController.SetTarget(transform);
                Debug.Log("相机目标切换回灵魂");
            }

            currentPossessedObject = null;
            currentPossessable = null;
            isPossessing = false;
        }
    }

    // 新增：隐藏粒子系统的方法
    void HideSoulParticles()
    {
        // 方法1：通过粒子系统组件
        if (soulParticles != null)
        {
            soulParticles.gameObject.SetActive(false);
            Debug.Log("隐藏粒子系统");
        }

        // 方法2：通过外观控制器（如果有）
        else if (soulAppearance != null)
        {
            soulAppearance.HideSoul();
            Debug.Log("通过外观控制器隐藏灵魂");
        }

        // 方法3：如果没有指定引用，尝试自动查找并禁用所有粒子系统
        else
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                ps.gameObject.SetActive(false);
            }
            if (allParticles.Length > 0)
            {
                Debug.Log("自动查找到并隐藏了 " + allParticles.Length + " 个粒子系统");
            }
        }
    }

    // 新增：显示粒子系统的方法
    void ShowSoulParticles()
    {
        // 确保灵魂位置正确
        if (soulParticles != null)
        {
            soulParticles.transform.position = transform.position;
            soulParticles.gameObject.SetActive(true);
            Debug.Log("显示粒子系统");
        }
        else if (soulAppearance != null)
        {
            soulAppearance.ShowSoul();
            Debug.Log("通过外观控制器显示灵魂");
        }
        else
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                ps.gameObject.SetActive(true);
                ps.transform.position = transform.position;
            }
            if (allParticles.Length > 0)
            {
                Debug.Log("自动查找到并显示了 " + allParticles.Length + " 个粒子系统");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, possessionRange);
    }

    // 在 PlayerSoulController 类中添加这个方法
    public void ForceReleasePossession()
    {
        if (isPossessing)
        {
            ReleasePossession();
        }
    }
}