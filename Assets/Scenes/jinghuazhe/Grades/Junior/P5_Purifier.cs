using UnityEngine;
using UnityEngine.AI;

public class P5_Purifier : MonoBehaviour
{
    [Header("=== P5专属设置 ===")]
    public Transform[] P5_patrolPoints;
    [SerializeField] private float waitTimeAtPoint = 1.5f;
    [SerializeField] private float pointArrivalDistance = 0.5f;

    [Header("=== AI Components ===")]
    private NavMeshAgent agent;
    private Transform player;

    [Header("=== Vision Settings ===")]
    [SerializeField] private float visionRadius = 8f;  // 默认设置为较大的值
    [SerializeField] private float visionAngle = 120f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float escapeDistance = 12f;  // 比视野半径大

    [Header("=== Alert Reactions ===")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.7f, 1f);
    [SerializeField] private Color alertColor = new Color(1f, 0.3f, 1f);
    [SerializeField] private float rotateSpeed = 5f;

    private Renderer purifierRenderer;
    private int currentPatrolIndex = 0;
    private float waitCounter = 0f;
    private bool isPlayerInSight = false;
    private bool wasPlayerInSight = false;

    private enum PatrolState { Moving, Waiting }
    private PatrolState currentPatrolState = PatrolState.Moving;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("P5 找不到NavMeshAgent组件！");
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("P5: 找到玩家对象");
        }
        else
        {
            Debug.LogWarning("P5 未找到标签为Player的物体！");
        }

        purifierRenderer = GetComponent<Renderer>();
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = normalColor;
        }

        if (P5_patrolPoints != null && P5_patrolPoints.Length > 0)
        {
            SetNextPatrolTarget();
            Debug.Log($"P5 脚本启动，开始巡逻。视野半径: {visionRadius}, 逃脱距离: {escapeDistance}");
        }
        else
        {
            Debug.LogWarning("P5 未设置巡逻点！");
        }
    }

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // 保存上一帧的状态
        wasPlayerInSight = isPlayerInSight;

        // 视野检测
        CheckPlayerInSight();

        // 实时显示距离信息
        if (player != null)
        {
            float currentDistance = Vector3.Distance(transform.position, player.position);
            // 在Game窗口中查看这个信息
            if (currentDistance < visionRadius + 2f) // 只在接近时显示，避免日志过多
            {
                Debug.Log($"P5-距离: {currentDistance:F1}, 视野: {visionRadius}, 逃脱: {escapeDistance}, 检测: {isPlayerInSight}");
            }
        }

        // 状态变化时输出日志
        if (isPlayerInSight && !wasPlayerInSight)
        {
            Debug.Log("P5: 检测到玩家！停止巡逻");
        }
        else if (!isPlayerInSight && wasPlayerInSight)
        {
            Debug.Log("P5: 玩家消失，恢复巡逻");
        }

        if (!isPlayerInSight)
        {
            PatrolBehavior();
            ResetAlertState();
        }
        else
        {
            HandlePlayerDetected();
            CheckPlayerEscape();
        }
    }

    /// <summary>
    /// 视野检测逻辑 - 修复版
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

        // 1. 距离检测 - 只使用visionRadius，不受escapeDistance限制
        if (distance > visionRadius)
        {
            isPlayerInSight = false;
            return;
        }

        // 2. 角度检测
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > visionAngle / 2)
        {
            isPlayerInSight = false;
            return;
        }

        // 3. 障碍物遮挡检测 - 提高射线起点
        Vector3 rayStart = transform.position + Vector3.up * 1.2f;
        Vector3 playerCenter = player.position + Vector3.up * 1.0f;
        Vector3 direction = (playerCenter - rayStart).normalized;

        // 在Scene视图中显示检测线
        Debug.DrawRay(rayStart, direction * Mathf.Min(distance, visionRadius),
                     isPlayerInSight ? Color.red : Color.yellow, 0.1f);

        if (Physics.Raycast(rayStart, direction, out RaycastHit hit, distance, obstacleMask))
        {
            // 如果击中的不是玩家，说明有障碍物遮挡
            if (!hit.collider.CompareTag("Player"))
            {
                isPlayerInSight = false;
                return;
            }
        }

        // 所有条件满足
        if (!wasPlayerInSight) // 只在状态变化时输出，避免日志过多
        {
            Debug.Log($"P5: 成功检测到玩家！距离: {distance:F1}, 角度: {angle:F1}");
        }
        isPlayerInSight = true;
    }

    /// <summary>
    /// 检查玩家是否逃脱到足够远的距离
    /// </summary>
    private void CheckPlayerEscape()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > escapeDistance)
        {
            Debug.Log($"P5: 玩家逃脱到安全距离 ({distanceToPlayer:F1} > {escapeDistance})，恢复巡逻");
            isPlayerInSight = false;
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// 玩家被检测到的处理逻辑
    /// </summary>
    private void HandlePlayerDetected()
    {
        // 立即停止移动
        if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            Debug.Log("P5: 已停止导航代理");
        }

        // 转向玩家
        if (player != null)
        {
            Vector3 targetLookDir = (player.position - transform.position).normalized;
            targetLookDir.y = 0;
            if (targetLookDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetLookDir);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            }
        }

        // 切换警戒色
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = alertColor;
        }
    }

    /// <summary>
    /// 重置警戒状态
    /// </summary>
    private void ResetAlertState()
    {
        if (purifierRenderer != null && purifierRenderer.material.color != normalColor)
        {
            purifierRenderer.material.color = normalColor;
        }
    }

    /// <summary>
    /// 巡逻行为逻辑
    /// </summary>
    private void PatrolBehavior()
    {
        // 只有在确实没有检测到玩家时才恢复移动
        if (!isPlayerInSight && agent.isStopped)
        {
            agent.isStopped = false;
        }

        switch (currentPatrolState)
        {
            case PatrolState.Moving:
                HandleMovingState();
                break;
            case PatrolState.Waiting:
                HandleWaitingState();
                break;
        }
    }

    private void HandleMovingState()
    {
        if (P5_patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
        {
            currentPatrolState = PatrolState.Waiting;
            waitCounter = waitTimeAtPoint;
            agent.isStopped = true;
            Debug.Log($"P5: 到达巡逻点 {currentPatrolIndex}，等待 {waitTimeAtPoint}秒");
        }
    }

    private void HandleWaitingState()
    {
        waitCounter -= Time.deltaTime;

        if (waitCounter <= 0f)
        {
            currentPatrolState = PatrolState.Moving;
            currentPatrolIndex = (currentPatrolIndex + 1) % P5_patrolPoints.Length;
            SetNextPatrolTarget();
            agent.isStopped = false;
        }
    }

    private void SetNextPatrolTarget()
    {
        if (P5_patrolPoints.Length > 0 && P5_patrolPoints[currentPatrolIndex] != null && agent != null)
        {
            agent.SetDestination(P5_patrolPoints[currentPatrolIndex].position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 视野半径 - 绿色
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // 逃脱距离 - 黄色
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, escapeDistance);

        // 视野角度锥形区域 - 半透明绿色
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        Vector3 forward = transform.forward * visionRadius;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2, 0) * forward;

        // 绘制视野锥形
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawLine(transform.position + leftBoundary, transform.position + rightBoundary);

        // 显示射线起点（调试用）
        Vector3 rayStart = transform.position + Vector3.up * 1.2f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rayStart, 0.1f);

        // 显示检测线
        if (player != null)
        {
            Vector3 playerCenter = player.position + Vector3.up * 1.0f;
            if (isPlayerInSight)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rayStart, playerCenter);
            }
            else
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(rayStart, playerCenter);
            }

            // 显示当前距离文本（需要Unity 2019.3+）
