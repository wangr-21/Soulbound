using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeerController : MonoBehaviour, IPossessable
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
    public string abilityDescription = "可以在陆地上奔跑和跳跃的鹿";

    [Header("AI 设置")]
    public DeerAI deerAI;
    private bool isAIControlled = false;

    [Header("附身时间限制")]
    public float maxPossessionTime = 120f; // 最大附身时间
    public float possessionTimeRemaining = 0f; // 剩余附身时间
    private bool isPossessionTimerActive = false; // 附身计时器是否激活
    private bool isTimeExhausted = false; // 时间是否已耗尽

    [Header("生命值设置")]
    public float maxHealth = 100f;
    public float currentHealth = 0f;

    [Header("死亡效果")]
    public GameObject deathEffect;
    public AudioClip deathSound;

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

    [Header("调试")]
    public bool showDebugInfo = true;

    // UI管理器引用
    private UIManager uiManager;

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

            // 设置Character Controller默认参数
            controller.height = 1.5f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0, 0.75f, 0);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;
            controller.minMoveDistance = 0.001f;
        }
        else if (showDebugInfo)
        {
            Debug.Log($"Character Controller设置:");
            Debug.Log($"  Height: {controller.height}");
            Debug.Log($"  Radius: {controller.radius}");
            Debug.Log($"  Center: {controller.center}");
            Debug.Log($"  StepOffset: {controller.stepOffset}");
        }

        // 获取或添加DeerAI组件
        deerAI = GetComponent<DeerAI>();
        if (deerAI == null)
        {
            deerAI = gameObject.AddComponent<DeerAI>();
            Debug.Log("自动添加DeerAI组件");
        }

        // 初始状态由AI控制（如果没有被附身）
        isAIControlled = !isPossessed;

        // 初始设置为待机状态
        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
        }

        // 初始化生命值和附身时间
        currentHealth = maxHealth;
        possessionTimeRemaining = maxPossessionTime;
        isTimeExhausted = false;

        // 获取UI管理器
        uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogWarning("未找到UIManager实例，UI可能无法正常工作");
        }

        // 强制调整到地面位置
        ForceGroundPosition();
    }

    void Update()
    {
        // 更新附身计时器
        if (isPossessionTimerActive && isPossessed)
        {
            UpdatePossessionTimer();
        }

        // 如果被附身，执行PossessedUpdate
        if (isPossessed)
        {
            PossessedUpdate();
            return;
        }

        // 如果没有被附身且AI控制
        if (isAIControlled && deerAI != null && deerAI.enabled)
        {
            // AI会通过DeerAI.Update()自动控制
            // 但我们仍然需要处理重力和地面检测
            CheckGroundStatus();
            ApplyGravity();
        }
    }

    /// <summary>
    /// 更新附身计时器
    /// </summary>
    private void UpdatePossessionTimer()
    {
        // 如果还有剩余时间，减少时间
        if (possessionTimeRemaining > 0 && !isTimeExhausted)
        {
            possessionTimeRemaining -= Time.deltaTime;

            // 确保时间不会变成负数
            if (possessionTimeRemaining < 0)
            {
                possessionTimeRemaining = 0;
            }

            // 添加调试信息
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"鹿附身剩余时间: {possessionTimeRemaining:F1}s, 生命值: {currentHealth:F1}");
            }

            // 检查时间是否耗尽
            if (possessionTimeRemaining <= 0)
            {
                isTimeExhausted = true;
                Debug.Log("鹿的附身时间耗尽！开始持续扣血");
            }
        }

        // 时间耗尽，持续扣血
        if (isTimeExhausted)
        {
            // 每秒扣20点血
            float damagePerSecond = 20f;
            float damageThisFrame = damagePerSecond * Time.deltaTime;
            TakeDamage(damageThisFrame);

            // 添加调试信息
            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log($"鹿时间耗尽持续扣血中，当前生命值: {currentHealth:F1}, 本帧伤害: {damageThisFrame:F2}");
            }
        }
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // 如果已经死亡，不再扣血

        currentHealth -= damage;

        // 确保生命值不低于0
        if (currentHealth < 0) currentHealth = 0;

        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} 死亡！当前生命值: {currentHealth}");

        // 播放死亡效果
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // 如果当前被附身，强制玩家灵魂脱离
        if (isPossessed)
        {
            PlayerSoulController.Instance.ForceReleasePossession();

            // 如果玩家在动物死亡时没有足够生命值，游戏结束
            if (PlayerSoulController.Instance.currentHealth <= 0)
            {
                StartCoroutine(DelayedGameOver(1f));
            }
        }

        // 销毁动物
        Destroy(gameObject);
    }

    private IEnumerator DelayedGameOver(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 调用玩家灵魂的游戏结束方法
        PlayerSoulController.Instance.OnSoulDissipate();
    }

    /// <summary>
    /// 被净化者攻击
    /// </summary>
    public void TakePurifierDamage(float damage)
    {
        TakeDamage(damage);
    }

    void ForceGroundPosition()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out hit, 10f, groundLayer))
        {
            float targetY = hit.point.y + controller.height * 0.5f + controller.skinWidth;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

            if (showDebugInfo)
                Debug.Log($"强制调整鹿到地面: 从 {transform.position.y} 调整到 {targetY}");
        }
        else if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f))
        {
            float targetY = hit.point.y + controller.height * 0.5f + controller.skinWidth;
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }

        verticalVelocity = -0.5f;
    }

    // 在DeerController中添加以下方法
    public void ResetState()
    {
        // 重置血量
        currentHealth = maxHealth;

        // 重置附身时间
        possessionTimeRemaining = maxPossessionTime;

        // 重置其他状态
        // isDead = false;
        // 其他需要重置的变量...

        Debug.Log("DeerController: 状态已重置");
    }

    // ===== 实现 IPossessable 接口 =====

    public void OnPossess()
    {
        Vector3 positionBefore = transform.position;
        Quaternion rotationBefore = transform.rotation;

        if (showDebugInfo)
            Debug.Log($"鹿被附身！位置: {positionBefore}, Y={positionBefore.y:F2}");

        isPossessed = true;
        isAIControlled = false;

        // 启动附身计时器
        isPossessionTimerActive = true;
        isTimeExhausted = false;
        possessionTimeRemaining = maxPossessionTime;

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (deerAI != null)
        {
            deerAI.enabled = false;
        }

        if (controller != null)
        {
            bool wasEnabled = controller.enabled;
            if (!wasEnabled)
            {
                controller.enabled = true;
            }
        }

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
            animator.CrossFade("Idle", 0.2f);
        }

        transform.position = positionBefore;
        transform.rotation = rotationBefore;

        if (showDebugInfo)
            Debug.Log($"OnPossess完成，确保位置保持在Y={transform.position.y:F2}");
    }

    public void OnRelease()
    {
        isPossessed = false;
        isAIControlled = true;

        // 停止附身计时器
        isPossessionTimerActive = false;
        isTimeExhausted = false;
        possessionTimeRemaining = maxPossessionTime;

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetBool(isGroundedParam, true);
        }

        moveDirection = Vector3.zero;
        verticalVelocity = -0.5f;
        isRunning = false;

        if (deerAI != null)
        {
            deerAI.enabled = true;
        }

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

    void ApplyGravity()
    {
        if (controller == null) return;

        groundCheckTimer -= Time.deltaTime;
        if (groundCheckTimer <= 0)
        {
            CheckGroundStatus();
            groundCheckTimer = 0.1f;
        }

        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;

            if (verticalVelocity < 0)
            {
                verticalVelocity += extraGravity * Time.deltaTime;
            }
        }
        else
        {
            verticalVelocity = -0.5f;
        }

        Vector3 verticalMove = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        controller.Move(verticalMove);
    }

    void HandleMovement()
    {
        if (!isPossessed) return;

        if (controller == null)
        {
            Debug.LogError("CharacterController为空！");
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool jump = Input.GetButtonDown("Jump");

        groundCheckTimer -= Time.deltaTime;
        if (groundCheckTimer <= 0)
        {
            CheckGroundStatus();
            groundCheckTimer = 0.1f;
        }

        Vector3 move = new Vector3(horizontal, 0, vertical);

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

        float currentSpeed = 0f;

        if (move.magnitude > 0.1f)
        {
            isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            controller.Move(move.normalized * currentSpeed * Time.deltaTime);

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

        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;

            if (verticalVelocity < 0)
            {
                verticalVelocity += extraGravity * Time.deltaTime;
            }
        }
        else
        {
            verticalVelocity = -0.5f;

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

        Vector3 verticalMove = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        controller.Move(verticalMove);

        if (animator != null)
        {
            float animSpeed = Mathf.Lerp(animator.GetFloat(speedParam), currentSpeed, Time.deltaTime * 5f);
            animator.SetFloat(speedParam, animSpeed);
        }
    }

    public void AIMove(Vector3 direction, bool isRunning = false)
    {
        if (controller == null || !isAIControlled) return;

        moveDirection = direction;

        float currentSpeed = walkSpeed;

        if (direction.magnitude > 0.1f)
        {
            controller.Move(direction.normalized * currentSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            if (animator != null)
            {
                float animSpeed = 0.5f;
                animator.SetFloat(speedParam, animSpeed);
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetFloat(speedParam, 0f);
            }
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool(isGroundedParam, isGrounded);

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"鹿状态: 在地面={isGrounded}, 垂直速度={verticalVelocity:F2}, 控制器在地面={controller.isGrounded}");
        }
    }

    void CheckGroundStatus()
    {
        if (controller == null) return;

        bool controllerGrounded = controller.isGrounded;

        RaycastHit hit;
        bool raycastGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer
        );

        bool sphereCastGrounded = Physics.SphereCast(
            transform.position + Vector3.up * 0.2f,
            controller.radius * 0.9f,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer
        );

        isGrounded = controllerGrounded || raycastGrounded || sphereCastGrounded;

        if (isGrounded && hit.collider != null)
        {
            float groundHeight = hit.point.y;
            float currentBottom = transform.position.y - controller.height * 0.5f;

            if (currentBottom > groundHeight + 0.05f)
            {
                float adjustY = groundHeight + controller.height * 0.5f + controller.skinWidth;
                transform.position = new Vector3(transform.position.x, adjustY, transform.position.z);

                if (showDebugInfo)
                    Debug.Log($"调整鹿位置确保地面接触: {adjustY}");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (showDebugInfo)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(
                transform.position + Vector3.up * 0.1f,
                transform.position + Vector3.up * 0.1f + Vector3.down * groundCheckDistance
            );

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

            if (Application.isPlaying && moveDirection.magnitude > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + moveDirection.normalized * 2f);
            }
        }
    }

    // 调试方法
    [ContextMenu("测试：立即时间耗尽")]
    public void TestTimeExhausted()
    {
        possessionTimeRemaining = 0.1f; // 设置很少的时间
        Debug.Log("测试：鹿的附身时间设置为0.1秒");
    }
}