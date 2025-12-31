using UnityEngine;
using System.Collections;

public enum SheepAIState
{
    Idle,           // 待机
    Wandering,      // 随机走动
    Paused,         // 短暂暂停
    Eating,         // 吃草（可选）
    LookingAround   // 环顾四周（可选）
}

public class SheepAI : MonoBehaviour
{
    [Header("AI 设置")]
    public SheepAIState currentState = SheepAIState.Idle;
    public bool enableAI = true;

    [Header("活动区域 - 扩大范围")]
    public Vector3 wanderCenter = Vector3.zero;
    public float wanderRadius = 15f;  // 从10增加到15，扩大移动范围
    public bool useRandomCenter = true;

    [Header("状态持续时间")]
    public float minIdleTime = 3f;
    public float maxIdleTime = 8f;
    public float minWanderTime = 8f;   // 增加走动时间，让羊走得更久一些
    public float maxWanderTime = 20f;  // 增加最大走动时间
    public float minPauseTime = 1f;
    public float maxPauseTime = 3f;

    [Header("移动参数 - 使用Walk动画")]
    public float wanderSpeed = 1.5f;    // 使用较慢的速度，确保只触发Walk动画
    public float rotationSpeed = 2f;
    public float stoppingDistance = 0.5f;

    [Header("动画参数")]
    public Animator animator;
    public string speedParam = "Speed";

    [Header("组件引用")]
    private CharacterController controller;
    private SheepController sheepController;

    [Header("状态计时器")]
    private float stateTimer = 0f;
    private float stateDuration = 0f;

    [Header("移动目标")]
    private Vector3 targetPosition;
    private bool hasReachedTarget = false;

    [Header("调试")]
    public bool showDebugInfo = false;
    public bool drawGizmos = true;

    [Header("动画控制")]
    [Tooltip("AI移动时使用的动画速度值，设置在Walk动画范围内（0.1-0.7）")]
    public float aiWalkAnimationSpeed = 0.5f;  // 设置为Walk动画范围的值

    void Start()
    {
        InitializeComponents();

        if (useRandomCenter)
        {
            wanderCenter = transform.position;
        }

        // 初始状态
        SwitchState(SheepAIState.Idle);
    }

