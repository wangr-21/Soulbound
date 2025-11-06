using UnityEngine;
using UnityEngine.AI;

public class P6_Purifier : MonoBehaviour
{
    [Header("=== P6专属设置 ===")]
    public Transform[] P6_patrolPoints;  // P6专属巡逻点（与P3功能一致）
    [SerializeField] private float waitTimeAtPoint = 1.5f;  // 与P3相同的停留时间
    [SerializeField] private float pointArrivalDistance = 0.5f;  // 与P3相同的到达判定距离

    [Header("=== AI Components ===")]
    private NavMeshAgent agent;
    private Transform player;

    [Header("=== Vision Settings (与P3一致) ===")]
    [SerializeField] private float visionRadius = 4f;  // 与P3相同的视野半径
    [SerializeField] private float visionAngle = 120f;  // 与P3相同的视野角度
    [SerializeField] private LayerMask obstacleMask;  // 相同的障碍物检测层

    [Header("=== Alert Reactions ===")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.7f, 1f);  // 浅蓝（与P3的蓝色区分实例）
    [SerializeField] private Color alertColor = new Color(1f, 0.3f, 1f);   // 浅品红（与P3的品红区分实例）
    [SerializeField] private float rotateSpeed = 5f;  // 与P3相同的转向速度

    private Renderer purifierRenderer;
    private int currentPatrolIndex = 0;
    private float waitCounter = 0f;
    private bool isPlayerInSight = false;

    private enum PatrolState { Moving, Waiting }
    private PatrolState currentPatrolState = PatrolState.Moving;

    void Start()
    {
        // 初始化导航组件（与P3逻辑一致）
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("P6 找不到NavMeshAgent组件！");
            return;
        }

        // 查找玩家（与P3逻辑一致）
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("P6 未找到标签为Player的物体！");
        }

        // 初始化渲染器（与P3逻辑一致）
        purifierRenderer = GetComponent<Renderer>();
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = normalColor;
        }

        // 开始巡逻（与P3逻辑一致）
        if (P6_patrolPoints != null && P6_patrolPoints.Length > 0)
        {
            SetNextPatrolTarget();
            Debug.Log("P6 脚本启动（与P3等价），开始巡逻");
        }
        else
        {
            Debug.LogWarning("P6 未设置巡逻点！");
        }
    }

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // 视野检测（与P3完全一致的逻辑：距离+角度+障碍物）
        CheckPlayerInSight();

        if (!isPlayerInSight)
        {
            PatrolBehavior();
            ResetAlertState();
        }
        else
        {
            HandlePlayerDetected();
        }
    }

    /// <summary>
    /// 与P3完全一致的视野检测逻辑
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
        if (distance > visionRadius)
        {
            isPlayerInSight = false;
            return;
        }

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > visionAngle / 2)
        {
            isPlayerInSight = false;
            return;
        }

        if (Physics.Raycast(transform.position, toPlayer.normalized, distance, obstacleMask))
        {
            isPlayerInSight = false;
            return;
        }

        isPlayerInSight = true;
    }

    /// <summary>
    /// 与P3完全一致的玩家检测处理（转向+变色）
    /// </summary>
    private void HandlePlayerDetected()
    {
        agent.isStopped = true;

        if (player != null)
        {
            Vector3 targetLookDir = (player.position - transform.position).normalized;
            targetLookDir.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(targetLookDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = alertColor;
        }
    }

    /// <summary>
    /// 与P3一致的警戒状态重置
    /// </summary>
    private void ResetAlertState()
    {
        if (purifierRenderer != null && purifierRenderer.material.color != normalColor)
        {
            purifierRenderer.material.color = normalColor;
        }
    }

    /// <summary>
    /// 与P3一致的巡逻行为
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
    /// 与P3一致的移动状态处理
    /// </summary>
    private void HandleMovingState()
    {
        if (P6_patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
        {
            currentPatrolState = PatrolState.Waiting;
            waitCounter = waitTimeAtPoint;
            agent.isStopped = true;
        }
    }

    /// <summary>
    /// 与P3一致的等待状态处理
    /// </summary>
    private void HandleWaitingState()
    {
        waitCounter -= Time.deltaTime;

        if (waitCounter <= 0f)
        {
            currentPatrolState = PatrolState.Moving;
            currentPatrolIndex = (currentPatrolIndex + 1) % P6_patrolPoints.Length;
            SetNextPatrolTarget();
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// 与P3一致的巡逻点设置
    /// </summary>
    private void SetNextPatrolTarget()
    {
        if (P6_patrolPoints.Length > 0 && P6_patrolPoints[currentPatrolIndex] != null && agent != null)
        {
            agent.SetDestination(P6_patrolPoints[currentPatrolIndex].position);
            Debug.Log($"P6 移动到巡逻点 {currentPatrolIndex}（与P3等价）");
        }
    }

    /// <summary>
    /// 与P3一致的调试Gizmos（仅颜色微调以区分实例）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.9f, 1f);  // 浅青色（与P3的青色区分）
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Vector3 fovLine1 = Quaternion.Euler(0, visionAngle / 2, 0) * transform.forward * visionRadius;
        Vector3 fovLine2 = Quaternion.Euler(0, -visionAngle / 2, 0) * transform.forward * visionRadius;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);

        if (isPlayerInSight && player != null)
        {
            Gizmos.color = new Color(1f, 0.6f, 1f);  // 浅品红视线
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}