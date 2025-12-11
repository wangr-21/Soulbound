using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LeopardController : PossessableBase
{
    [Header("豹子的设置")]
    public float walkSpeed = 12f;
    public float sprintSpeed = 25f;
    public float jumpForce = 10.0f;
    public float gravity = -20f;

    [Header("冲刺设置")]
    public float sprintStamina = 100f;
    public float sprintStaminaDrain = 20f;
    public float sprintStaminaRegen = 15f;

    [Header("特殊能力")]
    public float pounceForce = 15f;
    public float pounceCooldown = 3f;

    // 移动和跳跃相关变量
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool jumpTriggered = false;

    // 冲刺相关
    private bool isSprinting = false;
    private float currentSpeed;
    private float currentStamina;

    // 猛扑相关
    private bool canPounce = true;
    private float pounceTimer = 0f;

    // 相机相关
    private CameraController cameraController;

    // 音频相关
    private AudioSource audioSource;

    void Start()
    {
        // 设置对象名称和描述
        objectName = "豹子";
        abilityDescription = "快速移动、冲刺和猛扑";

        Debug.Log($"LeopardController初始化: {objectName} - {abilityDescription}");

        // 获取CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("LeopardController: 缺少CharacterController组件！");
            return;
        }

        // 调整CharacterController参数
        characterController.height = 2f;
        characterController.radius = 0.5f;
        characterController.center = new Vector3(0, 1f, 0);

        // 获取相机控制器
        if (Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }

        // 获取或添加AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.7f;
        }

        // 初始化状态
        currentSpeed = walkSpeed;
        currentStamina = sprintStamina;

        // 确保Base类初始化
        base.Start();

        Debug.Log("豹子初始化完成");
    }

    void Update()
    {
        // 更新猛扑冷却计时器
        if (!canPounce)
        {
            pounceTimer -= Time.deltaTime;
            if (pounceTimer <= 0f)
            {
                canPounce = true;
                Debug.Log("猛扑技能已冷却");
            }
        }

        // 更新耐力
        if (isSprinting)
        {
            currentStamina -= sprintStaminaDrain * Time.deltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                StopSprint();
            }
        }
        else if (currentStamina < sprintStamina)
        {
            currentStamina += sprintStaminaRegen * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, sprintStamina);
        }
    }

    // 实现被附身时的更新
    public override void PossessedUpdate()
    {
        HandleInput();
        HandleMovementAndJump();
    }

    // 处理输入
    private void HandleInput()
    {
        // 跳跃输入
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpTriggered = true;
        }

        // 冲刺输入
        if (Input.GetKeyDown(KeyCode.LeftShift) && currentStamina > 10f)
        {
            StartSprint();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            StopSprint();
        }

        // 猛扑输入
        if (Input.GetKeyDown(KeyCode.F) && canPounce && isGrounded)
        {
            Pounce();
        }
    }

    // 开始冲刺
    private void StartSprint()
    {
        if (!isSprinting && currentStamina > 10f)
        {
            isSprinting = true;
            currentSpeed = sprintSpeed;
            Debug.Log("开始冲刺！");
        }
    }

    // 停止冲刺
    private void StopSprint()
    {
        if (isSprinting)
        {
            isSprinting = false;
            currentSpeed = walkSpeed;
            Debug.Log("停止冲刺");
        }
    }

    // 猛扑技能
    private void Pounce()
    {
        canPounce = false;
        pounceTimer = pounceCooldown;

        // 计算猛扑方向
        Vector3 moveDirection = Vector3.zero;
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        if (moveX != 0 || moveZ != 0)
        {
            // 如果有输入，朝输入方向猛扑
            moveDirection = new Vector3(moveX, 0, moveZ);
            if (cameraController != null)
            {
                float cameraYRotation = cameraController.GetCurrentYRotation();
                Quaternion cameraRotation = Quaternion.Euler(0, cameraYRotation, 0);
                moveDirection = cameraRotation * moveDirection;
            }
        }
        else
        {
            // 如果没有输入，朝豹子前方猛扑
            moveDirection = transform.forward;
        }

        // 应用猛扑力
        moveDirection.Normalize();
        playerVelocity.y = Mathf.Sqrt(pounceForce * -2f * gravity * 0.5f);
        playerVelocity.x = moveDirection.x * pounceForce * 0.5f;
        playerVelocity.z = moveDirection.z * pounceForce * 0.5f;

        Debug.Log("猛扑！");
    }

    // 处理移动和跳跃
    private void HandleMovementAndJump()
    {
        if (characterController == null) return;

        isGrounded = characterController.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 moveDirection = new Vector3(moveX, 0, moveZ);

        // 基于相机视角转换移动方向
        if (cameraController != null)
        {
            float cameraYRotation = cameraController.GetCurrentYRotation();
            Quaternion cameraRotation = Quaternion.Euler(0, cameraYRotation, 0);
            moveDirection = cameraRotation * moveDirection;
        }

        // 应用移动
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // 处理跳跃
        if (jumpTriggered && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpTriggered = false;
            Debug.Log("豹子跳跃！");
        }

        // 应用重力
        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    // 重写附身方法
    public override void OnPossess()
    {
        base.OnPossess(); // 调用基类方法，设置isPossessed为true并改变颜色

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 重置状态
        currentStamina = sprintStamina;
        StopSprint();

        Debug.Log("已附身到豹子！");
        Debug.Log("控制说明：");
        Debug.Log("- WASD: 移动");
        Debug.Log("- 空格键: 跳跃");
        Debug.Log("- Shift: 冲刺（消耗耐力）");
        Debug.Log("- F键: 猛扑（冷却时间: " + pounceCooldown + "秒）");
        Debug.Log("- E键: 脱离");
    }

    // 重写脱离方法
    public override void OnRelease()
    {
        base.OnRelease(); // 调用基类方法，设置isPossessed为false并恢复颜色
        StopSprint();
        Debug.Log("从豹子脱离");
    }

    // 实现接口方法：获取能力描述
    public override string GetAbilityDescription()
    {
        return abilityDescription;
    }

    // 获取当前耐力百分比（用于UI显示）
    public float GetStaminaPercentage()
    {
        return currentStamina / sprintStamina;
    }

    // 获取猛扑冷却剩余时间（用于UI显示）
    public float GetPounceCooldownRemaining()
    {
        return canPounce ? 0f : pounceTimer;
    }

    // 获取猛扑是否可用
    public bool CanPounce()
    {
        return canPounce && isGrounded;
    }
}