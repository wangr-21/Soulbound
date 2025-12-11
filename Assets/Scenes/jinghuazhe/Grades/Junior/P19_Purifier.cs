using UnityEngine;
using UnityEngine.AI;

public class P19_Purifier : MonoBehaviour
{
    [Header("=== P19专属设置 ===")]
    public Transform[] P19_patrolPoints;
    [SerializeField] private float waitTimeAtPoint = 1.5f;
    [SerializeField] private float pointArrivalDistance = 0.5f;

    [Header("=== AI Components ===")]
    private NavMeshAgent agent;
    private Transform player;

    [Header("=== Movement Settings ===")]
    [SerializeField] private float normalWalkSpeed = 2f;
    [SerializeField] private float fastWalkSpeed = 6f;

    [Header("=== Animation Settings ===")]
    [SerializeField] private Animator animator;
    private string walkParameterName = "IsWalking";

    [Header("=== Particle Effects ===")]
    [SerializeField] private ParticleSystem alertParticleSystem;

    [Header("=== 视野设置 (可视化) ===")]
    [SerializeField] private float sightRange = 8f;
    [SerializeField] private float sightAngle = 60f;
    [SerializeField] private Color sightColor = new Color(1, 0, 0, 0.2f);
    [SerializeField] private Color sightDetectedColor = new Color(0, 1, 0, 0.2f);
    [SerializeField] private bool drawSightGizmos = true;
    [SerializeField] private bool drawRaycastGizmos = true;

    private int currentPatrolIndex = 0;
    private float waitCounter = 0f;
    private bool isPlayerInSight = false;
    private bool isAlertParticlePlaying = false;
    private bool isInitialized = false;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private ParticleSystem.MainModule particleMainModule;

    private enum PatrolState { Moving, Waiting }
    private PatrolState currentPatrolState = PatrolState.Moving;

