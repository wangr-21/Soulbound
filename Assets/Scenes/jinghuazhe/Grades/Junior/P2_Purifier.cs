using UnityEngine;
using UnityEngine.AI;

public class P2_Purifier : MonoBehaviour
{
    [Header("=== P2专属设置 ===")]
    public Transform[] P2_patrolPoints;  // P2专属巡逻点
    [SerializeField] private float waitTimeAtPoint = 1.8f;  // 停留时间（介于P1和P3之间）
    [SerializeField] private float pointArrivalDistance = 0.4f;  // 到达判定距离（介于P1和P3之间）

    [Header("=== AI Components ===")]
    private NavMeshAgent agent;
    private Transform player;

    [Header("=== Vision Settings (P2特性) ===")]
    [SerializeField] private float visionRadius = 3.5f;  // 视野半径（比P1远，比P3近）
    [SerializeField] private float visionAngle = 105f;  // 视野角度（比P1宽，比P3窄）
    [SerializeField] private LayerMask obstacleMask;  // 障碍物检测层（统一增强特性）

    [Header("=== Alert Reactions ===")]
    [SerializeField] private Color normalColor = Color.green;  // P2默认绿色（独特标识）
    [SerializeField] private Color alertColor = Color.yellow;  // 警戒色为黄色（区分P1/P3）
    [SerializeField] private float rotateSpeed = 4.5f;  // 转向速度（介于P1和P3之间）

    private Renderer purifierRenderer;
    private int currentPatrolIndex = 0;
    private float waitCounter = 0f;
    private bool isPlayerInSight = false;

    private enum PatrolState { Moving, Waiting }
    private PatrolState currentPatrolState = PatrolState.Moving;

    void Start()
    {
        // 初始化导航组件
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("P2 找不到NavMeshAgent组件！");
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
            Debug.LogWarning("P2 未找到标签为Player的物体！");
        }

        // 初始化渲染器
        purifierRenderer = GetComponent<Renderer>();
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = normalColor;
        }

        // 开始巡逻
        if (P2_patrolPoints != null && P2_patrolPoints.Length > 0)
        {
            SetNextPatrolTarget();
            Debug.Log("P2 增强版脚本启动，开始巡逻");
        }
        else
        {
            Debug.LogWarning("P2 未设置巡逻点！");
        }
    }

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // 增强版视野检测（距离+角度+障碍物遮挡）
        CheckPlayerInSight();

        if (!isPlayerInSight)
        {
            // 未检测到玩家时执行巡逻，并重置警戒状态
            PatrolBehavior();
            ResetAlertState();
        }
        else
        {
            // 检测到玩家时的处理（转向行为）
            HandlePlayerDetected();
        }
    }

    /// <summary>
    /// 增强版视野检测：距离+角度+无遮挡
    /// </summary>
    private void CheckPlayerInSight()
    {
        if (player == null)
        {
            isPlayerInSight = false;
            return;
        }

        // 1. 距离检测
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
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

        // 3. 障碍物遮挡检测
        if (Physics.Raycast(transform.position, toPlayer.normalized, distance, obstacleMask))
        {
            isPlayerInSight = false;
            return;
        }

        // 所有条件满足
        isPlayerInSight = true;
    }

    /// <summary>
    /// 玩家被检测到的处理逻辑（转向+变色）
    /// </summary>
    private void HandlePlayerDetected()
    {
        agent.isStopped = true;

        // 平滑转向玩家（忽略Y轴旋转）
        if (player != null)
        {
            Vector3 targetLookDir = (player.position - transform.position).normalized;
            targetLookDir.y = 0;  // 保持水平转向
            Quaternion targetRotation = Quaternion.LookRotation(targetLookDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        // 切换警戒色
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = alertColor;
        }
    }

    /// <summary>
    /// 重置警戒状态（恢复默认颜色）
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
        agent.isStopped = false;

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

    /// <summary>
    /// 移动状态处理（到达目标点后切换等待）
    /// </summary>
    private void HandleMovingState()
    {
        if (P2_patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
        {
            currentPatrolState = PatrolState.Waiting;
            waitCounter = waitTimeAtPoint;
            agent.isStopped = true;
        }
    }

    /// <summary>
    /// 等待状态处理（等待结束后切换下一个巡逻点）
    /// </summary>
    private void HandleWaitingState()
    {
        waitCounter -= Time.deltaTime;

        if (waitCounter <= 0f)
        {
            currentPatrolState = PatrolState.Moving;
            currentPatrolIndex = (currentPatrolIndex + 1) % P2_patrolPoints.Length;
            SetNextPatrolTarget();
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// 设置下一个巡逻目标
    /// </summary>
    private void SetNextPatrolTarget()
    {
        if (P2_patrolPoints.Length > 0 && P2_patrolPoints[currentPatrolIndex] != null && agent != null)
        {
            agent.SetDestination(P2_patrolPoints[currentPatrolIndex].position);
            Debug.Log($"P2 移动到巡逻点 {currentPatrolIndex}");
        }
    }

    /// <summary>
    /// 调试Gizmos（Scene视图可视化视野）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 视野半径（绿色标识P2）
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // 视野角度边界线
        Vector3 fovLine1 = Quaternion.Euler(0, visionAngle / 2, 0) * transform.forward * visionRadius;
        Vector3 fovLine2 = Quaternion.Euler(0, -visionAngle / 2, 0) * transform.forward * visionRadius;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);

        // 检测到玩家时绘制视线
        if (isPlayerInSight && player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}