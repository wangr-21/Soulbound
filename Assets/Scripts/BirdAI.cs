using UnityEngine;

public class BirdAI : MonoBehaviour
{
    [Header("AI 设置")]
    public float maxHeight = 15f;           // 最大飞行高度
    public float minHeight = 2f;            // 最小飞行高度
    public float preferredHeight = 8f;      // 首选飞行高度
    public float flyRadius = 25f;           // 飞行范围半径

    [Header("状态时间设置")]
    public float minFlyTime = 5f;           // 最短飞行时间
    public float maxFlyTime = 15f;          // 最长飞行时间
    public float minGlideTime = 2f;         // 最短滑翔时间
    public float maxGlideTime = 6f;         // 最长滑翔时间
    public float minIdleTime = 3f;          // 最短地面停留时间
    public float maxIdleTime = 10f;         // 最长地面停留时间
    public float landChance = 0.2f;         // 每次考虑着陆的概率
    public float glideChance = 0.3f;        // 开始滑翔的概率

    [Header("盘旋设置")]
    public float orbitRadius = 5f;          // 盘旋半径
    public float orbitSpeed = 0.5f;         // 盘旋速度
    public float heightVariation = 2f;      // 高度变化范围

    [Header("组件引用")]
    private BirdController birdController;
    private CharacterController characterController;

    [Header("AI 状态")]
    public BirdAIState currentState = BirdAIState.Flying;
    private float stateTimer = 0f;
    private float currentStateDuration = 0f;
    private Vector3 homePosition;           // "巢穴"位置（初始位置）
    private Vector3 currentTarget;          // 当前目标位置
    private Vector3 orbitCenter;            // 盘旋中心点
    private float orbitAngle = 0f;          // 盘旋角度
    private float targetHeight;             // 目标高度
    private bool isCircling = false;        // 是否在盘旋
    private Vector3 randomDirection;        // 随机飞行方向

    [Header("调试")]
    public bool showDebugInfo = true;
    public bool showGizmos = true;
    public Color flyRadiusColor = Color.blue;
    public Color orbitRadiusColor = Color.yellow;

    public enum BirdAIState
    {
        Idle,       // 地面待机状态
        Flying,     // 飞行状态
        Gliding,    // 滑翔状态
        Landing,    // 着陆状态
        TakingOff   // 起飞状态
    }

    void Start()
    {
        // 获取组件
        birdController = GetComponent<BirdController>();
        characterController = GetComponent<CharacterController>();

        if (birdController == null)
        {
            Debug.LogError("BirdAI: 找不到BirdController组件！");
        }

        if (characterController == null)
        {
            Debug.LogError("BirdAI: 找不到CharacterController组件！");
        }

        // 记录"巢穴"位置（初始位置）
        homePosition = transform.position;

        // 设置初始状态为飞行
        EnterFlyingState();

        if (showDebugInfo) Debug.Log($"鸟AI初始化完成，初始位置: {homePosition}");
    }

    void Update()
    {
        // 如果鸟被玩家附身，禁用AI
        if (birdController != null && birdController.isPossessed)
        {
            // 确保AI停止控制
            if (currentState != BirdAIState.Idle)
            {
                EnterIdleState();
            }
            return;
        }

        // 更新状态计时器
        stateTimer += Time.deltaTime;

        // 根据当前状态执行相应的行为
        switch (currentState)
        {
            case BirdAIState.Idle:
                UpdateIdleState();
                break;
            case BirdAIState.Flying:
                UpdateFlyingState();
                break;
            case BirdAIState.Gliding:
                UpdateGlidingState();
                break;
            case BirdAIState.Landing:
                UpdateLandingState();
                break;
            case BirdAIState.TakingOff:
                UpdateTakingOffState();
                break;
        }
    }

    #region 状态管理

    void EnterIdleState()
    {
        currentState = BirdAIState.Idle;
        stateTimer = 0f;
        currentStateDuration = Random.Range(minIdleTime, maxIdleTime);
        isCircling = false;

        // 停止所有移动，在地面待机
        if (birdController != null)
        {
            birdController.SetAIInput(Vector2.zero, false, false, false);
        }

        if (showDebugInfo) Debug.Log($"进入地面待机状态，持续时间: {currentStateDuration:F1}秒");
    }

    void UpdateIdleState()
    {
        // 检查是否应该起飞
        if (stateTimer >= currentStateDuration)
        {
            EnterTakingOffState();
        }

        // 在地面时保持静止
        if (birdController != null)
        {
            birdController.SetAIInput(Vector2.zero, false, false, false);
        }
    }

    void EnterFlyingState()
    {
        currentState = BirdAIState.Flying;
        stateTimer = 0f;
        currentStateDuration = Random.Range(minFlyTime, maxFlyTime);

        // 设置目标高度
        targetHeight = Random.Range(preferredHeight - 2f, preferredHeight + 2f);
        targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);

        // 决定是否盘旋
        isCircling = Random.value > 0.5f;

