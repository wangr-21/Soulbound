using UnityEngine;

public class FoxAI : MonoBehaviour
{
    [Header("AI 设置")]
    public float wanderRadius = 10f;        // 闲逛范围
    public float idleTimeMin = 2f;          // 最短空闲时间
    public float idleTimeMax = 5f;          // 最长空闲时间
    public float walkTimeMin = 3f;          // 最短行走时间
    public float walkTimeMax = 8f;          // 最长行走时间
    public float runChance = 0.2f;          // 跑步概率（0-1之间）

    [Header("组件引用")]
    private FoxController foxController;

    [Header("AI 状态")]
    public AIState currentState = AIState.Idle;
    private float stateTimer = 0f;
    private float currentStateDuration = 0f;
    private Vector3 homePosition;
    private Vector3 currentDestination;
    private bool hasValidDestination = false;

    [Header("调试")]
    public bool showGizmos = true;
    public Color wanderRadiusColor = Color.yellow;

    public enum AIState
    {
        Idle,       // 空闲状态
        Walking,    // 行走状态
        Running     // 跑步状态
    }

    void Start()
    {
        // 获取组件
        foxController = GetComponent<FoxController>();

        if (foxController == null)
        {
            Debug.LogError("FoxAI: 找不到FoxController组件！");
        }

        // 记录家的位置（初始位置）
        homePosition = transform.position;

        // 设置初始状态
        EnterIdleState();
    }

    void Update()
    {
        // 如果狐狸被玩家附身，禁用AI
        if (foxController != null && foxController.isPossessed)
        {
            return;
        }

        // 更新状态计时器
        stateTimer += Time.deltaTime;

        // 根据当前状态执行相应的行为
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;
            case AIState.Walking:
                UpdateWalkingState();
                break;
            case AIState.Running:
                UpdateRunningState();
                break;
        }
    }

    #region 状态管理

    void EnterIdleState()
    {
        currentState = AIState.Idle;
        stateTimer = 0f;
        currentStateDuration = Random.Range(idleTimeMin, idleTimeMax);
        hasValidDestination = false;

        // 停止移动
        if (foxController != null)
        {
            foxController.AIMove(Vector3.zero, false);
        }
    }

    void UpdateIdleState()
    {
        // 检查是否应该切换到行走状态
        if (stateTimer >= currentStateDuration)
        {
            // 决定是否跑步（小概率）
            if (Random.value < runChance)
            {
                EnterRunningState();
            }
            else
            {
                EnterWalkingState();
            }
        }
    }

    void EnterWalkingState()
    {
        currentState = AIState.Walking;
        stateTimer = 0f;
        currentStateDuration = Random.Range(walkTimeMin, walkTimeMax);

        // 获取随机目标点
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection.y = 0;
        Vector3 targetPosition = homePosition + randomDirection;

        // 确保目标点在范围内
        targetPosition = GetValidDestination(targetPosition);
        currentDestination = targetPosition;
        hasValidDestination = true;
    }

    void UpdateWalkingState()
    {
        if (hasValidDestination && foxController != null)
        {
            // 计算移动方向
            Vector3 direction = (currentDestination - transform.position);
            direction.y = 0;

            // 如果已经接近目标点，切换到空闲状态
            float distanceToTarget = direction.magnitude;
            if (distanceToTarget < 0.5f)
            {
                EnterIdleState();
                return;
            }

            // 行走移动
            foxController.AIMove(direction.normalized, false);
        }

        // 检查是否时间到了
        if (stateTimer >= currentStateDuration)
        {
            EnterIdleState();
        }
    }

    void EnterRunningState()
    {
        currentState = AIState.Running;
        stateTimer = 0f;
        currentStateDuration = Random.Range(2f, 4f); // 跑步时间较短

        // 获取随机目标点（跑步范围更大）
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius * 1.5f;
        randomDirection.y = 0;
        Vector3 targetPosition = transform.position + randomDirection;

        targetPosition = GetValidDestination(targetPosition);
        currentDestination = targetPosition;
        hasValidDestination = true;
    }

    void UpdateRunningState()
    {
        if (hasValidDestination && foxController != null)
        {
            // 计算移动方向
            Vector3 direction = (currentDestination - transform.position);
            direction.y = 0;

            // 如果已经接近目标点，切换到空闲状态
            float distanceToTarget = direction.magnitude;
            if (distanceToTarget < 1f)
            {
                EnterIdleState();
                return;
            }

            // 跑步移动（isRunning设置为true，但AIMove会根据参数调整速度）
            foxController.AIMove(direction.normalized, true);
        }

        // 检查是否时间到了
        if (stateTimer >= currentStateDuration)
        {
            EnterIdleState();
        }
    }

    #endregion

    #region 导航

    Vector3 GetValidDestination(Vector3 desiredPosition)
    {
        // 确保目标点在闲逛范围内
        Vector3 direction = desiredPosition - homePosition;
        direction.y = 0;

        if (direction.magnitude > wanderRadius)
        {
            direction = direction.normalized * wanderRadius;
            desiredPosition = homePosition + direction;
        }

        // 使用射线检测确保目标点在地面上
        RaycastHit hit;
        if (Physics.Raycast(desiredPosition + Vector3.up * 5f, Vector3.down, out hit, 10f, foxController.groundLayer))
        {
            // 返回地面上的点
            return hit.point;
        }

        return desiredPosition;
    }

    #endregion

    #region 公共方法

    // 外部调用，让狐狸受惊逃跑
    public void Scare(Vector3 scareSource)
    {
        if (currentState == AIState.Running || foxController.isPossessed)
            return;

        EnterRunningState();

        // 设置逃跑方向（远离惊吓源）
        Vector3 runDirection = (transform.position - scareSource).normalized;
        runDirection.y = 0;
        Vector3 targetPosition = transform.position + runDirection * wanderRadius * 2f;
        currentDestination = GetValidDestination(targetPosition);
        hasValidDestination = true;
    }

    // 设置新的闲逛中心点
    public void SetHomePosition(Vector3 newHome)
    {
        homePosition = newHome;
    }

    #endregion

    #region 调试

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // 绘制闲逛范围
        Gizmos.color = wanderRadiusColor;
        Gizmos.DrawWireSphere(homePosition, wanderRadius);

        // 绘制当前目标点
        if (Application.isPlaying && hasValidDestination)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentDestination, 0.3f);
            Gizmos.DrawLine(transform.position, currentDestination);

            // 状态文本
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "State: " + currentState);
#endif
        }
    }

    #endregion
}