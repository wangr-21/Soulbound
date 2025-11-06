using UnityEngine;
using UnityEngine.AI;

public class P3_Purifier : MonoBehaviour
{
    [Header("=== P3专属设置 ===")]
    public Transform[] P3_patrolPoints;  // P3专属巡逻点
    [SerializeField] private float waitTimeAtPoint = 1.5f;  // 停留时间比P1稍短
    [SerializeField] private float pointArrivalDistance = 0.5f;  // 到达判定距离稍大

    [Header("=== AI Components ===")]
    private NavMeshAgent agent;
    private Transform player;

    [Header("=== Vision Settings (P3视野更宽) ===")]
    [SerializeField] private float visionRadius = 4f;  // 视野范围比P1更远
    [SerializeField] private float visionAngle = 120f;  // 视野角度比P1更宽
    [SerializeField] private LayerMask obstacleMask;  // 新增障碍物检测层

    [Header("=== Alert Reactions ===")]
    [SerializeField] private Color normalColor = Color.blue;  // P3默认蓝色
    [SerializeField] private Color alertColor = Color.magenta;  // 警戒色为品红
    [SerializeField] private float rotateSpeed = 5f;  // 检测到玩家时的转向速度

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
            Debug.LogError("P3 找不到NavMeshAgent组件！");
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
            Debug.LogWarning("P3 未找到标签为Player的物体！");
        }

        // 初始化渲染器
        purifierRenderer = GetComponent<Renderer>();
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = normalColor;
        }

        // 开始巡逻
        if (P3_patrolPoints != null && P3_patrolPoints.Length > 0)
        {
            SetNextPatrolTarget();
            Debug.Log("P3 专属脚本启动，开始巡逻");
        }
        else
        {
            Debug.LogWarning("P3 未设置巡逻点！");
        }
    }

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // 检测玩家是否在视野内（P3增加了角度和障碍物检测）
        CheckPlayerInSight();

        if (!isPlayerInSight)
        {
            // 未检测到玩家时执行巡逻行为
            PatrolBehavior();
            ResetAlertState();
        }
        else
        {
            // 检测到玩家时的处理
            HandlePlayerDetected();
        }
    }

    /// <summary>
    /// P3的视野检测（比P1更完善：距离+角度+障碍物遮挡）
    /// </summary>
    private void CheckPlayerInSight()
    {
        if (player == null)
        {
            isPlayerInSight = false;
            return;
        }

        // 1. 检测距离是否在视野半径内
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > visionRadius)
        {
            isPlayerInSight = false;
            return;
        }

        // 2. 检测角度是否在视野范围内
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > visionAngle / 2)
        {
            isPlayerInSight = false;
            return;
        }

        // 3. 检测是否有障碍物遮挡
        if (Physics.Raycast(transform.position, toPlayer.normalized, distance, obstacleMask))
        {
            isPlayerInSight = false;
            return;
        }

        // 所有条件满足，检测到玩家
        isPlayerInSight = true;
    }

    /// <summary>
    /// 处理玩家被检测到的逻辑（P3会转向玩家）
    /// </summary>
    private void HandlePlayerDetected()
    {
        agent.isStopped = true;  // 停止巡逻

        // 转向玩家
        if (player != null)
        {
            Vector3 targetLookDir = (player.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetLookDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        // 改变颜色为警戒色
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = alertColor;
        }
    }

    /// <summary>
    /// 重置警戒状态（恢复颜色）
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
    /// 处理移动状态
    /// </summary>
    private void HandleMovingState()
    {
        if (P3_patrolPoints.Length == 0) return;

        // 到达目标点后切换到等待状态
        if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
        {
            currentPatrolState = PatrolState.Waiting;
            waitCounter = waitTimeAtPoint;
            agent.isStopped = true;
        }
    }

    /// <summary>
    /// 处理等待状态
    /// </summary>
    private void HandleWaitingState()
    {
        waitCounter -= Time.deltaTime;

        if (waitCounter <= 0f)
        {
            // 等待结束，切换到下一个巡逻点
            currentPatrolState = PatrolState.Moving;
            currentPatrolIndex = (currentPatrolIndex + 1) % P3_patrolPoints.Length;
            SetNextPatrolTarget();
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// 设置下一个巡逻目标点
    /// </summary>
    private void SetNextPatrolTarget()
    {
        if (P3_patrolPoints.Length > 0 && P3_patrolPoints[currentPatrolIndex] != null && agent != null)
        {
            agent.SetDestination(P3_patrolPoints[currentPatrolIndex].position);
            Debug.Log($"P3 移动到巡逻点 {currentPatrolIndex}");
        }
    }

    // 绘制Gizmos辅助调试
    private void OnDrawGizmosSelected()
    {
        // 绘制视野范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // 绘制视野角度
        Vector3 fovLine1 = Quaternion.Euler(0, visionAngle / 2, 0) * transform.forward * visionRadius;
        Vector3 fovLine2 = Quaternion.Euler(0, -visionAngle / 2, 0) * transform.forward * visionRadius;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);
    }
}