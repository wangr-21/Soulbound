using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MiddlePurifier : MonoBehaviour
{
    [Header("=== 中级净化者设置 ===")]
    [SerializeField] private float waitTimeAtPoint = 1.5f;
    [SerializeField] private float pointArrivalDistance = 0.5f;
    [SerializeField] private float patrolRadius = 10f;  // 随机巡逻范围

    [Header("=== AI Components ===")]
    private NavMeshAgent agent;
    private Transform player;

    [Header("=== Vision Settings ===")]
    [SerializeField] private float visionRadius = 12f;
    [SerializeField] private float horizontalVisionAngle = 140f;
    [SerializeField] private float verticalVisionAngle = 60f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float chaseEscapeDistance = 2f;

    [Header("=== Head Movement ===")]
    [SerializeField] private Transform visionHead;
    [SerializeField] private float headRotationSpeed = 2f;
    [SerializeField] private float maxHeadRotationAngle = 45f;

    [Header("=== Summon Settings ===")]
    [SerializeField] private float summonRadius = 4f;
    [SerializeField] private int maxSummonCount = 3;
    [SerializeField] private LayerMask purifierMask;
    [SerializeField] private float summonCooldown = 15f;

    [Header("=== Alert Reactions ===")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.5f, 0.1f);
    [SerializeField] private Color alertColor = new Color(1f, 0.2f, 0.1f);
    [SerializeField] private Color summonColor = new Color(1f, 0.8f, 0.1f);
    [SerializeField] private float rotateSpeed = 5f;

    [Header("=== Chase Settings ===")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float normalSpeed = 2f;
    [SerializeField] private float markDuration = 8f;

    private Renderer purifierRenderer;
    private float waitCounter = 0f;
    private float summonCooldownCounter = 0f;
    private float markCounter = 0f;
    private float currentHeadRotation = 0f;
    private bool isLookingUp = true;
    private bool isPlayerInSight = false;
    private bool wasPlayerInSight = false;
    private bool hasSummoned = false;
    private bool isChasing = false;
    private bool isPlayerMarked = false;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 currentPatrolTarget;

    private enum AIState { Patrolling, Chasing, Marking, Coordinating, Waiting }
    private AIState currentState = AIState.Patrolling;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        purifierRenderer = GetComponent<Renderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = normalColor;
        }

        agent.speed = normalSpeed;

        // 生成第一个随机巡逻点
        GenerateRandomPatrolPoint();

        if (visionHead == null) visionHead = transform;
    }

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        UpdateCooldowns();
        wasPlayerInSight = isPlayerInSight;
        UpdateHeadMovement();
        CheckPlayerInSight();

        switch (currentState)
        {
            case AIState.Patrolling:
                PatrolBehavior();
                break;
            case AIState.Chasing:
                ChaseBehavior();
                break;
            case AIState.Marking:
                MarkBehavior();
                break;
            case AIState.Coordinating:
                CoordinateBehavior();
                break;
            case AIState.Waiting:
                WaitBehavior();
                break;
        }

        HandleStateTransitions();
    }

    /// <summary>
    /// 生成随机巡逻点
    /// </summary>
    private void GenerateRandomPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection.y = 0; // 保持水平移动

        currentPatrolTarget = transform.position + randomDirection;

        // 确保目标点在导航网格上
        NavMeshHit hit;
        if (NavMesh.SamplePosition(currentPatrolTarget, out hit, patrolRadius, NavMesh.AllAreas))
        {
            currentPatrolTarget = hit.position;
            agent.SetDestination(currentPatrolTarget);
            Debug.Log($"中级净化者: 生成新的随机巡逻点 {currentPatrolTarget}");
        }
        else
        {
            // 如果找不到有效点，使用当前位置
            currentPatrolTarget = transform.position;
        }
    }

    /// <summary>
    /// 在玩家周围生成搜索点（标记状态时用）
    /// </summary>
    private void GenerateSearchPointAroundPlayer()
    {
        if (player == null) return;

        Vector3 randomDirection = Random.insideUnitSphere * 3f; // 在玩家周围3米内搜索
        randomDirection.y = 0;

        currentPatrolTarget = lastKnownPlayerPosition + randomDirection;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(currentPatrolTarget, out hit, 3f, NavMesh.AllAreas))
        {
            currentPatrolTarget = hit.position;
            agent.SetDestination(currentPatrolTarget);
        }
    }

    private void UpdateHeadMovement()
    {
        if (currentState == AIState.Patrolling || currentState == AIState.Waiting)
        {
            float rotationDirection = isLookingUp ? 1f : -1f;
            currentHeadRotation += rotationDirection * headRotationSpeed * Time.deltaTime;

            if (Mathf.Abs(currentHeadRotation) >= maxHeadRotationAngle)
            {
                isLookingUp = !isLookingUp;
            }

            visionHead.localRotation = Quaternion.Euler(currentHeadRotation, 0, 0);
        }
        else if (currentState == AIState.Chasing && player != null)
        {
            Vector3 directionToPlayer = player.position - visionHead.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            visionHead.rotation = Quaternion.Slerp(visionHead.rotation, targetRotation, headRotationSpeed * Time.deltaTime);
        }
        else
        {
            visionHead.localRotation = Quaternion.Slerp(visionHead.localRotation, Quaternion.identity, headRotationSpeed * Time.deltaTime);
        }
    }

    private void CheckPlayerInSight()
    {
        if (player == null)
        {
            isPlayerInSight = false;
            return;
        }

        Vector3 toPlayer = player.position - visionHead.position;
        float distance = toPlayer.magnitude;

        if (distance > visionRadius)
        {
            isPlayerInSight = false;
            return;
        }

        Vector3 horizontalDirection = new Vector3(toPlayer.x, 0, toPlayer.z).normalized;
        Vector3 headForward = new Vector3(visionHead.forward.x, 0, visionHead.forward.z).normalized;
        float horizontalAngle = Vector3.Angle(headForward, horizontalDirection);

        if (horizontalAngle > horizontalVisionAngle / 2)
        {
            isPlayerInSight = false;
            return;
        }

        Vector3 verticalDirection = toPlayer.normalized;
        float verticalAngle = Vector3.Angle(visionHead.forward, verticalDirection);

        if (verticalAngle > verticalVisionAngle / 2)
        {
            isPlayerInSight = false;
            return;
        }

        Vector3 rayStart = visionHead.position;
        Vector3 playerCenter = player.position + Vector3.up * 0.5f;

        Debug.DrawRay(rayStart, (playerCenter - rayStart).normalized * distance,
                     isPlayerInSight ? Color.red : Color.yellow, 0.1f);

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
        if (!wasPlayerInSight)
        {
            Debug.Log($"中级净化者: 检测到玩家！距离: {distance:F1}米");
        }
    }

    private void UpdateCooldowns()
    {
        if (summonCooldownCounter > 0) summonCooldownCounter -= Time.deltaTime;
        if (markCounter > 0) markCounter -= Time.deltaTime;
    }

    private void HandleStateTransitions()
    {
        if (isPlayerInSight && !wasPlayerInSight)
        {
            EnterChaseState();
        }
        else if (!isPlayerInSight && wasPlayerInSight && currentState == AIState.Chasing)
        {
            float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
            if (distanceToPlayer > chaseEscapeDistance)
            {
                EnterMarkingState();
            }
        }
    }

    private void EnterChaseState()
    {
        currentState = AIState.Chasing;
        isChasing = true;
        agent.speed = chaseSpeed;
        agent.isStopped = false;

        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = alertColor;
        }

        if (!hasSummoned && summonCooldownCounter <= 0)
        {
            SummonNearbyPurifiers();
        }

        Debug.Log("中级净化者: 发现玩家，开始追踪并召集同伴！");
    }

    private void SummonNearbyPurifiers()
    {
        Collider[] nearbyPurifiers = Physics.OverlapSphere(transform.position, summonRadius, purifierMask);
        int summonedCount = 0;

        foreach (Collider collider in nearbyPurifiers)
        {
            if (collider.gameObject != gameObject && summonedCount < maxSummonCount)
            {
                MiddlePurifier otherPurifier = collider.GetComponent<MiddlePurifier>();
                if (otherPurifier != null)
                {
                    otherPurifier.RespondToSummon(player != null ? player.position : transform.position);
                    Debug.Log($"中级净化者: 召集 {collider.gameObject.name} 协助追踪");
                    summonedCount++;
                }
            }
        }

        hasSummoned = true;
        summonCooldownCounter = summonCooldown;
        StartCoroutine(SummonEffect());
    }

    private IEnumerator SummonEffect()
    {
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = summonColor;
            yield return new WaitForSeconds(0.5f);
            if (currentState == AIState.Chasing)
            {
                purifierRenderer.material.color = alertColor;
            }
        }
    }

    private void ChaseBehavior()
    {
        if (player == null) return;
        agent.SetDestination(player.position);
        lastKnownPlayerPosition = player.position;
    }

    private void EnterMarkingState()
    {
        currentState = AIState.Marking;
        markCounter = markDuration;
        isPlayerMarked = true;

        // 在玩家最后位置周围生成搜索点
        GenerateSearchPointAroundPlayer();

        Debug.Log($"中级净化者: 玩家消失，在最后位置周围搜索，持续 {markDuration}秒");
    }

    private void MarkBehavior()
    {
        // 如果到达了搜索点，生成新的搜索点
        if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
        {
            GenerateSearchPointAroundPlayer();
        }

        if (purifierRenderer != null)
        {
            float flash = Mathf.PingPong(Time.time * 3f, 1f);
            purifierRenderer.material.color = Color.Lerp(alertColor, summonColor, flash);
        }

        if (markCounter <= 0)
        {
            ReturnToPatrol();
        }
    }

    private void CoordinateBehavior()
    {
        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = summonColor;
        }
    }

    private void ReturnToPatrol()
    {
        currentState = AIState.Patrolling;
        isChasing = false;
        hasSummoned = false;
        isPlayerMarked = false;
        agent.speed = normalSpeed;

        if (purifierRenderer != null)
        {
            purifierRenderer.material.color = normalColor;
        }

        // 生成新的随机巡逻点
        GenerateRandomPatrolPoint();
        Debug.Log("中级净化者: 恢复随机巡逻");
    }

    private void PatrolBehavior()
    {
        switch (currentState)
        {
            case AIState.Patrolling:
                HandlePatrolMoving();
                break;
            case AIState.Waiting:
                HandlePatrolWaiting();
                break;
        }
    }

    private void HandlePatrolMoving()
    {
        if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
        {
            currentState = AIState.Waiting;
            waitCounter = waitTimeAtPoint;
            Debug.Log("中级净化者: 到达随机点，等待中...");
        }
    }

    private void HandlePatrolWaiting()
    {
        waitCounter -= Time.deltaTime;
        if (waitCounter <= 0f)
        {
            currentState = AIState.Patrolling;
            GenerateRandomPatrolPoint();
        }
    }

    private void WaitBehavior()
    {
        // 等待时缓慢旋转扫描环境
        transform.Rotate(0, 30f * Time.deltaTime, 0);
    }

    // 被其他净化者召唤的响应
    public void RespondToSummon(Vector3 targetPosition)
    {
        if (currentState != AIState.Chasing && currentState != AIState.Marking)
        {
            currentState = AIState.Chasing;
            agent.SetDestination(targetPosition);
            agent.speed = chaseSpeed;

            if (purifierRenderer != null)
            {
                purifierRenderer.material.color = alertColor;
            }
            Debug.Log("中级净化者: 响应同伴召集！");
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawVisionCone();

        // 巡逻范围
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        // 召集范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, summonRadius);

        // 当前巡逻目标
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(currentPatrolTarget, 0.3f);
        Gizmos.DrawLine(transform.position, currentPatrolTarget);

        if (isPlayerMarked)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 1f);
        }
    }

    private void DrawVisionCone()
    {
        Vector3 headPos = visionHead != null ? visionHead.position : transform.position;
        Vector3 forward = visionHead != null ? visionHead.forward : transform.forward;

        Gizmos.color = new Color(0, 1, 0, 0.2f);

        int segments = 12;
        float horizontalStep = horizontalVisionAngle / segments;
        float verticalStep = verticalVisionAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            for (int j = 0; j <= segments; j++)
            {
                float hAngle = -horizontalVisionAngle / 2 + horizontalStep * i;
                float vAngle = -verticalVisionAngle / 2 + verticalStep * j;

                Quaternion rotation = Quaternion.Euler(vAngle, hAngle, 0);
                Vector3 direction = rotation * forward;
                Gizmos.DrawRay(headPos, direction * visionRadius);
            }
        }
    }

    [ContextMenu("测试随机移动")]
    private void TestRandomMovement()
    {
        GenerateRandomPatrolPoint();
    }
}