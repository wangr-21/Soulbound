using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class PlayerSoulController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float possessionRange = 5f;

    [Header("跳跃设置")]
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    [Header("粒子系统引用")]
    public ParticleSystem soulParticles;
    public SoulAppearanceController soulAppearance;

    [Header("附身设置")]
    public LayerMask possessableLayerMask = -1;

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

    // 新增：位置锁定
    private Vector3 deerPositionBeforePossession;
    private Quaternion deerRotationBeforePossession;

    private void Awake()
    {
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

        if (soulParticles == null)
        {
            soulParticles = GetComponentInChildren<ParticleSystem>();
        }

        if (soulAppearance == null)
        {
            soulAppearance = GetComponent<SoulAppearanceController>();
        }

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

        if (isPossessing && currentPossessable != null)
        {
            currentPossessable.PossessedUpdate();
        }
        else
        {
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

        float cameraYRotation = 0f;
        if (cameraController != null)
        {
            cameraYRotation = cameraController.GetCurrentYRotation();
        }

        Vector3 moveDirection = new Vector3(currentMovementInput.x, 0, currentMovementInput.y);

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, cameraYRotation, 0);
            moveDirection = cameraRotation * moveDirection;

            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
        }

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
        if (debugMode) Debug.Log("=== 尝试附身 ===");

        // 可视化附身范围
        Debug.DrawRay(transform.position, Vector3.up * possessionRange, Color.red, 2f);

        // 修复：使用正确的方法查找IPossessable对象
        if (debugMode)
        {
            FindAndLogAllPossessables();
        }

        Debug.DrawRay(transform.position, Vector3.up * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.down * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.left * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.right * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.forward * possessionRange, Color.red, 2f);
        Debug.DrawRay(transform.position, Vector3.back * possessionRange, Color.red, 2f);

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

            // 重要：排除灵魂自己！
            if (obj == this.gameObject)
            {
                if (debugMode) Debug.Log($"跳过检测：这是灵魂自己 ({obj.name})");
                continue;
            }

            float distance = Vector3.Distance(transform.position, obj.transform.position);

            if (debugMode)
            {
                Debug.Log($"--- 检测到对象: {obj.name} ---");
            }

            // 方法1: 使用GetComponent直接获取IPossessable接口
            IPossessable possessable = obj.GetComponent<IPossessable>();
            if (possessable != null)
            {
                if (debugMode)
                {
                    Debug.Log($"  ✓ 直接找到IPossessable组件！类型: {possessable.GetType().Name}");
                    Debug.Log($"  调用GetAbilityDescription: {possessable.GetAbilityDescription()}");
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

            // 方法2: 尝试从子对象中获取
            possessable = obj.GetComponentInChildren<IPossessable>();
            if (possessable != null)
            {
                if (debugMode)
                {
                    Debug.Log($"  ✓ 在子对象中找到IPossessable！类型: {possessable.GetType().Name}");
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                }
                continue;
            }

            // 方法3: 尝试从父对象中获取
            possessable = obj.GetComponentInParent<IPossessable>();
            if (possessable != null)
            {
                if (debugMode)
                {
                    Debug.Log($"  ✓ 在父对象中找到IPossessable！类型: {possessable.GetType().Name}");
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestPossessable = possessable;
                }
                continue;
            }

            if (debugMode)
            {
                Debug.Log($"  ✗ 没有找到IPossessable组件");

                Component[] allComps = obj.GetComponents<Component>();
                Debug.Log($"  对象上的所有组件 ({allComps.Length}):");
                foreach (Component comp in allComps)
                {
                    Debug.Log($"    - {comp.GetType().FullName}");
                }
            }
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

    // 新增方法：查找并记录所有实现了IPossessable的对象
    void FindAndLogAllPossessables()
    {
        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        List<IPossessable> allPossessables = new List<IPossessable>();

        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb is IPossessable)
            {
                allPossessables.Add(mb as IPossessable);
            }
        }

        Debug.Log($"场景中所有IPossessable对象: {allPossessables.Count}");
        foreach (IPossessable p in allPossessables)
        {
            MonoBehaviour mb = p as MonoBehaviour;
            if (mb != null)
            {
                Debug.Log($"  - {p.GetType().Name} on {mb.gameObject.name}");
            }
        }
    }

    void PossessObject(GameObject target, IPossessable possessable)
    {
        currentPossessedObject = target;
        currentPossessable = possessable;

        if (debugMode) Debug.Log($"准备附身到: {target.name} (类型: {possessable.GetType().Name})");

        // 重要：在调用OnPossess之前，先记录鹿的当前位置和旋转
        deerPositionBeforePossession = target.transform.position;
        deerRotationBeforePossession = target.transform.rotation;

        if (debugMode)
            Debug.Log($"鹿的原始位置: {deerPositionBeforePossession}, Y={deerPositionBeforePossession.y:F2}, 旋转: {deerRotationBeforePossession.eulerAngles}");

        // 调用鹿的OnPossess方法
        currentPossessable.OnPossess();
        isPossessing = true;

        // 隐藏灵魂
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        if (characterController != null) characterController.enabled = false;

        HideSoulParticles();

        // 重要：检查并确保鹿的位置没有因为任何原因改变
        // 如果鹿的位置改变了，立即纠正回原始位置
        if (currentPossessedObject != null)
        {
            // 检查位置是否改变
            float positionChange = Vector3.Distance(currentPossessedObject.transform.position, deerPositionBeforePossession);
            if (positionChange > 0.01f)
            {
                if (debugMode) Debug.LogWarning($"检测到鹿位置改变: {positionChange:F2} 单位，正在纠正...");

                // 记录改变后的位置用于调试
                Vector3 changedPosition = currentPossessedObject.transform.position;

                // 纠正位置和旋转
                currentPossessedObject.transform.position = deerPositionBeforePossession;
                currentPossessedObject.transform.rotation = deerRotationBeforePossession;

                if (debugMode)
                    Debug.Log($"纠正位置: 从 {changedPosition} (Y={changedPosition.y:F2}) 到 {deerPositionBeforePossession} (Y={deerPositionBeforePossession.y:F2})");
            }

            if (debugMode)
                Debug.Log($"附身后鹿的位置: {currentPossessedObject.transform.position}, Y={currentPossessedObject.transform.position.y:F2}");
        }

        if (cameraController != null)
        {
            cameraController.SetTarget(currentPossessedObject.transform);
            if (debugMode) Debug.Log("相机目标切换到: " + currentPossessedObject.name);
        }

        if (debugMode) Debug.Log("成功附身！");

        // 启动位置监控协程
        StartCoroutine(MonitorDeerPosition());
    }

    // 新增协程：监控鹿的位置
    IEnumerator MonitorDeerPosition()
    {
        // 连续监控几帧，确保位置稳定
        for (int i = 0; i < 5; i++)
        {
            yield return null; // 等待一帧

            if (currentPossessedObject != null)
            {
                // 检查Y值是否异常
                if (Mathf.Abs(currentPossessedObject.transform.position.y - deerPositionBeforePossession.y) > 0.5f)
                {
                    Debug.LogError($"第{i + 1}帧: 鹿的Y值异常！期望: {deerPositionBeforePossession.y:F2}, 实际: {currentPossessedObject.transform.position.y:F2}");

                    // 强制纠正Y值
                    Vector3 correctedPosition = new Vector3(
                        currentPossessedObject.transform.position.x,
                        deerPositionBeforePossession.y,
                        currentPossessedObject.transform.position.z
                    );

                    currentPossessedObject.transform.position = correctedPosition;
                    Debug.Log($"已纠正Y值到: {correctedPosition.y:F2}");
                }
            }
        }

        if (debugMode) Debug.Log("位置监控完成");
    }

    void ReleasePossession()
    {
        if (currentPossessable != null)
        {
            // 调用鹿的释放方法
            currentPossessable.OnRelease();

            // 恢复灵魂的渲染和碰撞
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = true;

            // 将灵魂放在鹿的旁边（不要改变鹿的位置）
            if (currentPossessedObject != null)
            {
                transform.position = currentPossessedObject.transform.position + Vector3.up * 2f;
                if (debugMode) Debug.Log($"灵魂出现在鹿旁边，位置: {transform.position}");
            }

            // 启用灵魂的控制器
            if (characterController != null)
            {
                characterController.enabled = true;
            }

            // 显示灵魂粒子效果
            ShowSoulParticles();

            // 切换相机目标回灵魂
            if (cameraController != null)
            {
                cameraController.SetTarget(transform);
                if (debugMode) Debug.Log("相机目标切换回灵魂");
            }

            // 重置附身状态
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