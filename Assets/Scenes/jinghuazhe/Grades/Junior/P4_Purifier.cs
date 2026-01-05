using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class P4_Purifier : MonoBehaviour
{
    [Header("=== P4专属设置 ===")]
    public Transform[] P4_patrolPoints;
    [SerializeField] private float waitTimeAtPoint = 1.5f;
    [SerializeField] private float pointArrivalDistance = 0.5f;

    [Header("=== AI Components ===")]
    private NavMeshAgent agent;
    private Transform player;

    [Header("=== Movement Settings ===")]
    [SerializeField] private float normalWalkSpeed = 2f;
    [SerializeField] private float fastWalkSpeed = 6f;

    [Header("=== 攻击设置 ===")]
    [SerializeField] private float attackRange = 1.5f; // 攻击范围
    [SerializeField] private float attackDamage = 20f; // 每次攻击伤害
    [SerializeField] private float attackCooldown = 2f; // 攻击冷却时间
    private float lastAttackTime = 0f; // 上次攻击时间
    [SerializeField] private AudioClip attackSound; // 攻击音效
    [SerializeField] private GameObject attackEffect; // 攻击特效

    [Header("=== 碰撞伤害设置 ===")]
    [SerializeField] private float collisionDamage = 20f; // 碰撞造成的伤害
    [SerializeField] private float collisionCooldown = 1f; // 碰撞伤害冷却时间
    private float lastCollisionTime = 0f; // 上次碰撞伤害时间
    [SerializeField] private AudioClip collisionSound; // 碰撞音效
    [SerializeField] private Color collisionColor = Color.red; // 碰撞时的颜色
    private Renderer purifierRenderer; // 净化者的渲染器
    private Color originalColor; // 原始颜色
    private Material originalMaterial; // 原始材质
    [SerializeField] private float collisionFlashDuration = 0.2f; // 颜色闪烁持续时间
    [SerializeField] private GameObject collisionEffect; // 碰撞特效预制体

    [Header("=== Animation Settings ===")]
    [SerializeField] private Animator animator;
    private string walkParameterName = "IsWalking";
    private string attackParameterName = "Attack"; // 攻击动画参数

    [Header("=== Particle Effects ===")]
    [SerializeField] private ParticleSystem alertParticleSystem;

    [Header("=== 视野设置 (可视化) ===")]
    [SerializeField] private float sightRange = 8f;
    [SerializeField] private float sightAngle = 60f;
    [SerializeField] private Color sightColor = new Color(1, 0, 0, 0.2f);
    [SerializeField] private Color sightDetectedColor = new Color(0, 1, 0, 0.2f);
    [SerializeField] private bool drawSightGizmos = true;
    [SerializeField] private bool drawRaycastGizmos = true;

    [Header("=== 碰撞体设置 ===")]
    [SerializeField] private float colliderRadius = 0.5f; // 碰撞体半径
    [SerializeField] private float colliderHeight = 1.5f; // 碰撞体高度
    private CapsuleCollider purifierCollider; // 净化者碰撞体

    [Header("=== 对不同动物的伤害设置 ===")]
    [SerializeField] private float deerDamageMultiplier = 1.0f; // 对鹿的伤害倍率
    [SerializeField] private float sheepDamageMultiplier = 1.0f; // 对绵羊的伤害倍率
    [SerializeField] private float foxDamageMultiplier = 1.0f; // 对狐狸的伤害倍率
    [SerializeField] private float birdDamageMultiplier = 1.0f; // 对鸟的伤害倍率

    private int currentPatrolIndex = 0;
    private float waitCounter = 0f;
    private bool isPlayerInSight = false;
    private bool isAlertParticlePlaying = false;
    private bool isInitialized = false;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private GameObject currentTarget; // 当前攻击目标
    private bool isAttacking = false; // 是否正在攻击

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
            Debug.LogError("P4: 找不到NavMeshAgent组件！");
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

        // 初始化渲染器和材质
        InitializeRenderer();

        // 初始化碰撞体
        InitializeCollider();

        // 初始化粒子系统
        InitializeParticleSystem();

        // 检查动画参数
        FindCorrectAnimationParameter();

        // 开始巡逻
        InitializePatrol();

        isInitialized = true;

        Debug.Log($"P4净化者初始化完成 - 位置: {transform.position}");
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"P4: 找到玩家: {player.name}");
        }
        else
        {
            Debug.LogError("P4: 未找到玩家对象（需添加Player标签）！");
        }
    }

    private void InitializeRenderer()
    {
        purifierRenderer = GetComponent<Renderer>();
        if (purifierRenderer != null)
        {
            originalMaterial = purifierRenderer.material;
            originalColor = purifierRenderer.material.color;
            Debug.Log($"P4: 渲染器初始化完成 - 材质: {originalMaterial.name}");
        }
        else
        {
            Debug.LogWarning("P4: 找不到Renderer组件，颜色闪烁效果将不可用");
        }
    }

    private void InitializeCollider()
    {
        // 检查是否已有碰撞体
        purifierCollider = GetComponent<CapsuleCollider>();
        if (purifierCollider == null)
        {
            // 如果没有碰撞体，添加一个CapsuleCollider
            purifierCollider = gameObject.AddComponent<CapsuleCollider>();
            purifierCollider.radius = colliderRadius;
            purifierCollider.height = colliderHeight;
            purifierCollider.center = new Vector3(0, colliderHeight / 2, 0);

            Debug.Log($"P4: 已添加CapsuleCollider - 半径: {colliderRadius}, 高度: {colliderHeight}");
        }
        else
        {
            // 调整现有碰撞体大小
            purifierCollider.radius = colliderRadius;
            purifierCollider.height = colliderHeight;
            purifierCollider.center = new Vector3(0, colliderHeight / 2, 0);
        }

        // 确保碰撞体不是触发器
        purifierCollider.isTrigger = false;

        // 添加Rigidbody用于更好的碰撞检测
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // 设置为运动学，避免物理影响移动
            rb.useGravity = false; // 不使用重力

            // 冻结所有旋转，防止旋转
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            Debug.Log("P4: 已添加Rigidbody组件（运动学，冻结旋转）");
        }
        else
        {
            // 确保现有的Rigidbody设置正确
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
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
            Debug.Log("P4: 警报粒子系统初始化完成");
        }
        else
        {
            Debug.LogWarning("P4: 未找到警报粒子系统");
        }
    }

    private void FindCorrectAnimationParameter()
    {
        if (animator == null)
        {
            Debug.LogWarning("P4: 未找到Animator组件");
            return;
        }

        // 寻找行走参数
        string[] possibleWalkNames = { "Walk", "isWalking", "Walking", "Move", "IsMoving" };
        foreach (string name in possibleWalkNames)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == name && param.type == AnimatorControllerParameterType.Bool)
                {
                    walkParameterName = name;
                    Debug.Log($"P4: 找到行走参数: {walkParameterName}");
                    break;
                }
            }
        }

        // 寻找攻击参数
        string[] possibleAttackNames = { "Attack", "Attacking", "isAttacking" };
        foreach (string name in possibleAttackNames)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == name && param.type == AnimatorControllerParameterType.Bool)
                {
                    attackParameterName = name;
                    Debug.Log($"P4: 找到攻击参数: {attackParameterName}");
                    break;
                }
            }
        }
    }

    private void InitializePatrol()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("P4: 代理不在NavMesh上！");
            return;
        }

        if (P4_patrolPoints == null || P4_patrolPoints.Length == 0)
        {
            Debug.LogError("P4: 巡逻点数组为空！");
            return;
        }

        // 验证巡逻点
        for (int i = 0; i < P4_patrolPoints.Length; i++)
        {
            if (P4_patrolPoints[i] == null)
            {
                Debug.LogError($"P4: 巡逻点 {i} 为null！");
                return;
            }
        }

        currentPatrolIndex = 0;
        currentPatrolState = PatrolState.Moving;
        SetPatrolTargetEnhanced(currentPatrolIndex);

        Debug.Log($"P4: 巡逻初始化完成 - 巡逻点数量: {P4_patrolPoints.Length}");
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

        // 检查攻击条件
        if (isPlayerInSight)
        {
            // 更新当前目标
            UpdateCurrentTarget();

            // 检查是否在攻击范围内
            if (IsTargetInAttackRange())
            {
                // 停止移动，准备攻击
                agent.isStopped = true;

                // 尝试攻击
                TryAttack();
            }
            else
            {
                // 不在攻击范围内，继续追逐
                agent.isStopped = false;
                ChasePlayer();
                isAttacking = false;
            }
        }
        else
        {
            agent.isStopped = false;
            Patrol();
            isAttacking = false;
        }

        // 更新动画
        UpdateAnimation();

        // 同步粒子状态
        SyncParticleState();
    }

    /// <summary>
    /// 更新当前攻击目标
    /// </summary>
    private void UpdateCurrentTarget()
    {
        // 优先攻击玩家灵魂
        if (player != null)
        {
            currentTarget = player.gameObject;
        }

        // 如果玩家附身在动物上，则攻击动物
        PlayerSoulController playerSoul = PlayerSoulController.Instance;
        if (playerSoul != null && playerSoul.isPossessing && playerSoul.currentPossessedObject != null)
        {
            currentTarget = playerSoul.currentPossessedObject;
        }
    }

    /// <summary>
    /// 检查目标是否在攻击范围内
    /// </summary>
    private bool IsTargetInAttackRange()
    {
        if (currentTarget == null) return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        return distance <= attackRange;
    }

    /// <summary>
    /// 尝试攻击
    /// </summary>
    private void TryAttack()
    {
        // 检查攻击冷却
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // 执行攻击
            PerformAttack();
            lastAttackTime = Time.time;
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
        }
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    private void PerformAttack()
    {
        if (currentTarget == null) return;

        // 播放攻击动画
        if (animator != null && !string.IsNullOrEmpty(attackParameterName))
        {
            animator.SetTrigger(attackParameterName);
        }

        // 播放攻击音效
        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, transform.position);
        }

        // 生成攻击特效
        if (attackEffect != null)
        {
            Instantiate(attackEffect, transform.position + transform.forward * 1f, Quaternion.identity);
        }

        // 对目标造成伤害
        ApplyDamageToTarget();
    }

    /// <summary>
    /// 对目标造成伤害（攻击模式）
    /// </summary>
    private void ApplyDamageToTarget()
    {
        if (currentTarget == null) return;

        // 检查目标类型并应用相应伤害
        if (currentTarget.CompareTag("Player"))
        {
            // 攻击玩家灵魂
            PlayerSoulController playerSoul = currentTarget.GetComponent<PlayerSoulController>();
            if (playerSoul != null)
            {
                playerSoul.TakePurifierDamage();
                Debug.Log($"P4净化者攻击玩家灵魂，造成{attackDamage}点伤害");
            }
        }
        else
        {
            // 攻击动物或其他对象
            // 检查是否有IPossessable接口
            IPossessable possessable = currentTarget.GetComponent<IPossessable>();
            if (possessable != null && possessable is MonoBehaviour)
            {
                MonoBehaviour mb = possessable as MonoBehaviour;

                // 检查是否是DeerController
                DeerController deer = mb.GetComponent<DeerController>();
                if (deer != null)
                {
                    float finalDamage = attackDamage * deerDamageMultiplier;
                    deer.TakePurifierDamage(finalDamage);
                    Debug.Log($"P4净化者攻击鹿，造成{finalDamage}点伤害 (基础{attackDamage} × 倍率{deerDamageMultiplier})");
                    return;
                }

                // 检查是否是SheepController
                SheepController sheep = mb.GetComponent<SheepController>();
                if (sheep != null)
                {
                    float finalDamage = attackDamage * sheepDamageMultiplier;
                    sheep.TakePurifierDamage(finalDamage);
                    Debug.Log($"P4净化者攻击绵羊，造成{finalDamage}点伤害 (基础{attackDamage} × 倍率{sheepDamageMultiplier})");
                    return;
                }

                // 检查是否是FoxController
                FoxController fox = mb.GetComponent<FoxController>();
                if (fox != null)
                {
                    float finalDamage = attackDamage * foxDamageMultiplier;
                    fox.TakePurifierDamage(finalDamage);
                    Debug.Log($"P4净化者攻击狐狸，造成{finalDamage}点伤害 (基础{attackDamage} × 倍率{foxDamageMultiplier})");
                    return;
                }

                // 检查是否是BirdController
                BirdController bird = mb.GetComponent<BirdController>();
                if (bird != null)
                {
                    float finalDamage = attackDamage * birdDamageMultiplier;
                    bird.TakePurifierDamage(finalDamage);
                    Debug.Log($"P4净化者攻击鸟，造成{finalDamage}点伤害 (基础{attackDamage} × 倍率{birdDamageMultiplier})");
                    return;
                }

                // 通用伤害接口
                if (mb.gameObject.TryGetComponent<HealthSystem>(out HealthSystem healthSystem))
                {
                    healthSystem.TakeDamage(attackDamage);
                }
            }
        }
    }

    /// <summary>
    /// 同步粒子系统实际状态和标记
    /// </summary>
    private void SyncParticleState()
    {
        if (alertParticleSystem == null) return;

        if (alertParticleSystem.isPlaying != isAlertParticlePlaying)
        {
            isAlertParticlePlaying = alertParticleSystem.isPlaying;
        }
    }

    /// <summary>
    /// 检查玩家是否在视野内
    /// </summary>
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

    /// <summary>
    /// 发现玩家时触发
    /// </summary>
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

        Debug.Log("P4: 发现玩家！");
    }

    /// <summary>
    /// 丢失玩家视野时触发
    /// </summary>
    private void OnPlayerLost()
    {
        agent.speed = normalWalkSpeed;
        currentTarget = null;
        isAttacking = false;

        // 停止粒子特效
        if (alertParticleSystem != null && isAlertParticlePlaying)
        {
            alertParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            isAlertParticlePlaying = false;
        }

        ReturnToPatrol();

        Debug.Log("P4: 丢失玩家视野");
    }

    /// <summary>
    /// 追逐玩家
    /// </summary>
    private void ChasePlayer()
    {
        if (player != null && currentTarget == player.gameObject)
        {
            agent.SetDestination(player.position);
        }

        // 超出逃脱距离则丢失视野
        if (Vector3.Distance(transform.position, player.position) > sightRange * 1.5f)
        {
            isPlayerInSight = false;
        }
    }

    /// <summary>
    /// 巡逻逻辑
    /// </summary>
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
    /// 处理移动状态
    /// </summary>
    private void HandleMovingStateEnhanced()
    {
        if (P4_patrolPoints.Length == 0) return;

        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            if (!SetPatrolTargetEnhanced(currentPatrolIndex))
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % P4_patrolPoints.Length;
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

    /// <summary>
    /// 处理等待状态
    /// </summary>
    private void HandleWaitingState()
    {
        waitCounter -= Time.deltaTime;

        if (waitCounter <= 0f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % P4_patrolPoints.Length;
            currentPatrolState = PatrolState.Moving;
            SetPatrolTargetEnhanced(currentPatrolIndex);
        }
    }

    /// <summary>
    /// 设置巡逻目标
    /// </summary>
    private bool SetPatrolTargetEnhanced(int index)
    {
        if (!IsPatrolValid(index)) return false;

        Vector3 targetPosition = P4_patrolPoints[index].position;

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

    /// <summary>
    /// 检查巡逻点是否有效
    /// </summary>
    private bool IsPatrolValid(int index)
    {
        if (P4_patrolPoints == null || P4_patrolPoints.Length == 0) return false;
        if (index < 0 || index >= P4_patrolPoints.Length) return false;
        if (P4_patrolPoints[index] == null) return false;
        if (agent == null) return false;

        return true;
    }

    /// <summary>
    /// 修复旋转问题
    /// </summary>
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

    /// <summary>
    /// 返回巡逻状态
    /// </summary>
    private void ReturnToPatrol()
    {
        if (currentPatrolState == PatrolState.Moving)
        {
            SetPatrolTargetEnhanced(currentPatrolIndex);
        }
    }

    /// <summary>
    /// 更新动画状态
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool shouldWalk = (currentPatrolState == PatrolState.Moving && agent.remainingDistance > pointArrivalDistance) ||
                         (isPlayerInSight && agent.remainingDistance > pointArrivalDistance);

        animator.SetBool(walkParameterName, shouldWalk);

        // 设置攻击动画状态
        if (!string.IsNullOrEmpty(attackParameterName))
        {
            animator.SetBool(attackParameterName, isAttacking);
        }
    }

    /// <summary>
    /// 碰撞检测 - 进入碰撞
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    /// <summary>
    /// 碰撞检测 - 触发器进入（可选）
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 如果您希望使用触发器而非碰撞体，可以启用此方法
        // HandleCollision(other.gameObject);
    }

    /// <summary>
    /// 处理碰撞逻辑
    /// </summary>
    private void HandleCollision(GameObject collidedObject)
    {
        // 检查冷却时间
        if (Time.time - lastCollisionTime < collisionCooldown)
        {
            return; // 冷却时间内不处理碰撞
        }

        // 检查碰撞对象是否是玩家或玩家附身的对象
        if (IsPlayerOrPossessedObject(collidedObject))
        {
            // 造成碰撞伤害
            ApplyCollisionDamage(collidedObject);

            // 更新最后碰撞时间
            lastCollisionTime = Time.time;

            // 播放碰撞效果
            PlayCollisionEffects();

            Debug.Log($"P4净化者与{collidedObject.name}碰撞，造成伤害");
        }
    }

    /// <summary>
    /// 检查是否是玩家或玩家附身的对象
    /// </summary>
    private bool IsPlayerOrPossessedObject(GameObject obj)
    {
        // 检查是否是玩家灵魂
        if (obj.CompareTag("Player"))
        {
            return true;
        }

        // 检查是否是玩家当前附身的对象
        PlayerSoulController playerSoul = PlayerSoulController.Instance;
        if (playerSoul != null && playerSoul.isPossessing)
        {
            if (playerSoul.currentPossessedObject == obj)
            {
                return true;
            }
        }

        // 检查是否是野生动物（可选，如果您希望净化者也与野生动物碰撞）
        if (obj.GetComponent<DeerController>() != null ||
            obj.GetComponent<SheepController>() != null ||
            obj.GetComponent<FoxController>() != null ||
            obj.GetComponent<BirdController>() != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 应用碰撞伤害（碰撞模式）
    /// </summary>
    private void ApplyCollisionDamage(GameObject target)
    {
        // 尝试获取玩家脚本
        PlayerSoulController playerSoul = PlayerSoulController.Instance;

        // 检查目标是否是玩家灵魂
        if (target.CompareTag("Player") && target == playerSoul?.gameObject)
        {
            // 直接攻击玩家灵魂
            playerSoul.TakePurifierDamage();
            Debug.Log($"净化者碰撞玩家灵魂，造成{collisionDamage}点伤害");
            return;
        }

        // 检查目标是否是动物（玩家附身的对象）
        if (playerSoul != null && playerSoul.isPossessing)
        {
            // 如果玩家附身在动物上，检查是否是当前碰撞的对象
            if (target == playerSoul.currentPossessedObject)
            {
                // 对鹿造成伤害
                DeerController deer = target.GetComponent<DeerController>();
                if (deer != null)
                {
                    float finalDamage = collisionDamage * deerDamageMultiplier;
                    deer.TakePurifierDamage(finalDamage);
                    Debug.Log($"净化者碰撞鹿（玩家附身），造成{finalDamage}点伤害，鹿剩余生命值: {deer.currentHealth}");
                    return;
                }

                // 对绵羊造成伤害
                SheepController sheep = target.GetComponent<SheepController>();
                if (sheep != null)
                {
                    float finalDamage = collisionDamage * sheepDamageMultiplier;
                    sheep.TakePurifierDamage(finalDamage);
                    Debug.Log($"净化者碰撞绵羊（玩家附身），造成{finalDamage}点伤害，绵羊剩余生命值: {sheep.currentHealth}");
                    return;
                }

                // 对狐狸造成伤害
                FoxController fox = target.GetComponent<FoxController>();
                if (fox != null)
                {
                    float finalDamage = collisionDamage * foxDamageMultiplier;
                    fox.TakePurifierDamage(finalDamage);
                    Debug.Log($"净化者碰撞狐狸（玩家附身），造成{finalDamage}点伤害，狐狸剩余生命值: {fox.currentHealth}");
                    return;
                }

                // 对鸟造成伤害
                BirdController bird = target.GetComponent<BirdController>();
                if (bird != null)
                {
                    float finalDamage = collisionDamage * birdDamageMultiplier;
                    bird.TakePurifierDamage(finalDamage);
                    Debug.Log($"净化者碰撞鸟（玩家附身），造成{finalDamage}点伤害，鸟剩余生命值: {bird.currentHealth}");
                    return;
                }
            }
        }

        // 其他情况（如动物没有被附身）
        // 检查野生鹿
        DeerController standaloneDeer = target.GetComponent<DeerController>();
        if (standaloneDeer != null)
        {
            float finalDamage = collisionDamage * deerDamageMultiplier;
            standaloneDeer.TakePurifierDamage(finalDamage);
            Debug.Log($"净化者碰撞野生鹿，造成{finalDamage}点伤害");
            return;
        }

        // 检查野生绵羊
        SheepController standaloneSheep = target.GetComponent<SheepController>();
        if (standaloneSheep != null)
        {
            float finalDamage = collisionDamage * sheepDamageMultiplier;
            standaloneSheep.TakePurifierDamage(finalDamage);
            Debug.Log($"净化者碰撞野生绵羊，造成{finalDamage}点伤害");
            return;
        }

        // 检查野生狐狸
        FoxController standaloneFox = target.GetComponent<FoxController>();
        if (standaloneFox != null)
        {
            float finalDamage = collisionDamage * foxDamageMultiplier;
            standaloneFox.TakePurifierDamage(finalDamage);
            Debug.Log($"净化者碰撞野生狐狸，造成{finalDamage}点伤害");
            return;
        }

        // 检查野生鸟
        BirdController standaloneBird = target.GetComponent<BirdController>();
        if (standaloneBird != null)
        {
            float finalDamage = collisionDamage * birdDamageMultiplier;
            standaloneBird.TakePurifierDamage(finalDamage);
            Debug.Log($"净化者碰撞野生鸟，造成{finalDamage}点伤害");
            return;
        }

        // 通用伤害接口
        if (target.TryGetComponent<HealthSystem>(out HealthSystem healthSystem))
        {
            healthSystem.TakeDamage(collisionDamage);
        }
    }

    /// <summary>
    /// 播放碰撞效果
    /// </summary>
    private void PlayCollisionEffects()
    {
        // 播放碰撞音效
        if (collisionSound != null)
        {
            AudioSource.PlayClipAtPoint(collisionSound, transform.position, 0.5f);
        }

        // 生成碰撞特效
        if (collisionEffect != null)
        {
            GameObject effect = Instantiate(collisionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // 2秒后销毁特效
        }

        // 颜色闪烁效果
        if (purifierRenderer != null)
        {
            StartCoroutine(FlashCollisionColor());
        }
    }

    /// <summary>
    /// 颜色闪烁协程
    /// </summary>
    private IEnumerator FlashCollisionColor()
    {
        if (purifierRenderer != null)
        {
            // 保存原始材质
            Material originalMat = purifierRenderer.material;

            // 创建临时材质并设置颜色
            Material flashMaterial = new Material(originalMat);
            flashMaterial.color = collisionColor;
            purifierRenderer.material = flashMaterial;

            // 等待一段时间
            yield return new WaitForSeconds(collisionFlashDuration);

            // 恢复原始材质
            purifierRenderer.material = originalMat;
        }
    }

    /// <summary>
    /// 绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        // 绘制视野范围
        DrawSightGizmos();

        // 绘制攻击范围
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 绘制碰撞体范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * colliderHeight / 2, colliderRadius);

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
        if (P4_patrolPoints != null)
        {
            // 当前目标点
            if (currentPatrolIndex < P4_patrolPoints.Length && P4_patrolPoints[currentPatrolIndex] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(P4_patrolPoints[currentPatrolIndex].position, 0.5f);
            }

            // 所有巡逻点
            for (int i = 0; i < P4_patrolPoints.Length; i++)
            {
                if (P4_patrolPoints[i] != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(P4_patrolPoints[i].position, 0.2f);
                }
            }
        }

        // 绘制到玩家的连线（如果在视野内）
        if (isPlayerInSight && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
        }
    }

    /// <summary>
    /// 绘制视野锥体
    /// </summary>
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

    /// <summary>
    /// 攻击动画事件回调（可在动画关键帧调用）
    /// </summary>
    public void OnAttackAnimationEvent()
    {
        // 这个方法可以在攻击动画的关键帧中调用，确保伤害在正确的时间应用
        ApplyDamageToTarget();
    }

    /// <summary>
    /// 重置净化者状态（可用于重新开始游戏时）
    /// </summary>
    public void ResetPurifier()
    {
        if (isInitialized)
        {
            // 重置巡逻
            currentPatrolIndex = 0;
            currentPatrolState = PatrolState.Moving;
            SetPatrolTargetEnhanced(currentPatrolIndex);

            // 重置玩家检测状态
            isPlayerInSight = false;
            currentTarget = null;
            isAttacking = false;

            // 停止警报粒子
            if (alertParticleSystem != null && isAlertParticlePlaying)
            {
                alertParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                isAlertParticlePlaying = false;
            }

            // 重置移动速度
            agent.speed = normalWalkSpeed;
            agent.isStopped = false;

            // 重置碰撞冷却
            lastCollisionTime = 0f;
            lastAttackTime = 0f;

            Debug.Log("P4净化者已重置");
        }
    }

    /// <summary>
    /// 强制重新巡逻（调试用）
    /// </summary>
    [ContextMenu("强制重新巡逻")]
    private void ForceRestartPatrol()
    {
        currentPatrolIndex = 0;
        currentPatrolState = PatrolState.Moving;
        SetPatrolTargetEnhanced(currentPatrolIndex);
        Debug.Log("P4: 强制重新巡逻");
    }

    /// <summary>
    /// 手动停止粒子特效（调试用）
    /// </summary>
    [ContextMenu("手动停止粒子特效")]
    private void ManualStopParticle()
    {
        if (alertParticleSystem != null)
        {
            alertParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            isAlertParticlePlaying = false;
            Debug.Log("P4: 手动停止粒子特效");
        }
    }

    /// <summary>
    /// 手动播放粒子特效（调试用）
    /// </summary>
    [ContextMenu("手动播放粒子特效")]
    private void ManualPlayParticle()
    {
        if (alertParticleSystem != null)
        {
            alertParticleSystem.Play(true);
            isAlertParticlePlaying = true;
            Debug.Log("P4: 手动播放粒子特效");
        }
    }

    /// <summary>
    /// 模拟与玩家碰撞（调试用）
    /// </summary>
    [ContextMenu("模拟与玩家碰撞")]
    private void SimulatePlayerCollision()
    {
        if (player != null)
        {
            HandleCollision(player.gameObject);
        }
        else
        {
            Debug.LogWarning("P4: 无法找到玩家进行模拟碰撞");
        }
    }

    /// <summary>
    /// 设置对不同动物的伤害倍率
    /// </summary>
    public void SetAnimalDamageMultiplier(string animalType, float multiplier)
    {
        switch (animalType.ToLower())
        {
            case "deer":
                deerDamageMultiplier = multiplier;
                break;
            case "sheep":
                sheepDamageMultiplier = multiplier;
                break;
            case "fox":
                foxDamageMultiplier = multiplier;
                break;
            case "bird":
                birdDamageMultiplier = multiplier;
                break;
            default:
                Debug.LogWarning($"未知的动物类型: {animalType}");
                break;
        }
    }

    /// <summary>
    /// 获取对特定动物的伤害值
    /// </summary>
    public float GetDamageForAnimal(GameObject animal)
    {
        if (animal == null) return attackDamage;

        // 根据动物类型返回对应的伤害值
        if (animal.GetComponent<DeerController>() != null)
        {
            return attackDamage * deerDamageMultiplier;
        }
        else if (animal.GetComponent<SheepController>() != null)
        {
            return attackDamage * sheepDamageMultiplier;
        }
        else if (animal.GetComponent<FoxController>() != null)
        {
            return attackDamage * foxDamageMultiplier;
        }
        else if (animal.GetComponent<BirdController>() != null)
        {
            return attackDamage * birdDamageMultiplier;
        }

        return attackDamage;
    }
}