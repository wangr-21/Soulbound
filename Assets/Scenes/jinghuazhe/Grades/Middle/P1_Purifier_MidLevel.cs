using UnityEngine;
using UnityEngine.AI;

public class P1_Purifier_MidLevel : MonoBehaviour
{
    [Header("=== 中级净化者设置 ===")]
    public Transform[] villageWaypoints; // 村庄小路关键点
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;
    [SerializeField] private float pointArrivalDistance = 0.5f;

    [Header("=== AI组件 ===")]
    private NavMeshAgent agent;
    private Transform player;

    [Header("=== 视觉设置 ===")]
    [SerializeField] private float visionRadius = 12f;
    [SerializeField] private float visionAngle = 130f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float escapeDistance = 16f;

    [Header("=== 警戒反应 ===")]
    [SerializeField] private Color normalColor = new Color(1f, 0.5f, 0f); // 橙色
    [SerializeField] private Color alertColor = new Color(1f, 0.2f, 0f);  // 亮橙色
    [SerializeField] private float rotateSpeed = 6f;

    [Header("=== 随机移动设置 ===")]
    [SerializeField] private float randomMoveRadius = 8f;
    [SerializeField] private int maxPathAttempts = 15;

    // 私有变量
    private Renderer purifierRenderer;
    private float waitCounter = 0f;
    private bool isPlayerInSight = false;
    private bool wasPlayerInSight = false;
    private Vector3 currentTarget;
    private bool hasValidTarget = false;

    private enum State { Moving, Waiting, Alert }
    private State currentState = State.Moving;

    void Start()
    {
        InitializeComponents();
        SetRandomDestination();
        Debug.Log("中级净化者初始化完成！");
    }

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // 更新状态
        wasPlayerInSight = isPlayerInSight;
        CheckPlayerInSight();

