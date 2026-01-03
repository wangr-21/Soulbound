using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class P1_Purifier : MonoBehaviour
{
    [Header("=== P1专属设置 ===")]
    public Transform[] P1_patrolPoints;
    [SerializeField] private float waitTimeAtPoint = 1.5f;
    [SerializeField] private float pointArrivalDistance = 0.5f;

    [Header("=== AI Components ===")]
    private NavMeshAgent agent;
    private PlayerSoulController playerSoul; // 改为直接引用玩家组件

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
    [SerializeField] private float attackKnockbackForce = 5f; // 攻击击退力

    [Header("=== 碰撞伤害设置 ===")]
    [SerializeField] private bool enableCollisionDamage = true; // 是否启用碰撞伤害
    [SerializeField] private float collisionDamage = 20f; // 碰撞伤害值
    [SerializeField] private float collisionDamageCooldown = 1f; // 碰撞伤害冷却时间
    private float lastCollisionDamageTime = 0f; // 上次碰撞伤害时间

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

    // 添加刚体和碰撞器组件引用
    private Rigidbody rb;
    private Collider purifierCollider;

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
        rb = GetComponent<Rigidbody>();
        purifierCollider = GetComponent<Collider>();

        if (agent == null)
        {
            Debug.LogError("P1: 找不到NavMeshAgent组件！");
            return;
        }

        // 设置刚体属性
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // 设置碰撞器属性
        if (purifierCollider != null)
        {
            purifierCollider.isTrigger = false; // 确保不是触发器，以检测碰撞
        }

        // 初始化NavMeshAgent
        agent.speed = normalWalkSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = pointArrivalDistance;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // 查找玩家 - 使用组件而不是标签
        FindPlayerByComponent();

        // 初始化粒子系统
        InitializeParticleSystem();

        // 检查动画参数
        FindCorrectAnimationParameter();

        // 开始巡逻
        InitializePatrol();

        isInitialized = true;
    }

    private void FindPlayerByComponent()
    {
        // 方法1: 通过单例获取
        playerSoul = PlayerSoulController.Instance;

        if (playerSoul != null)
        {
            Debug.Log($"P1: 通过单例找到玩家灵魂: {playerSoul.gameObject.name}");
            return;
        }

        // 方法2: 查找场景中所有PlayerSoulController组件
        PlayerSoulController[] allPlayers = FindObjectsOfType<PlayerSoulController>();
        if (allPlayers.Length > 0)
        {
            playerSoul = allPlayers[0];
            Debug.Log($"P1: 通过FindObjectsOfType找到玩家灵魂: {playerSoul.gameObject.name}");
            return;
        }

        // 方法3: 通过名称查找
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerSoul = playerObj.GetComponent<PlayerSoulController>();
            if (playerSoul != null)
            {
                Debug.Log($"P1: 通过名称找到玩家灵魂: {playerObj.name}");
                return;
            }
        }

        Debug.LogError("P1: 未找到玩家灵魂组件！");
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

        // 寻找行走参数
        string[] possibleWalkNames = { "Walk", "isWalking", "Walking", "Move", "IsMoving" };
        foreach (string name in possibleWalkNames)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == name && param.type == AnimatorControllerParameterType.Bool)
                {
                    walkParameterName = name;
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
                    break;
                }
            }
        }
    }

    private void InitializePatrol()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("P1: 代理不在NavMesh上！");
            return;
        }

        if (P1_patrolPoints == null || P1_patrolPoints.Length == 0)
        {
            Debug.LogError("P1: 巡逻点数组为空！");
            return;
        }

        // 验证巡逻点
        for (int i = 0; i < P1_patrolPoints.Length; i++)
        {
            if (P1_patrolPoints[i] == null)
            {
                Debug.LogError($"P1: 巡逻点 {i} 为null！");
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
        // 重置当前目标
        currentTarget = null;

        // 检查玩家是否附身在动物上
        if (playerSoul != null && playerSoul.isPossessing && playerSoul.currentPossessedObject != null)
        {
            // 如果玩家附身在动物上，则攻击动物
            currentTarget = playerSoul.currentPossessedObject;
            Debug.Log($"净化者: 目标更新为玩家附身的动物: {currentTarget.name}");
        }
        else if (playerSoul != null && playerSoul.gameObject != null)
        {
            // 否则攻击玩家灵魂
            currentTarget = playerSoul.gameObject;
            Debug.Log($"净化者: 目标更新为玩家灵魂: {currentTarget.name}");
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

        // 击退效果
        ApplyKnockback();
    }

    /// <summary>
    /// 对目标造成伤害
    /// </summary>
    private void ApplyDamageToTarget()
    {
        if (currentTarget == null) return;

        Debug.Log($"净化者: 尝试对 {currentTarget.name} 造成攻击伤害");

        // 检查目标是否是玩家灵魂
        PlayerSoulController targetPlayer = currentTarget.GetComponent<PlayerSoulController>();
        if (targetPlayer != null)
        {
            targetPlayer.TakePurifierDamage();
            Debug.Log($"净化者攻击玩家灵魂，造成{attackDamage}点伤害");
            return;
        }

        // 攻击动物或其他对象
        // 检查是否有IPossessable接口
        IPossessable possessable = currentTarget.GetComponent<IPossessable>();
        if (possessable != null && possessable is MonoBehaviour)
        {
            MonoBehaviour mb = possessable as MonoBehaviour;

            // 检查是否是DeerController（或其他动物控制器）
            DeerController deer = mb.GetComponent<DeerController>();
            if (deer != null)
            {
                deer.TakePurifierDamage(attackDamage);
                Debug.Log($"净化者攻击鹿，造成{attackDamage}点伤害");
                return;
            }

            // 检查其他动物控制器
            FoxController fox = mb.GetComponent<FoxController>();
            if (fox != null)
            {
                fox.TakePurifierDamage(attackDamage);
                Debug.Log($"净化者攻击狐狸，造成{attackDamage}点伤害");
                return;
            }

            SheepController sheep = mb.GetComponent<SheepController>();
            if (sheep != null)
            {
                sheep.TakePurifierDamage(attackDamage);
                Debug.Log($"净化者攻击绵羊，造成{attackDamage}点伤害");
                return;
            }

            BirdController bird = mb.GetComponent<BirdController>();
            if (bird != null)
            {
                bird.TakePurifierDamage(attackDamage);
                Debug.Log($"净化者攻击鸟，造成{attackDamage}点伤害");
                return;
            }

            // 通用伤害接口
            if (mb.gameObject.TryGetComponent<HealthSystem>(out HealthSystem healthSystem))
            {
                healthSystem.TakeDamage(attackDamage);
            }
        }
    }

    /// <summary>
    /// 应用击退效果
    /// </summary>
    private void ApplyKnockback()
    {
        if (currentTarget == null || attackKnockbackForce <= 0) return;

        // 计算击退方向
        Vector3 knockbackDirection = currentTarget.transform.position - transform.position;
        knockbackDirection.y = 0; // 保持水平方向
        knockbackDirection.Normalize();

        // 应用击退力
        Rigidbody targetRigidbody = currentTarget.GetComponent<Rigidbody>();
        if (targetRigidbody != null)
        {
            targetRigidbody.AddForce(knockbackDirection * attackKnockbackForce, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// 碰撞检测 - 当净化者与玩家碰撞时造成伤害（不使用标签）
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!enableCollisionDamage) return;

        GameObject collidedObject = collision.gameObject;
        Debug.Log($"净化者碰撞到: {collidedObject.name}");

        // 检查是否是玩家灵魂
        PlayerSoulController player = collidedObject.GetComponent<PlayerSoulController>();
        if (player != null)
        {
            Debug.Log($"净化者: 检测到玩家灵魂碰撞，应用碰撞伤害");
            ApplyCollisionDamageToPlayer(player.gameObject);
            return;
        }

        // 检查是否是玩家附身的动物
        if (playerSoul != null && playerSoul.isPossessing && playerSoul.currentPossessedObject != null)
        {
            if (collidedObject == playerSoul.currentPossessedObject)
            {
                Debug.Log($"净化者: 检测到玩家附身动物碰撞，应用碰撞伤害");
                ApplyCollisionDamageToAnimal(collidedObject);
                return;
            }
        }

        // 检查是否是动物（即使玩家没有附身）
        IPossessable possessable = collidedObject.GetComponent<IPossessable>();
        if (possessable != null)
        {
            Debug.Log($"净化者: 检测到动物碰撞，应用碰撞伤害");
            ApplyCollisionDamageToAnimal(collidedObject);
            return;
        }
    }

    /// <summary>
    /// 触发器检测
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!enableCollisionDamage) return;

        GameObject collidedObject = other.gameObject;
        Debug.Log($"净化者触发器进入: {collidedObject.name}");

        // 检查是否是玩家灵魂
        PlayerSoulController player = collidedObject.GetComponent<PlayerSoulController>();
        if (player != null)
        {
            Debug.Log($"净化者: 触发器检测到玩家灵魂，应用碰撞伤害");
            ApplyCollisionDamageToPlayer(player.gameObject);
            return;
        }

        // 检查是否是玩家附身的动物
        if (playerSoul != null && playerSoul.isPossessing && playerSoul.currentPossessedObject != null)
        {
            if (collidedObject == playerSoul.currentPossessedObject)
            {
                Debug.Log($"净化者: 触发器检测到玩家附身动物，应用碰撞伤害");
                ApplyCollisionDamageToAnimal(collidedObject);
                return;
            }
        }

        // 检查是否是动物
        IPossessable possessable = collidedObject.GetComponent<IPossessable>();
        if (possessable != null)
        {
            Debug.Log($"净化者: 触发器检测到动物，应用碰撞伤害");
            ApplyCollisionDamageToAnimal(collidedObject);
            return;
        }
    }

    /// <summary>
    /// 对玩家应用碰撞伤害
    /// </summary>
    private void ApplyCollisionDamageToPlayer(GameObject playerObject)
    {
        // 检查冷却时间
        if (Time.time - lastCollisionDamageTime < collisionDamageCooldown)
        {
            Debug.Log($"碰撞伤害冷却中: {collisionDamageCooldown - (Time.time - lastCollisionDamageTime):F2}秒");
            return;
        }

        // 更新上次碰撞伤害时间
        lastCollisionDamageTime = Time.time;

        // 获取玩家组件
        PlayerSoulController player = playerObject.GetComponent<PlayerSoulController>();
        if (player != null)
        {
            // 播放碰撞特效
            if (attackEffect != null)
            {
                Instantiate(attackEffect, transform.position, Quaternion.identity);
            }

            // 重要修改：调用玩家的碰撞伤害处理方法
            player.ApplyCollisionDamageFromPurifier(this.gameObject);
            Debug.Log($"净化者碰撞玩家灵魂，调用玩家的碰撞伤害处理方法");
        }
    }

    /// <summary>
    /// 对动物应用碰撞伤害
    /// </summary>
    private void ApplyCollisionDamageToAnimal(GameObject animalObject)
    {
        // 检查冷却时间
        if (Time.time - lastCollisionDamageTime < collisionDamageCooldown)
        {
            return;
        }

        // 更新上次碰撞伤害时间
        lastCollisionDamageTime = Time.time;

        // 播放碰撞特效
        if (attackEffect != null)
        {
            Instantiate(attackEffect, transform.position, Quaternion.identity);
        }

        // 检查动物类型并应用伤害
        IPossessable possessable = animalObject.GetComponent<IPossessable>();
        if (possessable != null && possessable is MonoBehaviour)
        {
            MonoBehaviour mb = possessable as MonoBehaviour;

            // 检查动物类型并应用伤害
            DeerController deer = mb.GetComponent<DeerController>();
            if (deer != null)
            {
                deer.TakePurifierDamage(collisionDamage);
                Debug.Log($"净化者碰撞鹿，造成{collisionDamage}点伤害");
                return;
            }

            FoxController fox = mb.GetComponent<FoxController>();
            if (fox != null)
            {
                fox.TakePurifierDamage(collisionDamage);
                Debug.Log($"净化者碰撞狐狸，造成{collisionDamage}点伤害");
                return;
            }

            SheepController sheep = mb.GetComponent<SheepController>();
            if (sheep != null)
            {
                sheep.TakePurifierDamage(collisionDamage);
                Debug.Log($"净化者碰撞绵羊，造成{collisionDamage}点伤害");
                return;
            }

            BirdController bird = mb.GetComponent<BirdController>();
            if (bird != null)
            {
                bird.TakePurifierDamage(collisionDamage);
                Debug.Log($"净化者碰撞鸟，造成{collisionDamage}点伤害");
                return;
            }

            // 通用伤害接口
            if (mb.gameObject.TryGetComponent<HealthSystem>(out HealthSystem healthSystem))
            {
                healthSystem.TakeDamage(collisionDamage);
            }
        }
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

        if (playerSoul == null || playerSoul.gameObject == null)
        {
            // 尝试重新查找玩家
            FindPlayerByComponent();
            return;
        }

        // 获取当前目标位置
        Vector3 targetPosition = GetCurrentTargetPosition();
        if (targetPosition == Vector3.zero) return;

        // 距离检查
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > sightRange) return;

        // 角度检查
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        if (angleToTarget > sightAngle) return;

        // 障碍物检查
        Vector3 rayStart = transform.position + Vector3.up * 1.5f;
        Vector3 rayTarget = targetPosition + Vector3.up * 1f;
        Vector3 rayDirection = (rayTarget - rayStart).normalized;

        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, sightRange))
        {
            if (drawRaycastGizmos)
            {
                // 检查是否看到了目标
                bool isTarget = IsHitTarget(hit.collider.gameObject);
                Debug.DrawLine(rayStart, hit.point, isTarget ? Color.green : Color.red);
            }

            if (IsHitTarget(hit.collider.gameObject))
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
    /// 获取当前目标的坐标
    /// </summary>
    private Vector3 GetCurrentTargetPosition()
    {
        if (playerSoul == null) return Vector3.zero;

        if (playerSoul.isPossessing && playerSoul.currentPossessedObject != null)
        {
            return playerSoul.currentPossessedObject.transform.position;
        }
        else if (playerSoul.gameObject != null)
        {
            return playerSoul.transform.position;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 检查击中的对象是否是当前目标
    /// </summary>
    private bool IsHitTarget(GameObject hitObject)
    {
        if (playerSoul == null) return false;

        // 检查是否是玩家灵魂
        if (hitObject == playerSoul.gameObject) return true;

        // 检查是否是玩家附身的动物
        if (playerSoul.isPossessing && playerSoul.currentPossessedObject != null)
        {
            if (hitObject == playerSoul.currentPossessedObject) return true;
        }

        return false;
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
        currentTarget = null;
        isAttacking = false;

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
        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.transform.position);
        }

        // 超出逃脱距离则丢失视野
        if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) > sightRange * 1.5f)
        {
            isPlayerInSight = false;
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
        if (P1_patrolPoints.Length == 0) return;

        if (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            if (!SetPatrolTargetEnhanced(currentPatrolIndex))
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % P1_patrolPoints.Length;
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
            currentPatrolIndex = (currentPatrolIndex + 1) % P1_patrolPoints.Length;
            currentPatrolState = PatrolState.Moving;
            SetPatrolTargetEnhanced(currentPatrolIndex);
        }
    }

    private bool SetPatrolTargetEnhanced(int index)
    {
        if (!IsPatrolValid(index)) return false;

        Vector3 targetPosition = P1_patrolPoints[index].position;

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
        if (P1_patrolPoints == null || P1_patrolPoints.Length == 0) return false;
        if (index < 0 || index >= P1_patrolPoints.Length) return false;
        if (P1_patrolPoints[index] == null) return false;
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

        // 设置攻击动画状态
        if (!string.IsNullOrEmpty(attackParameterName))
        {
            animator.SetBool(attackParameterName, isAttacking);
        }
    }

    private void OnDrawGizmos()
    {
        // 绘制视野范围
        DrawSightGizmos();

        // 绘制攻击范围
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 绘制碰撞范围
        if (enableCollisionDamage)
        {
            Gizmos.color = Color.red;
            if (purifierCollider != null)
            {
                Gizmos.DrawWireSphere(transform.position, purifierCollider.bounds.extents.magnitude);
            }
        }

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
        if (P1_patrolPoints != null)
        {
            // 当前目标点
            if (currentPatrolIndex < P1_patrolPoints.Length && P1_patrolPoints[currentPatrolIndex] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(P1_patrolPoints[currentPatrolIndex].position, 0.5f);
            }

            // 所有巡逻点
            for (int i = 0; i < P1_patrolPoints.Length; i++)
            {
                if (P1_patrolPoints[i] != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(P1_patrolPoints[i].position, 0.2f);
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

    /// <summary>
    /// 攻击动画事件回调（可在动画关键帧调用）
    /// </summary>
    public void OnAttackAnimationEvent()
    {
        // 这个方法可以在攻击动画的关键帧中调用，确保伤害在正确的时间应用
        ApplyDamageToTarget();
    }

    /// <summary>
    /// 启用或禁用碰撞伤害
    /// </summary>
    public void SetCollisionDamageEnabled(bool enabled)
    {
        enableCollisionDamage = enabled;
        Debug.Log($"净化者碰撞伤害已{(enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 设置碰撞伤害值
    /// </summary>
    public void SetCollisionDamage(float damage)
    {
        collisionDamage = damage;
        Debug.Log($"净化者碰撞伤害已设置为{damage}点");
    }
}