#if UNITY_EDITOR
            float distance = Vector3.Distance(transform.position, player.position);
            string distanceText = $"距离: {distance:F1}\n视野: {visionRadius}\n检测: {isPlayerInSight}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, distanceText);
#endif
        }
    }

    // 添加一个简单的调试方法，可以在Inspector中调用
    [ContextMenu("强制检测玩家")]
    private void ForceDetectPlayer()
    {
        Debug.Log("=== 强制检测玩家 ===");
        if (player == null)
        {
            Debug.LogError("玩家对象为空！");
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, (player.position - transform.position).normalized);

        Debug.Log($"玩家距离: {distance:F2}");
        Debug.Log($"玩家角度: {angle:F2} (最大允许: {visionAngle / 2})");
        Debug.Log($"视野半径: {visionRadius}");
        Debug.Log($"逃脱距离: {escapeDistance}");

        CheckPlayerInSight();
        Debug.Log($"最终检测结果: {isPlayerInSight}");
    }

    [ContextMenu("显示当前设置")]
    private void ShowCurrentSettings()
    {
        Debug.Log("=== P5当前设置 ===");
        Debug.Log($"视野半径: {visionRadius}");
        Debug.Log($"视野角度: {visionAngle}");
        Debug.Log($"逃脱距离: {escapeDistance}");
        Debug.Log($"巡逻点数量: {P5_patrolPoints?.Length ?? 0}");
        Debug.Log($"障碍物层级: {obstacleMask.value}");
    }
}