    void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogWarning("SheepAI: 未找到CharacterController组件");
        }

        sheepController = GetComponent<SheepController>();
        if (sheepController == null)
        {
            Debug.LogWarning("SheepAI: 未找到SheepController组件");
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        // 设置默认的AI动画速度
        if (aiWalkAnimationSpeed < 0.1f || aiWalkAnimationSpeed > 0.7f)
        {
            aiWalkAnimationSpeed = 0.5f; // 确保在Walk动画范围内
            Debug.Log("调整AI动画速度为Walk范围值: 0.5");
        }
    }

    void Update()
    {
        if (!enableAI) return;

        // 检查是否被附身，如果被附身则禁用AI
        if (sheepController != null && sheepController.isPossessed)
        {
            if (currentState != SheepAIState.Idle)
            {
                SwitchState(SheepAIState.Idle);
            }
            return;
        }

        // 更新当前状态
        UpdateState();

        // 更新状态计时器
        stateTimer += Time.deltaTime;

        // 检查状态是否需要切换
        CheckStateTransition();
    }

    void UpdateState()
    {
        switch (currentState)
        {
            case SheepAIState.Idle:
                UpdateIdleState();
                break;

            case SheepAIState.Wandering:
                UpdateWanderingState();
                break;

            case SheepAIState.Paused:
                UpdatePausedState();
                break;

            case SheepAIState.Eating:
                UpdateEatingState();
                break;

            case SheepAIState.LookingAround:
                UpdateLookingAroundState();
                break;
        }
    }

    void UpdateIdleState()
    {
        // 待机状态：播放待机动画，不移动
        if (animator != null)
        {
            // 平滑过渡到待机动画
            float currentAnimSpeed = animator.GetFloat(speedParam);
            float targetSpeed = 0f;
            animator.SetFloat(speedParam, Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * 5f));
        }

        // 可以添加一些小动作，比如随机转头
        RandomHeadMovement();
    }

    void UpdateWanderingState()
    {
        if (controller == null) return;

        // 检查是否到达目标位置
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget <= stoppingDistance || hasReachedTarget)
        {
            // 到达目标，减速停止
            if (animator != null)
            {
                float currentSpeed = animator.GetFloat(speedParam);
                animator.SetFloat(speedParam, Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 5f));
            }
            hasReachedTarget = true;
        }
        else
        {
            // 向目标位置移动
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            moveDirection.y = 0;

            // 应用移动 - 使用较慢的速度确保只触发Walk动画
            controller.Move(moveDirection * wanderSpeed * Time.deltaTime);

            // 旋转朝向移动方向
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // 更新动画 - 使用固定的Walk动画速度值，确保不会触发Run动画
            if (animator != null)
            {
                // 使用aiWalkAnimationSpeed，确保在Walk动画范围内（0.1-0.7）
                float currentAnimSpeed = animator.GetFloat(speedParam);
                animator.SetFloat(speedParam, Mathf.Lerp(currentAnimSpeed, aiWalkAnimationSpeed, Time.deltaTime * 3f));

                if (showDebugInfo && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"AI行走: 速度={wanderSpeed}, 动画值={aiWalkAnimationSpeed}, 距离目标={distanceToTarget:F2}");
                }
            }
        }
    }

    void UpdatePausedState()
    {
        // 暂停状态：短暂停止，播放待机动画
        if (animator != null)
        {
            float currentAnimSpeed = animator.GetFloat(speedParam);
            animator.SetFloat(speedParam, Mathf.Lerp(currentAnimSpeed, 0f, Time.deltaTime * 5f));
        }
    }

    void UpdateEatingState()
    {
        // 吃草状态（可选功能）
        if (animator != null)
        {
            float currentAnimSpeed = animator.GetFloat(speedParam);
            animator.SetFloat(speedParam, Mathf.Lerp(currentAnimSpeed, 0f, Time.deltaTime * 5f));
            // 这里可以触发吃草动画，如果你有的话
        }
    }

    void UpdateLookingAroundState()
    {
        // 环顾四周状态
        if (animator != null)
        {
            float currentAnimSpeed = animator.GetFloat(speedParam);
            animator.SetFloat(speedParam, Mathf.Lerp(currentAnimSpeed, 0f, Time.deltaTime * 5f));
        }

        // 随机转动头部或身体
        RandomHeadMovement();
    }

    void CheckStateTransition()
    {
        // 检查状态计时器，决定是否切换到下一个状态
        if (stateTimer >= stateDuration)
        {
            switch (currentState)
            {
                case SheepAIState.Idle:
                    // 从待机状态切换到随机走动
                    SwitchState(SheepAIState.Wandering);
                    break;

                case SheepAIState.Wandering:
                    // 从走动状态切换到暂停或待机
                    if (Random.value > 0.4f) // 降低切换到暂停的概率，让羊走得更久
                    {
                        SwitchState(SheepAIState.Paused);
                    }
                    else
                    {
                        SwitchState(SheepAIState.Idle);
                    }
                    break;

                case SheepAIState.Paused:
                    // 从暂停状态切换到待机或走动
                    if (Random.value > 0.5f)
                    {
                        SwitchState(SheepAIState.Wandering);
                    }
                    else
                    {
                        SwitchState(SheepAIState.Idle);
                    }
                    break;

                case SheepAIState.Eating:
                    // 吃完草后切换到待机
                    SwitchState(SheepAIState.Idle);
                    break;

                case SheepAIState.LookingAround:
                    // 环顾四周后切换到走动或待机
                    if (Random.value > 0.5f)
                    {
                        SwitchState(SheepAIState.Wandering);
                    }
                    else
                    {
                        SwitchState(SheepAIState.Idle);
                    }
                    break;
            }
        }
    }

    void SwitchState(SheepAIState newState)
    {
        // 退出当前状态的清理工作
        ExitCurrentState();

        // 切换到新状态
        currentState = newState;
        stateTimer = 0f;
        hasReachedTarget = false;

        // 根据新状态进行初始化
        switch (newState)
        {
            case SheepAIState.Idle:
                stateDuration = Random.Range(minIdleTime, maxIdleTime);
                if (showDebugInfo) Debug.Log("绵羊AI: 切换到待机状态，持续时间: " + stateDuration);
                break;

            case SheepAIState.Wandering:
                stateDuration = Random.Range(minWanderTime, maxWanderTime);
                SetRandomWanderTarget();
                if (showDebugInfo) Debug.Log("绵羊AI: 切换到走动状态，持续时间: " + stateDuration);
                break;

            case SheepAIState.Paused:
                stateDuration = Random.Range(minPauseTime, maxPauseTime);
                if (showDebugInfo) Debug.Log("绵羊AI: 切换到暂停状态，持续时间: " + stateDuration);
                break;

            case SheepAIState.Eating:
                stateDuration = Random.Range(minIdleTime, maxIdleTime * 0.5f);
                if (showDebugInfo) Debug.Log("绵羊AI: 切换到吃草状态，持续时间: " + stateDuration);
                break;

            case SheepAIState.LookingAround:
                stateDuration = Random.Range(minPauseTime, maxPauseTime);
                if (showDebugInfo) Debug.Log("绵羊AI: 切换到环顾状态，持续时间: " + stateDuration);
                break;
        }
    }

    void ExitCurrentState()
    {
        // 退出当前状态时的清理工作
        switch (currentState)
        {
            case SheepAIState.Wandering:
                // 停止移动
                if (animator != null)
                {
                    animator.SetFloat(speedParam, 0f);
                }
                break;
        }
    }

    void SetRandomWanderTarget()
    {
        // 在活动区域内随机选择一个目标位置
        // 使用更大的范围
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPosition = new Vector3(
            wanderCenter.x + randomCircle.x,
            wanderCenter.y,
            wanderCenter.z + randomCircle.y
        );

        // 确保目标位置在地面上
        RaycastHit hit;
        int maxAttempts = 5;
        int attempts = 0;

        // 尝试找到有效的地面位置
        while (attempts < maxAttempts)
        {
            if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                // 检查地面是否平坦（法线接近垂直）
                if (Vector3.Angle(hit.normal, Vector3.up) < 30f)
                {
                    targetPosition.y = hit.point.y + controller.height * 0.5f + controller.skinWidth;
                    break;
                }
            }

            // 如果找不到合适的地面，重新随机一个位置
            randomCircle = Random.insideUnitCircle * wanderRadius;
            targetPosition = new Vector3(
                wanderCenter.x + randomCircle.x,
                wanderCenter.y,
                wanderCenter.z + randomCircle.y
            );

            attempts++;
        }

        if (showDebugInfo)
        {
            Debug.Log($"绵羊AI: 设置新目标位置: {targetPosition}, 距离: {Vector3.Distance(transform.position, targetPosition):F2}");
            Debug.Log($"活动范围: 中心={wanderCenter}, 半径={wanderRadius}");
        }
    }

    void RandomHeadMovement()
    {
        // 随机的小动作，比如轻微转头
        if (Random.value < 0.01f) // 每帧1%的几率
        {
            // 轻微随机旋转
            float randomYaw = Random.Range(-30f, 30f);
            transform.Rotate(0, randomYaw * Time.deltaTime, 0);
        }
    }

    // 外部调用：启用/禁用AI
    public void EnableAI(bool enable)
    {
        enableAI = enable;
        if (!enable)
        {
            SwitchState(SheepAIState.Idle);
        }
    }

    // 设置活动区域
    public void SetWanderArea(Vector3 center, float radius)
    {
        wanderCenter = center;
        wanderRadius = Mathf.Max(radius, 5f); // 确保最小半径为5
    }

    // 调试可视化
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // 绘制活动区域
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(wanderCenter, wanderRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(wanderCenter, wanderRadius);

        // 绘制中心点
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(wanderCenter, 0.5f);

        // 绘制当前目标位置
        if (currentState == SheepAIState.Wandering && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, targetPosition);

            // 绘制目标点上的箭头
            Vector3 arrowDirection = (targetPosition - transform.position).normalized;
            DrawArrow(targetPosition, arrowDirection, 0.5f);
        }

        // 绘制状态指示器
        Gizmos.color = GetStateColor(currentState);
        Vector3 stateIndicatorPos = transform.position + Vector3.up * 2f;
        Gizmos.DrawSphere(stateIndicatorPos, 0.2f);

        // 绘制状态文字
#if UNITY_EDITOR
        UnityEditor.Handles.Label(stateIndicatorPos + Vector3.up * 0.3f, currentState.ToString());
#endif
    }

    // 绘制箭头辅助方法
    void DrawArrow(Vector3 pos, Vector3 direction, float size)
    {
        Gizmos.DrawRay(pos, direction * size);
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;
        Gizmos.DrawRay(pos + direction * size, right * size * 0.3f);
        Gizmos.DrawRay(pos + direction * size, left * size * 0.3f);
    }

    Color GetStateColor(SheepAIState state)
    {
        switch (state)
        {
            case SheepAIState.Idle: return Color.gray;
            case SheepAIState.Wandering: return Color.blue;
            case SheepAIState.Paused: return Color.yellow;
            case SheepAIState.Eating: return Color.green;
            case SheepAIState.LookingAround: return Color.cyan;
            default: return Color.white;
        }
    }
}