using UnityEngine;

public class DeerAI : MonoBehaviour
{
    [Header("AI 设置")]
    public float wanderRadius = 10f;        // 闲逛范围
    public float idleTimeMin = 2f;          // 最短空闲时间
    public float idleTimeMax = 8f;          // 最长空闲时间
    public float walkTimeMin = 3f;          // 最短行走时间
    public float walkTimeMax = 10f;         // 最长行走时间

    [Header("组件引用")]
    private Animator animator;
    private DeerController deerController;

    [Header("AI 状态")]
    public AIState currentState = AIState.Idle;
    private float stateTimer = 0f;
    private float currentStateDuration = 0f;
    private Vector3 homePosition;
    private Vector3 currentDestination;
    private bool hasValidDestination = false;

    [Header("调试")]
    public bool showGizmos = true;
    public Color wanderRadiusColor = Color.cyan;

    public enum AIState
    {
        Idle,       // 空闲状态
        Walking,    // 行走状态
        Running,    // 跑步状态（受惊时）
        Eating,     // 吃草状态
    }

    void Start()
    {
        // 获取组件
        animator = GetComponentInChildren<Animator>();
        deerController = GetComponent<DeerController>();

        if (animator == null)
        {
            Debug.LogError("DeerAI: 找不到Animator组件！");
        }

        if (deerController == null)
        {
            Debug.LogError("DeerAI: 找不到DeerController组件！");
        }

        // 记录家的位置（初始位置）
        homePosition = transform.position;

        // 设置初始状态
        EnterIdleState();
    }

    void Update()
    {
        // 如果鹿被玩家附身，禁用AI
        if (deerController != null && deerController.isPossessed)
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
            case AIState.Eating:
                UpdateEatingState();
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
        if (deerController != null)
        {
            // 传递零向量来停止移动
            deerController.AIMove(Vector3.zero, false);
        }

        // 决定下一个状态
        float randomValue = Random.value;
        if (randomValue < 0.3f)
        {
            // 30% 概率进入吃草状态
            currentStateDuration *= 1.5f; // 吃草时间更长
            currentState = AIState.Eating;
            EnterEatingState();
        }
    }

    void UpdateIdleState()
    {
        // 检查是否应该切换到行走状态
        if (stateTimer >= currentStateDuration)
        {
            // 决定是否行走
            if (Random.value < 0.7f) // 70% 概率行走
            {
                EnterWalkingState();
            }
            else
            {
                // 继续空闲，但可能切换到吃草
                if (Random.value < 0.4f)
                {
                    EnterEatingState();
                }
                else
                {
                    EnterIdleState(); // 重新开始空闲
                }
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
        if (hasValidDestination && deerController != null)
        {
            // 计算移动方向
            Vector3 direction = (currentDestination - transform.position);
            direction.y = 0; // 保持水平移动

            // 如果已经接近目标点，停止移动
            float distanceToTarget = direction.magnitude;
            if (distanceToTarget < 1f)
            {
                // 到达目的地，切换到空闲状态
                EnterIdleState();
                return;
            }

            // 通过控制器移动（AI状态只使用Walk动画）
            deerController.AIMove(direction.normalized, false);
        }

        // 检查是否时间到了
        if (stateTimer >= currentStateDuration)
        {
            // 时间到，切换到空闲状态
            EnterIdleState();
        }

        // 有小概率切换到跑步（受惊）
        if (Random.value < 0.02f) // 每帧2%的概率
        {
            EnterRunningState();
        }
    }

    void EnterRunningState()
    {
        currentState = AIState.Running;
        stateTimer = 0f;
        currentStateDuration = Random.Range(2f, 5f); // 跑步时间较短

        // 寻找远离当前位置的方向
        Vector3 runDirection = Random.insideUnitSphere;
        runDirection.y = 0;
        runDirection.Normalize();

        Vector3 targetPosition = transform.position + runDirection * wanderRadius * 0.5f;
        targetPosition = GetValidDestination(targetPosition);
        currentDestination = targetPosition;
        hasValidDestination = true;
    }

    void UpdateRunningState()
    {
        if (hasValidDestination && deerController != null)
        {
            // 计算移动方向
            Vector3 direction = (currentDestination - transform.position);
            direction.y = 0;

            // 如果已经接近目标点，停止移动
            float distanceToTarget = direction.magnitude;
            if (distanceToTarget < 1f)
            {
                // 到达目的地，切换到空闲状态
                EnterIdleState();
                return;
            }

            // 跑步状态也使用AIMove，但速度会快一些
            // 注意：AIMove方法会忽略isRunning参数，但至少确保方向正确
            deerController.AIMove(direction.normalized, true);
        }

        // 检查是否应该停止跑步
        if (stateTimer >= currentStateDuration)
        {
            // 停止跑步，切换到行走或空闲
            if (Random.value < 0.5f)
            {
                EnterWalkingState();
            }
            else
            {
                EnterIdleState();
            }
        }
    }

    void EnterEatingState()
    {
        currentState = AIState.Eating;
        stateTimer = 0f;
        currentStateDuration = Random.Range(idleTimeMin * 2f, idleTimeMax * 1.5f);
        hasValidDestination = false;

        // 停止移动
        if (deerController != null)
        {
            deerController.AIMove(Vector3.zero, false);
        }
    }

    void UpdateEatingState()
    {
        // 吃草状态，基本不动
        if (stateTimer >= currentStateDuration)
        {
            // 吃完草，切换到空闲或行走
            if (Random.value < 0.5f)
            {
                EnterWalkingState();
            }
            else
            {
                EnterIdleState();
            }
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
        if (Physics.Raycast(desiredPosition + Vector3.up * 10f, Vector3.down, out hit, 20f, deerController.groundLayer))
        {
            // 返回地面上的点
            return hit.point;
        }

        return desiredPosition;
    }

    #endregion

    #region 公共方法

    // 外部调用，让鹿受惊逃跑
    public void Scare(Vector3 scareSource)
    {
        if (currentState == AIState.Running) return; // 已经在逃跑

        EnterRunningState();

        // 设置逃跑方向（远离惊吓源）
        Vector3 runDirection = (transform.position - scareSource).normalized;
        runDirection.y = 0;
        Vector3 targetPosition = transform.position + runDirection * wanderRadius;
        currentDestination = GetValidDestination(targetPosition);
        hasValidDestination = true;

        // 缩短行走时间（逃跑时间短）
        currentStateDuration = Random.Range(2f, 4f);
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
            Gizmos.color = Color.yellow;
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