    void Start()
    {
        InitializePurifier();
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void InitializePurifier()
    {
        // 获取组件
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("P19: 找不到NavMeshAgent组件！");
            return;
        }

        // 初始化NavMeshAgent
        agent.speed = normalWalkSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = pointArrivalDistance;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // 查找玩家
        FindPlayer();

        // 初始化粒子系统
        InitializeParticleSystem();

        // 检查动画参数
        FindCorrectAnimationParameter();

        // 开始巡逻
        InitializePatrol();

        isInitialized = true;
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("P19: 未找到玩家对象（需添加Player标签）！");
        }
    }

    private void InitializeParticleSystem()
    {
        isAlertParticlePlaying = false;

        if (alertParticleSystem != null)
        {
            particleMainModule = alertParticleSystem.main;
            particleMainModule.playOnAwake = false;

            // 强制停止并清空粒子
            alertParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void FindCorrectAnimationParameter()
    {
        if (animator == null) return;

        string[] possibleNames = { "Walk", "isWalking", "Walking", "Move", "IsMoving" };
        foreach (string name in possibleNames)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == name && param.type == AnimatorControllerParameterType.Bool)
                {
                    walkParameterName = name;
                    return;
                }
            }
        }

        // 备用方案
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
            {
                walkParameterName = param.name;
                return;
            }
        }
    }

    private void InitializePatrol()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("P19: 代理不在NavMesh上！");
            return;
        }

        if (P19_patrolPoints == null || P19_patrolPoints.Length == 0)
        {
            Debug.LogError("P19: 巡逻点数组为空！");
            return;
        }

        // 验证巡逻点
        for (int i = 0; i < P19_patrolPoints.Length; i++)
        {
            if (P19_patrolPoints[i] == null)
            {
                Debug.LogError($"P19: 巡逻点 {i} 为null！");
                return;
            }
        }

        currentPatrolIndex = 0;
        currentPatrolState = PatrolState.Moving;
        SetPatrolTargetEnhanced(currentPatrolIndex);
    }

    void Update()
    {
        if (!isInitialized || agent == null) return;

        // 视野检测
        bool previousSight = isPlayerInSight;
        CheckPlayerInSight();

        // 状态变化处理
        if (isPlayerInSight && !previousSight)
        {
            OnPlayerDetected();
        }
        else if (!isPlayerInSight && previousSight)
        {
            OnPlayerLost();
        }

        // 执行行为
        if (isPlayerInSight)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        // 更新动画
        UpdateAnimation();

        // 同步粒子状态
        SyncParticleState();
    }

    // 同步粒子系统实际状态和标记
    private void SyncParticleState()
    {
        if (alertParticleSystem == null) return;

        if (alertParticleSystem.isPlaying != isAlertParticlePlaying)
        {
            isAlertParticlePlaying = alertParticleSystem.isPlaying;
        }
    }

    private void CheckPlayerInSight()
    {
        isPlayerInSight = false;

        if (player == null) return;

        // 距离检查
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > sightRange) return;

        // 角度检查
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > sightAngle) return;

        // 障碍物检查
        Vector3 rayStart = transform.position + Vector3.up * 1.5f;
        Vector3 rayTarget = player.position + Vector3.up * 1f;
        Vector3 rayDirection = (rayTarget - rayStart).normalized;

        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, sightRange))
        {
            if (drawRaycastGizmos)
            {
                Debug.DrawLine(rayStart, hit.point, hit.collider.CompareTag("Player") ? Color.green : Color.red);
            }

            if (hit.collider.CompareTag("Player"))
            {
                isPlayerInSight = true;
            }
        }
        else if (drawRaycastGizmos)
        {
            Debug.DrawLine(rayStart, rayStart + rayDirection * sightRange, Color.yellow);
        }
    }

    // 发现玩家时触发
    private void OnPlayerDetected()
    {
        agent.speed = fastWalkSpeed;

        // 播放粒子特效
        if (alertParticleSystem != null && !isAlertParticlePlaying)
        {
            alertParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            alertParticleSystem.Play(true);
            isAlertParticlePlaying = true;
        }
    }

    // 丢失玩家视野时触发
    private void OnPlayerLost()
    {
        agent.speed = normalWalkSpeed;

        // 停止粒子特效
        if (alertParticleSystem != null && isAlertParticlePlaying)
        {
            alertParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            isAlertParticlePlaying = false;
        }

        ReturnToPatrol();
    }

    private void ChasePlayer()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);

            // 超出逃脱距离则丢失视野
            if (Vector3.Distance(transform.position, player.position) > sightRange * 1.5f)
            {
                isPlayerInSight = false;
            }
        }
    }

    private void Patrol()
    {
        switch (currentPatrolState)
        {
            case PatrolState.Moving:
                HandleMovingStateEnhanced();
                break;
            case PatrolState.Waiting:
                HandleWaitingState();
                break;
        }
    }

    private void HandleMovingStateEnhanced()
    {
        if (P19_patrolPoints.Length == 0) return;

        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            if (!SetPatrolTargetEnhanced(currentPatrolIndex))
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % P19_patrolPoints.Length;
                SetPatrolTargetEnhanced(currentPatrolIndex);
            }
            return;
        }

        // 防卡住逻辑
        if (agent.remainingDistance > pointArrivalDistance &&
            agent.velocity.magnitude < 0.1f &&
            !agent.pathPending)
        {
            agent.ResetPath();
            SetPatrolTargetEnhanced(currentPatrolIndex);
            return;
        }

        // 旋转修复
        FixRotationIssue();

        // 到达巡逻点
        if (agent.remainingDistance <= pointArrivalDistance && !agent.pathPending)
        {
            currentPatrolState = PatrolState.Waiting;
            waitCounter = waitTimeAtPoint;
        }
    }

    private void HandleWaitingState()
    {
        waitCounter -= Time.deltaTime;

        if (waitCounter <= 0f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % P19_patrolPoints.Length;
            currentPatrolState = PatrolState.Moving;
            SetPatrolTargetEnhanced(currentPatrolIndex);
        }
    }

    private bool SetPatrolTargetEnhanced(int index)
    {
        if (!IsPatrolValid(index)) return false;

        Vector3 targetPosition = P19_patrolPoints[index].position;

        // 检查NavMesh有效性
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        else
        {
            return false;
        }

        // 检查路径可达性
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(targetPosition, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(targetPosition);
                return true;
            }
            else if (NavMesh.FindClosestEdge(targetPosition, out NavMeshHit edgeHit, NavMesh.AllAreas))
            {
                agent.SetDestination(edgeHit.position);
                return true;
            }
        }

        return false;
    }

    private bool IsPatrolValid(int index)
    {
        if (P19_patrolPoints == null || P19_patrolPoints.Length == 0) return false;
        if (index < 0 || index >= P19_patrolPoints.Length) return false;
        if (P19_patrolPoints[index] == null) return false;
        if (agent == null) return false;

        return true;
    }

    private void FixRotationIssue()
    {
        if (agent.hasPath && agent.remainingDistance > pointArrivalDistance)
        {
            float rotationChange = Quaternion.Angle(transform.rotation, lastRotation);

            if (agent.velocity.magnitude < 0.1f && rotationChange > 5f)
            {
                agent.updateRotation = false;

                Vector3 direction = (agent.steeringTarget - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
                }
            }
            else
            {
                agent.updateRotation = true;
            }
        }

        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void ReturnToPatrol()
    {
        if (currentPatrolState == PatrolState.Moving)
        {
            SetPatrolTargetEnhanced(currentPatrolIndex);
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool shouldWalk = (currentPatrolState == PatrolState.Moving && agent.remainingDistance > pointArrivalDistance) ||
                         (isPlayerInSight && agent.remainingDistance > pointArrivalDistance);

        animator.SetBool(walkParameterName, shouldWalk);
    }

    private void OnDrawGizmos()
    {
        // 绘制视野范围
        DrawSightGizmos();

        // 绘制导航路径
        if (isInitialized && agent != null && agent.hasPath)
        {
            Gizmos.color = isPlayerInSight ? Color.red : Color.blue;
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
            }
        }

        // 绘制巡逻点
        if (P19_patrolPoints != null)
        {
            // 当前目标点
            if (currentPatrolIndex < P19_patrolPoints.Length && P19_patrolPoints[currentPatrolIndex] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(P19_patrolPoints[currentPatrolIndex].position, 0.5f);
            }

            // 所有巡逻点
            for (int i = 0; i < P19_patrolPoints.Length; i++)
            {
                if (P19_patrolPoints[i] != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(P19_patrolPoints[i].position, 0.2f);
                }
            }
        }
    }

    private void DrawSightGizmos()
    {
        if (!drawSightGizmos) return;

        Gizmos.color = isPlayerInSight ? sightDetectedColor : sightColor;
        Vector3 center = transform.position + Vector3.up * 0.5f;

        // 视野锥外框
        Vector3 forward = transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, sightAngle, 0) * forward * sightRange;
        Vector3 leftBoundary = Quaternion.Euler(0, -sightAngle, 0) * forward * sightRange;

        Gizmos.DrawLine(center, center + forward * sightRange);
        Gizmos.DrawLine(center, center + rightBoundary);
        Gizmos.DrawLine(center, center + leftBoundary);

        // 视野半径
        Gizmos.DrawWireSphere(center, sightRange);

        // 扇形填充
        int segments = 16;
        Vector3 prevPoint = center + Quaternion.Euler(0, -sightAngle, 0) * forward * sightRange;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -sightAngle + (sightAngle * 2) * (i / (float)segments);
            Vector3 currPoint = center + Quaternion.Euler(0, angle, 0) * forward * sightRange;
            Gizmos.DrawLine(prevPoint, currPoint);
            prevPoint = currPoint;
        }
    }

    // 调试用上下文菜单（保留核心功能）
    [ContextMenu("强制重新巡逻")]
    private void ForceRestartPatrol()
    {
        currentPatrolIndex = 0;
        currentPatrolState = PatrolState.Moving;
        SetPatrolTargetEnhanced(currentPatrolIndex);
    }

    [ContextMenu("手动停止粒子特效")]
    private void ManualStopParticle()
    {
        if (alertParticleSystem != null)
        {
            alertParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            isAlertParticlePlaying = false;
        }
    }

    [ContextMenu("手动播放粒子特效")]
    private void ManualPlayParticle()
    {
        if (alertParticleSystem != null)
        {
            alertParticleSystem.Play(true);
            isAlertParticlePlaying = true;
        }
    }
}