        // 状态机
        switch (currentState)
        {
            case State.Moving:
                if (!isPlayerInSight)
                    HandleMovingState();
                else
                    TransitionToAlert();
                break;

            case State.Waiting:
                if (!isPlayerInSight)
                    HandleWaitingState();
                else
                    TransitionToAlert();
                break;

            case State.Alert:
                HandleAlertState();
                break;
        }
    }

    /// <summary>
    /// 初始化所有组件
    /// </summary>
    private void InitializeComponents()
    {
        // 获取NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("中级净化者：缺少NavMeshAgent组件！");
            return;
        }

        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("中级净化者：未找到玩家对象！");
        }

        // 设置渲染器和颜色
        purifierRenderer = GetComponent<Renderer>();
        if (purifierRenderer != null)
        {
            purifierRenderer.material = new Material(Shader.Find("Standard"));
            purifierRenderer.material.color = normalColor;
        }

        // 检查小路关键点
        if (villageWaypoints == null || villageWaypoints.Length == 0)
        {
            Debug.LogWarning("中级净化者：未设置村庄小路关键点！将在当前位置附近移动。");
        }
    }

    /// <summary>
    /// 视野检测
    /// </summary>
    private void CheckPlayerInSight()
    {
        if (player == null)
        {
            isPlayerInSight = false;
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        // 距离检查
        if (distance > visionRadius)
        {
            isPlayerInSight = false;
            return;
        }

        // 角度检查
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > visionAngle / 2)
        {
            isPlayerInSight = false;
            return;
        }

        // 障碍物检查
        Vector3 rayStart = transform.position + Vector3.up * 1.2f;
        Vector3 playerCenter = player.position + Vector3.up * 1.0f;

        Debug.DrawRay(rayStart, (playerCenter - rayStart).normalized * distance,
                     isPlayerInSight ? Color.red : Color.yellow);

        if (Physics.Raycast(rayStart, (playerCenter - rayStart).normalized,
            out RaycastHit hit, distance, obstacleMask))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                isPlayerInSight = false;
                return;
            }
        }

        isPlayerInSight = true;
    }

    /// <summary>
    /// 移动状态处理
    /// </summary>
    private void HandleMovingState()
    {
        if (!hasValidTarget)
        {
            SetRandomDestination();
            return;
        }

        // 检查是否到达目标
        if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
        {
            currentState = State.Waiting;
            waitCounter = Random.Range(minWaitTime, maxWaitTime);
            agent.isStopped = true;
        }

        // 防卡住检测
        if (agent.velocity.magnitude < 0.1f && agent.remainingDistance > pointArrivalDistance)
        {
            Debug.Log("中级净化者：检测到可能卡住，重新规划路径");
            SetRandomDestination();
        }
    }

    /// <summary>
    /// 等待状态处理
    /// </summary>
    private void HandleWaitingState()
    {
        waitCounter -= Time.deltaTime;

        if (waitCounter <= 0f)
        {
            currentState = State.Moving;
            SetRandomDestination();
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// 警戒状态处理
    /// </summary>
    private void HandleAlertState()
    {
        // 停止移动
        if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 面向玩家
        if (player != null)
        {
            Vector3 lookDir = (player.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }
        }

        // 改变颜色
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = alertColor;
        }

        // 检查玩家是否逃脱
        CheckPlayerEscape();
    }

    /// <summary>
    /// 切换到警戒状态
    /// </summary>
    private void TransitionToAlert()
    {
        if (currentState != State.Alert)
        {
            currentState = State.Alert;
            Debug.Log("中级净化者：进入警戒状态！");
        }
    }

    /// <summary>
    /// 检查玩家逃脱
    /// </summary>
    private void CheckPlayerEscape()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > escapeDistance)
        {
            isPlayerInSight = false;
            currentState = State.Moving;
            agent.isStopped = false;

            if (purifierRenderer != null)
            {
                purifierRenderer.material.color = normalColor;
            }

            Debug.Log("中级净化者：玩家逃脱，恢复巡逻");
        }
    }

    /// <summary>
    /// 设置随机目的地
    /// </summary>
    private void SetRandomDestination()
    {
        Vector3 randomPos = FindValidRandomPosition();

        if (randomPos != Vector3.zero)
        {
            currentTarget = randomPos;
            agent.SetDestination(currentTarget);
            hasValidTarget = true;
        }
        else
        {
            // 如果找不到有效位置，稍后重试
            hasValidTarget = false;
            waitCounter = 1f;
            currentState = State.Waiting;
        }
    }

    /// <summary>
    /// 寻找有效的随机位置
    /// </summary>
    private Vector3 FindValidRandomPosition()
    {
        Vector3 center = GetMovementCenter();

        for (int i = 0; i < maxPathAttempts; i++)
        {
            // 生成随机方向和大小的向量
            Vector2 randomCircle = Random.insideUnitCircle * randomMoveRadius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            // 在NavMesh上寻找最近的点
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, randomMoveRadius, NavMesh.AllAreas))
            {
                // 检查路径是否可达
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
        }

        Debug.LogWarning("中级净化者：无法找到有效的移动位置");
        return Vector3.zero;
    }

    /// <summary>
    /// 获取移动中心点
    /// </summary>
    private Vector3 GetMovementCenter()
    {
        // 优先使用小路关键点
        if (villageWaypoints != null && villageWaypoints.Length > 0)
        {
            return villageWaypoints[Random.Range(0, villageWaypoints.Length)].position;
        }

        // 否则使用当前位置
        return transform.position;
    }

    // ========== 调试可视化 ==========
    private void OnDrawGizmosSelected()
    {
        // 视野范围
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // 逃脱距离
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, escapeDistance);

        // 移动范围
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireSphere(GetMovementCenter(), randomMoveRadius);

        // 当前目标
        if (hasValidTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentTarget, 0.3f);
            Gizmos.DrawLine(transform.position, currentTarget);
        }

        // 视野锥形
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Vector3 leftBound = Quaternion.Euler(0, -visionAngle / 2, 0) * transform.forward * visionRadius;
        Vector3 rightBound = Quaternion.Euler(0, visionAngle / 2, 0) * transform.forward * visionRadius;
        Gizmos.DrawLine(transform.position, transform.position + leftBound);
        Gizmos.DrawLine(transform.position, transform.position + rightBound);
    }

    [ContextMenu("测试随机移动")]
    public void TestRandomMovement()
    {
        Debug.Log("测试随机移动...");
        SetRandomDestination();
    }

    [ContextMenu("显示状态信息")]
    public void ShowStatusInfo()
    {
        Debug.Log($"=== 中级净化者状态 ===\n" +
                  $"当前状态: {currentState}\n" +
                  $"玩家检测: {isPlayerInSight}\n" +
                  $"有效目标: {hasValidTarget}\n" +
                  $"移动速度: {agent.velocity.magnitude:F2}\n" +
                  $"剩余距离: {agent.remainingDistance:F2}");
    }
}