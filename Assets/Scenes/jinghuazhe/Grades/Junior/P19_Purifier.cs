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

    private int currentPatrolIndex = 0;
    private float waitCounter = 0f;
    private bool isPlayerInSight = false;
    private bool isAlertParticlePlaying = false;
    private bool isInitialized = false;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

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
        Debug.Log("=== P19初始化开始 ===");

        // 获取组件
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("P19: 找不到NavMeshAgent组件！");
            return;
        }

        // 重置NavMeshAgent设置
        agent.speed = normalWalkSpeed;
        agent.angularSpeed = 120f; // 降低旋转速度避免打转
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
        Debug.Log("=== P19初始化完成 ===");
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("P19: 找到玩家对象");
        }
        else
        {
            Debug.LogWarning("P19: 未找到玩家对象");
        }
    }

    private void InitializeParticleSystem()
    {
        if (alertParticleSystem != null)
        {
            alertParticleSystem.Stop();
            isAlertParticlePlaying = false;
        }
    }

    private void FindCorrectAnimationParameter()
    {
        if (animator == null)
        {
            Debug.LogWarning("P19: 没有Animator组件");
            return;
        }

        // 尝试常见的行走参数名
        string[] possibleNames = { "Walk", "isWalking", "Walking", "Move", "IsMoving" };
        foreach (string name in possibleNames)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == name && param.type == AnimatorControllerParameterType.Bool)
                {
                    walkParameterName = name;
                    Debug.Log($"P19: 使用动画参数 '{walkParameterName}'");
                    return;
                }
            }
        }

        // 如果没有找到，使用第一个布尔参数
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
            {
                walkParameterName = param.name;
                Debug.LogWarning($"P19: 使用找到的备用参数 '{walkParameterName}'");
                return;
            }
        }

        Debug.LogWarning("P19: 没有找到可用的行走动画参数");
    }

    private void InitializePatrol()
    {
        // 检查是否在NavMesh上
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("P19: 代理不在NavMesh上！需要重新放置对象或重新烘焙NavMesh");
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

        if (SetPatrolTargetEnhanced(currentPatrolIndex))
        {
            Debug.Log($"P19: 开始巡逻，共有 {P19_patrolPoints.Length} 个巡逻点");
        }
        else
        {
            Debug.LogError("P19: 无法设置初始巡逻目标！");
        }
    }

    void Update()
    {
        if (!isInitialized || agent == null) return;

        // 视野检测
        bool previousSight = isPlayerInSight;
        CheckPlayerInSight();

        // 处理状态变化
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

        // 调试信息
        DebugDisplay();
    }

    private void CheckPlayerInSight()
    {
        if (player == null)
        {
            isPlayerInSight = false;
            return;
        }

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // 距离检查
        if (distanceToPlayer > 8f) // 固定视野半径
        {
            isPlayerInSight = false;
            return;
        }

        // 角度检查
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > 60f) // 固定视野角度
        {
            isPlayerInSight = false;
            return;
        }

        // 障碍物检查
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, distanceToPlayer))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                isPlayerInSight = false;
                return;
            }
        }

        isPlayerInSight = true;
    }

    private void OnPlayerDetected()
    {
        Debug.Log("P19: 发现玩家！进入追逐状态");
        agent.speed = fastWalkSpeed;

        if (alertParticleSystem != null && !isAlertParticlePlaying)
        {
            alertParticleSystem.Play();
            isAlertParticlePlaying = true;
        }
    }

    private void OnPlayerLost()
    {
        Debug.Log("P19: 玩家消失，恢复巡逻");
        agent.speed = normalWalkSpeed;

        if (alertParticleSystem != null && isAlertParticlePlaying)
        {
            alertParticleSystem.Stop();
            isAlertParticlePlaying = false;
        }

        // 恢复巡逻
        ReturnToPatrol();
    }

    private void ChasePlayer()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);

            // 检查逃脱距离
            if (Vector3.Distance(transform.position, player.position) > 12f) // 固定逃脱距离
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

    /// <summary>
    /// 强化的移动状态处理
    /// </summary>
    private void HandleMovingStateEnhanced()
    {
        if (P19_patrolPoints.Length == 0) return;

        // 检查是否没有路径或路径无效
        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning("P19: 路径无效，重新计算路径");
            if (!SetPatrolTargetEnhanced(currentPatrolIndex))
            {
                // 如果当前点不可达，尝试下一个点
                currentPatrolIndex = (currentPatrolIndex + 1) % P19_patrolPoints.Length;
                SetPatrolTargetEnhanced(currentPatrolIndex);
            }
            return;
        }

        // 检查是否卡住
        if (agent.remainingDistance > pointArrivalDistance &&
            agent.velocity.magnitude < 0.1f &&
            !agent.pathPending)
        {
            Debug.LogWarning($"P19: 可能卡住，重新路径计算。剩余距离: {agent.remainingDistance}");

            // 重新计算路径
            agent.ResetPath();
            SetPatrolTargetEnhanced(currentPatrolIndex);
            return;
        }

        // 修复旋转问题
        FixRotationIssue();

        // 原始的距离检查逻辑
        if (agent.remainingDistance <= pointArrivalDistance && !agent.pathPending)
        {
            Debug.Log($"P19: 到达巡逻点 {currentPatrolIndex}，开始等待");
            currentPatrolState = PatrolState.Waiting;
            waitCounter = waitTimeAtPoint;
        }
    }

    private void HandleWaitingState()
    {
        waitCounter -= Time.deltaTime;

        if (waitCounter <= 0f)
        {
            // 移动到下一个点
            currentPatrolIndex = (currentPatrolIndex + 1) % P19_patrolPoints.Length;
            currentPatrolState = PatrolState.Moving;

            if (SetPatrolTargetEnhanced(currentPatrolIndex))
            {
                Debug.Log($"P19: 等待结束，前往巡逻点 {currentPatrolIndex}");
            }
        }
    }

    /// <summary>
    /// 强化的巡逻目标设置方法
    /// </summary>
    private bool SetPatrolTargetEnhanced(int index)
    {
        if (!IsPatrolValid(index)) return false;

        Vector3 targetPosition = P19_patrolPoints[index].position;

        // 检查目标点是否在NavMesh上
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            Debug.Log($"P19: 找到有效NavMesh位置 {targetPosition}");
        }
        else
        {
            Debug.LogError($"P19: 巡逻点 {index} 不在NavMesh上！位置: {targetPosition}");
            return false;
        }

        // 使用CalculatePath检查路径是否可达
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(targetPosition, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(targetPosition);
                Debug.Log($"P19: 成功设置巡逻目标 {index} - {targetPosition}");
                return true;
            }
            else
            {
                Debug.LogWarning($"P19: 路径不可达 (状态: {path.status})，尝试寻找最近可达点");

                // 尝试寻找最近的可达点
                if (NavMesh.FindClosestEdge(targetPosition, out NavMeshHit edgeHit, NavMesh.AllAreas))
                {
                    agent.SetDestination(edgeHit.position);
                    Debug.Log($"P19: 使用最近可达点 {edgeHit.position}");
                    return true;
                }
            }
        }

        Debug.LogError($"P19: 无法计算到巡逻点 {index} 的路径");
        return false;
    }

    private bool IsPatrolValid(int index)
    {
        if (P19_patrolPoints == null || P19_patrolPoints.Length == 0)
        {
            Debug.LogError("P19: 巡逻点数组为空");
            return false;
        }

        if (index < 0 || index >= P19_patrolPoints.Length)
        {
            Debug.LogError($"P19: 巡逻点索引 {index} 超出范围");
            return false;
        }

        if (P19_patrolPoints[index] == null)
        {
            Debug.LogError($"P19: 巡逻点 {index} 为null");
            return false;
        }

        if (agent == null)
        {
            Debug.LogError("P19: NavMeshAgent 为null");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 修复旋转问题 - 修复版本
    /// </summary>
    private void FixRotationIssue()
    {
        // 如果代理在移动但位置没有变化，可能是旋转问题
        if (agent.hasPath && agent.remainingDistance > pointArrivalDistance)
        {
            // 计算旋转角度变化
            float rotationChange = Quaternion.Angle(transform.rotation, lastRotation);

            // 检查是否在旋转但没有移动
            if (agent.velocity.magnitude < 0.1f && rotationChange > 5f)
            {
                Debug.LogWarning("P19: 检测到可能卡在旋转上，尝试修复");

                // 临时禁用自动旋转，手动控制朝向
                agent.updateRotation = false;

                Vector3 direction = (agent.steeringTarget - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                        Time.deltaTime * 2f); // 使用固定速度而不是angularSpeed
                }
            }
            else
            {
                // 恢复正常旋转控制
                agent.updateRotation = true;
            }
        }

        // 更新上一帧的位置和旋转
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

    private void DebugDisplay()
    {
        if (Time.frameCount % 120 == 0) // 每2秒输出一次
        {
            string state = isPlayerInSight ? "追逐" : "巡逻";
            string moveState = currentPatrolState.ToString();
            string pathInfo = agent.hasPath ? $"路径正常" : "无路径";

            Debug.Log($"P19状态: {state} | 移动: {moveState} | 目标点: {currentPatrolIndex} | {pathInfo} | 剩余距离: {agent.remainingDistance:F1}");
        }
    }

    private void OnDrawGizmos()
    {
        if (!isInitialized) return;

        // 绘制当前路径
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = isPlayerInSight ? Color.red : Color.blue;
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
            }
        }

        // 绘制当前目标点
        if (P19_patrolPoints != null && currentPatrolIndex < P19_patrolPoints.Length && P19_patrolPoints[currentPatrolIndex] != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(P19_patrolPoints[currentPatrolIndex].position, 0.5f);
        }
    }

    [ContextMenu("强制重新巡逻")]
    private void ForceRestartPatrol()
    {
        Debug.Log("=== 强制重新开始巡逻 ===");
        currentPatrolIndex = 0;
        currentPatrolState = PatrolState.Moving;
        SetPatrolTargetEnhanced(currentPatrolIndex);
    }

    [ContextMenu("显示NavMesh信息")]
    private void ShowNavMeshInfo()
    {
        Debug.Log("=== NavMesh信息 ===");
        Debug.Log($"Agent位置: {transform.position}");
        Debug.Log($"在NavMesh上: {NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas)}");
        Debug.Log($"可导航: {agent.isOnNavMesh}");
        Debug.Log($"有路径: {agent.hasPath}");
        Debug.Log($"路径状态: {agent.pathStatus}");

        if (P19_patrolPoints != null)
        {
            for (int i = 0; i < P19_patrolPoints.Length; i++)
            {
                if (P19_patrolPoints[i] != null)
                {
                    bool onNavMesh = NavMesh.SamplePosition(P19_patrolPoints[i].position, out hit, 1f, NavMesh.AllAreas);
                    Debug.Log($"巡逻点 {i}: {P19_patrolPoints[i].position} - 在NavMesh上: {onNavMesh}");
                }
            }
        }
    }

    [ContextMenu("诊断巡逻问题")]
    private void DiagnosePatrolIssue()
    {
        Debug.Log("=== P19巡逻问题诊断 ===");
        Debug.Log($"代理在NavMesh上: {agent.isOnNavMesh}");
        Debug.Log($"代理有路径: {agent.hasPath}");
        Debug.Log($"路径状态: {agent.pathStatus}");
        Debug.Log($"代理速度: {agent.velocity.magnitude}");
        Debug.Log($"剩余距离: {agent.remainingDistance}");
        Debug.Log($"是否路径计算中: {agent.pathPending}");

        if (P19_patrolPoints != null && currentPatrolIndex < P19_patrolPoints.Length)
        {
            Vector3 targetPos = P19_patrolPoints[currentPatrolIndex].position;
            Debug.Log($"当前目标点: {targetPos}");

            // 检查目标点是否在NavMesh上
            bool onNavMesh = NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas);
            Debug.Log($"目标点在NavMesh上: {onNavMesh}");

            if (onNavMesh)
            {
                // 检查路径是否可达
                NavMeshPath testPath = new NavMeshPath();
                if (agent.CalculatePath(targetPos, testPath))
                {
                    Debug.Log($"路径计算状态: {testPath.status}");
                }
            }
        }
    }

    [ContextMenu("重新初始化导航代理")]
    private void ReinitializeAgent()
    {
        Debug.Log("=== 重新初始化导航代理 ===");

        if (agent != null)
        {
            agent.enabled = false;
            agent.enabled = true;

            // 重新设置参数
            agent.speed = normalWalkSpeed;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = pointArrivalDistance;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.updateRotation = true;

            // 重新开始巡逻
            ForceRestartPatrol();
        }
    }
}