        if (isCircling)
        {
            // 设置盘旋中心点
            Vector2 randomCircle = Random.insideUnitCircle * flyRadius * 0.5f;
            orbitCenter = homePosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            orbitAngle = Random.Range(0f, 360f);

            if (showDebugInfo) Debug.Log($"进入盘旋飞行状态，中心点: {orbitCenter}, 高度: {targetHeight:F1}m");
        }
        else
        {
            // 随机飞行方向
            randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            currentTarget = homePosition + randomDirection * Random.Range(flyRadius * 0.3f, flyRadius);

            if (showDebugInfo) Debug.Log($"进入随机飞行状态，目标点: {currentTarget}, 高度: {targetHeight:F1}m");
        }
    }

    void UpdateFlyingState()
    {
        // 检查是否应该滑翔
        if (Random.value < glideChance * Time.deltaTime && !isCircling && transform.position.y > minHeight + 2f)
        {
            EnterGlidingState();
            return;
        }

        // 检查是否应该考虑着陆
        if (Random.value < landChance * Time.deltaTime && stateTimer > 2f && transform.position.y < preferredHeight)
        {
            // 检查当前位置是否适合着陆
            if (CheckLandingSpot())
            {
                EnterLandingState();
                return;
            }
        }

        // 飞行控制
        if (isCircling)
        {
            // 盘旋飞行
            UpdateCircling();
        }
        else
        {
            // 随机飞行
            UpdateRandomFlight();
        }

        // 检查是否时间到了
        if (stateTimer >= currentStateDuration)
        {
            // 决定下一个状态
            if (Random.value < 0.7f)
            {
                EnterFlyingState(); // 继续飞行
            }
            else
            {
                EnterGlidingState(); // 切换到滑翔
            }
        }
    }

    void UpdateCircling()
    {
        // 更新盘旋角度
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        // 计算盘旋位置
        float radian = orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radian) * orbitRadius, 0, Mathf.Sin(radian) * orbitRadius);

        // 添加高度变化
        float heightOffset = Mathf.Sin(Time.time * 0.5f) * heightVariation;
        Vector3 targetPosition = orbitCenter + offset;
        targetPosition.y = targetHeight + heightOffset;

        // 计算移动方向
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0; // 水平移动

        // 调整高度
        float heightDifference = targetPosition.y - transform.position.y;
        bool ascending = heightDifference > 0.5f;
        bool descending = heightDifference < -0.5f;

        // 设置AI输入
        if (birdController != null)
        {
            Vector2 moveInput = new Vector2(direction.normalized.x, direction.normalized.z);
            birdController.SetAIInput(moveInput, ascending, descending, false);
        }

        // 旋转朝向移动方向
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
        }
    }

    void UpdateRandomFlight()
    {
        // 计算到目标的水平距离
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(currentTarget.x, currentPos.y, currentTarget.z);
        float distanceToTarget = Vector3.Distance(currentPos, targetPos);

        // 如果接近目标，选择新目标
        if (distanceToTarget < 3f)
        {
            randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            currentTarget = transform.position + randomDirection * Random.Range(5f, 15f);

            // 确保在飞行范围内
            Vector3 toHome = currentTarget - homePosition;
            toHome.y = 0;
            if (toHome.magnitude > flyRadius)
            {
                toHome = toHome.normalized * flyRadius;
                currentTarget = homePosition + toHome;
            }
        }

        // 计算移动方向
        Vector3 direction = (currentTarget - transform.position);
        direction.y = 0;

        // 调整高度到目标高度
        float heightDifference = targetHeight - transform.position.y;
        bool ascending = heightDifference > 1f;
        bool descending = heightDifference < -1f;

        // 设置AI输入
        if (birdController != null)
        {
            Vector2 moveInput = new Vector2(direction.normalized.x, direction.normalized.z);
            birdController.SetAIInput(moveInput, ascending, descending, false);
        }

        // 旋转朝向移动方向
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
        }
    }

    void EnterGlidingState()
    {
        currentState = BirdAIState.Gliding;
        stateTimer = 0f;
        currentStateDuration = Random.Range(minGlideTime, maxGlideTime);

        // 设置滑翔方向（轻微向下）
        randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        currentTarget = transform.position + randomDirection * 20f;

        if (showDebugInfo) Debug.Log($"进入滑翔状态，持续时间: {currentStateDuration:F1}秒");
    }

    void UpdateGlidingState()
    {
        // 计算移动方向（水平方向）
        Vector3 horizontalDirection = (currentTarget - transform.position);
        horizontalDirection.y = 0;

        // 滑翔时下降，不主动上升
        if (birdController != null)
        {
            Vector2 moveInput = new Vector2(horizontalDirection.normalized.x, horizontalDirection.normalized.z);
            birdController.SetAIInput(moveInput, false, false, true); // 滑翔状态
        }

        // 检查是否应该结束滑翔
        if (stateTimer >= currentStateDuration || transform.position.y < minHeight + 1f)
        {
            if (transform.position.y < minHeight + 2f && CheckLandingSpot())
            {
                EnterLandingState();
            }
            else
            {
                EnterFlyingState();
            }
        }
    }

    void EnterLandingState()
    {
        currentState = BirdAIState.Landing;
        stateTimer = 0f;
        currentStateDuration = 5f; // 着陆最大时间限制

        // 寻找着陆点（当前位置下方）
        RaycastHit hit;
        Vector3 landingSpot = transform.position;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, 50f, birdController.groundLayer))
        {
            landingSpot = hit.point;
            landingSpot.y += characterController.height * 0.5f; // 确保站立在地面
        }
        else
        {
            // 如果找不到地面，使用homePosition
            landingSpot = homePosition;
        }

        currentTarget = landingSpot;

        if (showDebugInfo) Debug.Log($"进入着陆状态，目标着陆点: {currentTarget}");
    }

    void UpdateLandingState()
    {
        // 计算到着陆点的水平方向
        Vector3 horizontalDirection = (currentTarget - transform.position);
        horizontalDirection.y = 0;

        float distanceToTarget = horizontalDirection.magnitude;
        float verticalDistance = transform.position.y - currentTarget.y;

        // 控制下降
        bool shouldDescend = verticalDistance > 0.5f;

        // 设置AI输入
        if (birdController != null)
        {
            Vector2 moveInput = new Vector2(horizontalDirection.normalized.x, horizontalDirection.normalized.z);
            birdController.SetAIInput(moveInput, false, shouldDescend, false);
        }

        // 检查是否着陆成功
        if (verticalDistance < 0.5f && distanceToTarget < 1f)
        {
            // 成功着陆
            EnterIdleState();
        }
        else if (stateTimer >= currentStateDuration)
        {
            // 着陆超时，重新飞行
            EnterFlyingState();
        }
    }

    void EnterTakingOffState()
    {
        currentState = BirdAIState.TakingOff;
        stateTimer = 0f;
        currentStateDuration = 3f; // 起飞时间限制

        // 设置起飞目标高度
        targetHeight = Random.Range(minHeight + 2f, preferredHeight);

        // 选择起飞方向
        randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        currentTarget = transform.position + randomDirection * 10f;

        if (showDebugInfo) Debug.Log($"进入起飞状态，目标高度: {targetHeight:F1}m");
    }

    void UpdateTakingOffState()
    {
        // 计算方向
        Vector3 direction = (currentTarget - transform.position);
        Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z);

        // 主要向上飞
        bool shouldAscend = transform.position.y < targetHeight;

        // 设置AI输入
        if (birdController != null)
        {
            Vector2 moveInput = new Vector2(horizontalDirection.normalized.x, horizontalDirection.normalized.z);
            birdController.SetAIInput(moveInput, shouldAscend, false, false);
        }

        // 检查是否达到目标高度
        if (transform.position.y >= targetHeight - 1f || stateTimer >= currentStateDuration)
        {
            EnterFlyingState();
        }
    }

    #endregion

    #region 辅助方法

    // 检查当前位置是否适合着陆
    bool CheckLandingSpot()
    {
        // 检查下方是否有地面
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 20f, birdController.groundLayer))
        {
            // 检查地面是否相对平坦
            float groundAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (groundAngle < 30f) // 允许的最大坡度
            {
                // 检查着陆点是否在飞行范围内
                Vector3 landingSpot = hit.point;
                float distanceFromHome = Vector3.Distance(new Vector3(landingSpot.x, 0, landingSpot.z),
                                                         new Vector3(homePosition.x, 0, homePosition.z));

                if (distanceFromHome < flyRadius * 0.8f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region 调试

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // 绘制飞行范围
        Gizmos.color = flyRadiusColor;
        Gizmos.DrawWireSphere(homePosition, flyRadius);

        // 绘制当前目标点
        if (Application.isPlaying)
        {
            if (currentState == BirdAIState.Flying || currentState == BirdAIState.Gliding ||
                currentState == BirdAIState.Landing || currentState == BirdAIState.TakingOff)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentTarget, 0.5f);
                Gizmos.DrawLine(transform.position, currentTarget);
            }

            // 绘制盘旋范围
            if (currentState == BirdAIState.Flying && isCircling)
            {
                Gizmos.color = orbitRadiusColor;
                Gizmos.DrawWireSphere(orbitCenter, orbitRadius);

                // 绘制盘旋路径
                for (int i = 0; i < 36; i++)
                {
                    float angle1 = i * 10f * Mathf.Deg2Rad;
                    float angle2 = (i + 1) * 10f * Mathf.Deg2Rad;

                    Vector3 point1 = orbitCenter + new Vector3(Mathf.Cos(angle1) * orbitRadius, targetHeight, Mathf.Sin(angle1) * orbitRadius);
                    Vector3 point2 = orbitCenter + new Vector3(Mathf.Cos(angle2) * orbitRadius, targetHeight, Mathf.Sin(angle2) * orbitRadius);

                    Gizmos.DrawLine(point1, point2);
                }
            }

            // 状态文本
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"状态: {currentState}\n" +
                $"高度: {transform.position.y:F1}m\n" +
                $"目标高度: {targetHeight:F1}m");
#endif
        }
    }

    #endregion
}