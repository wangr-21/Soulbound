using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("相机目标")]
    public Transform target; // 要跟随的目标（灵魂或被附身对象）

    [Header("第一人称设置")]
    public Vector3 firstPersonOffset = new Vector3(0, 1.5f, 0.5f); // 降低Y值到1.5，增加向前偏移到0.5
    public float firstPersonFOV = 75f; // 第一人称视野角度
    private float originalFOV; // 原始视野角度

    [Header("第三人称设置")]
    public float thirdPersonDistance = 20f; // 第三人称相机距离进一步增加到20f
    public float thirdPersonHeight = 8f;   // 第三人称相机高度增加到8f
    public float mouseSensitivity = 2f;    // 鼠标灵敏度

    [Header("第一人称视角限制")]
    public float firstPersonMinY = -70f;   // 最低低头角度
    public float firstPersonMaxY = 70f;    // 最高抬头角度
    public float firstPersonHeadBobAmount = 0.05f; // 头部晃动幅度
    public float firstPersonHeadBobSpeed = 4f;     // 头部晃动速度

    [Header("其他设置")]
    public float smoothSpeed = 12f;        // 平滑速度
    public float cameraCollisionRadius = 1f; // 相机碰撞检测半径
    public LayerMask cameraCollisionMask = -1; // 相机碰撞层

    private bool isFirstPerson = false;    // 当前视角模式
    private float mouseX = 0f;             // 鼠标X轴累积旋转
    private float mouseY = 0f;             // 鼠标Y轴累积旋转
    private bool isMouseLocked = true;     // 鼠标是否锁定
    private Vector3 cameraVelocity = Vector3.zero; // 用于SmoothDamp的当前速度
    private Camera cam; // 相机组件
    private float headBobTimer = 0f;       // 头部晃动计时器
    private Vector3 originalFirstPersonOffset; // 原始第一人称偏移

    void Start()
    {
        // 获取相机组件
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            originalFOV = cam.fieldOfView;
        }

        // 如果没有指定目标，尝试找到玩家
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        // 初始化旋转角度
        if (target != null)
        {
            mouseX = target.eulerAngles.y;
            mouseY = 0f;
        }

        // 保存原始第一人称偏移
        originalFirstPersonOffset = firstPersonOffset;

        // 锁定并隐藏鼠标
        LockMouse();

        // 根据角色大小自动调整相机
        AdjustCameraForRadius(4f);
    }

    void Update()
    {
        // 按V键切换视角
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleViewMode();
        }

        // 按ESC键切换鼠标锁定状态
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMouseLock();
        }

        // 始终处理鼠标输入
        HandleMouseInput();
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (isFirstPerson)
        {
            UpdateFirstPersonCamera();
        }
        else
        {
            UpdateThirdPersonCamera();
        }
    }

    // 切换视角模式
    void ToggleViewMode()
    {
        Debug.Log("V键被按下，开始切换视角");
        isFirstPerson = !isFirstPerson;

        // 切换时调整视野
        if (cam != null)
        {
            cam.fieldOfView = isFirstPerson ? firstPersonFOV : originalFOV;
        }

        Debug.Log($"切换到 {(isFirstPerson ? "第一人称" : "第三人称")} 视角");
    }

    // 切换鼠标锁定状态
    void ToggleMouseLock()
    {
        if (isMouseLocked)
        {
            UnlockMouse();
        }
        else
        {
            LockMouse();
        }
    }

    // 锁定鼠标
    void LockMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isMouseLocked = true;
    }

    // 解锁鼠标
    void UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isMouseLocked = false;
    }

    // 处理鼠标输入
    void HandleMouseInput()
    {
        if (!isMouseLocked) return;

        // 获取鼠标移动
        float mouseDeltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseDeltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 累积旋转角度
        mouseX += mouseDeltaX;
        mouseY -= mouseDeltaY;

        // 限制垂直旋转角度
        if (isFirstPerson)
        {
            // 第一人称视角有更严格的垂直角度限制
            mouseY = Mathf.Clamp(mouseY, firstPersonMinY, firstPersonMaxY);
        }
        else
        {
            // 第三人称视角限制不变
            mouseY = Mathf.Clamp(mouseY, -90f, 90f);
        }
    }

    // 更新第一人称相机 - 降低视角高度
    void UpdateFirstPersonCamera()
    {
        // 计算头部晃动效果
        Vector3 headBobOffset = CalculateHeadBob();

        // 计算相机位置
        // 第一人称相机应该在目标的位置上加上偏移
        Vector3 desiredPosition = target.position;

        // 添加基础的偏移
        desiredPosition += Vector3.up * firstPersonOffset.y;      // 垂直偏移（高度）

        // 添加水平偏移（基于目标的旋转）
        Vector3 horizontalOffset = target.right * firstPersonOffset.x + target.forward * firstPersonOffset.z;
        desiredPosition += horizontalOffset;

        // 添加头部晃动
        desiredPosition += headBobOffset;

        // 使用SmoothDamp平滑移动相机
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref cameraVelocity, 0.05f, Mathf.Infinity, Time.deltaTime);

        // 设置相机旋转
        // 第一人称视角直接跟随鼠标输入
        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);
        transform.rotation = rotation;

        // 更新目标的水平旋转（只影响角色的水平朝向）
        Vector3 targetEulerAngles = target.eulerAngles;
        targetEulerAngles.y = mouseX; // 只更新Y轴旋转
        target.rotation = Quaternion.Euler(targetEulerAngles);
    }

    // 计算头部晃动效果
    Vector3 CalculateHeadBob()
    {
        if (target == null) return Vector3.zero;

        // 获取目标的移动速度
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        CharacterController targetCC = target.GetComponent<CharacterController>();

        float speed = 0f;
        if (targetRb != null)
        {
            speed = targetRb.velocity.magnitude;
        }
        else if (targetCC != null)
        {
            speed = targetCC.velocity.magnitude;
        }

        // 只有在移动时才添加头部晃动
        if (speed > 0.1f)
        {
            headBobTimer += Time.deltaTime * firstPersonHeadBobSpeed * (speed * 0.5f);

            // 计算晃动偏移
            float verticalBob = Mathf.Sin(headBobTimer * 2f) * firstPersonHeadBobAmount * 0.5f;
            float horizontalBob = Mathf.Sin(headBobTimer) * firstPersonHeadBobAmount * 0.3f;

            // 基于目标的方向计算偏移
            Vector3 bobOffset = new Vector3(
                horizontalBob,
                verticalBob,
                0
            );

            // 将偏移转换到目标的局部空间
            bobOffset = target.TransformDirection(bobOffset);

            return bobOffset;
        }
        else
        {
            // 停止移动时，平滑归零头部晃动
            headBobTimer = 0f;
            return Vector3.Lerp(headBobOffset, Vector3.zero, Time.deltaTime * 5f);
        }
    }

    // 头部晃动的当前偏移（用于平滑归零）
    private Vector3 headBobOffset = Vector3.zero;

    // 更新第三人称相机 - 进一步增加距离
    void UpdateThirdPersonCamera()
    {
        // 设置目标物体的旋转（只影响水平旋转，Y轴）
        Quaternion targetRotation = Quaternion.Euler(0, mouseX, 0);

        // 平滑旋转目标
        if (target.rotation != targetRotation)
        {
            target.rotation = Quaternion.Slerp(target.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }

        // 计算相机应该的位置（在目标后方更远的距离）
        Vector3 direction = new Vector3(0, 0, -thirdPersonDistance);
        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);

        // 计算理想的相机位置
        Vector3 idealPosition = target.position + rotation * direction + Vector3.up * thirdPersonHeight;

        // 计算相机目标看向的点（在角色中心上方）
        Vector3 lookAtPoint = target.position + Vector3.up * (thirdPersonHeight * 0.3f);

        // 检测相机和目标之间是否有障碍物
        Vector3 rayDirection = idealPosition - lookAtPoint;
        float rayDistance = rayDirection.magnitude;

        // 使用球体投射来避免相机卡进障碍物
        RaycastHit hit;
        if (Physics.SphereCast(lookAtPoint,
                              cameraCollisionRadius,
                              rayDirection.normalized,
                              out hit,
                              rayDistance,
                              cameraCollisionMask))
        {
            // 如果有障碍物，将相机移动到障碍物前面一点
            idealPosition = hit.point + hit.normal * cameraCollisionRadius;
        }

        // 平滑移动相机
        transform.position = Vector3.SmoothDamp(transform.position, idealPosition, ref cameraVelocity, 0.12f, Mathf.Infinity, Time.deltaTime);

        // 让相机看向目标点
        transform.LookAt(lookAtPoint);
    }

    // 公共方法：设置新的跟随目标（用于附身时切换目标）
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // 重置一些相机参数
        if (target != null)
        {
            mouseX = target.eulerAngles.y;
            mouseY = 0f;
            cameraVelocity = Vector3.zero; // 重置速度以避免抖动
            headBobTimer = 0f;
            headBobOffset = Vector3.zero;
        }
    }

    // 公共方法：获取当前视角模式
    public bool IsFirstPerson()
    {
        return isFirstPerson;
    }

    // 公共方法：获取当前的水平旋转角度（用于移动方向计算）
    public float GetCurrentYRotation()
    {
        return mouseX;
    }

    // 根据角色半径调整相机参数
    public void AdjustCameraForRadius(float characterRadius)
    {
        // 根据角色半径调整相机参数
        thirdPersonDistance = Mathf.Max(characterRadius * 3f, 15f);
        thirdPersonHeight = Mathf.Max(characterRadius * 1.5f, 6f);

        // 第一人称相机应该更像人眼高度
        // 根据角色大小调整第一人称高度，但保持在人眼高度范围内
        firstPersonOffset.y = Mathf.Clamp(characterRadius * 0.8f, 1.2f, 2.0f);
        firstPersonOffset.z = Mathf.Clamp(characterRadius * 0.1f, 0.1f, 0.3f);

        cameraCollisionRadius = characterRadius * 0.5f;

        Debug.Log($"相机已调整：第三人称距离={thirdPersonDistance}, 高度={thirdPersonHeight}");
        Debug.Log($"第一人称：高度={firstPersonOffset.y}, 前向偏移={firstPersonOffset.z}");
    }

    // 调整第一人称视角高度（可以在运行时调用）
    public void AdjustFirstPersonHeight(float newHeight)
    {
        firstPersonOffset.y = newHeight;
        Debug.Log($"第一人称高度调整为: {newHeight}");
    }

    // 调整第一人称前向偏移（控制相机在角色前方还是中心）
    public void AdjustFirstPersonForwardOffset(float forwardOffset)
    {
        firstPersonOffset.z = forwardOffset;
        Debug.Log($"第一人称前向偏移调整为: {forwardOffset}");
    }

    // 重置第一人称偏移到原始值
    public void ResetFirstPersonOffset()
    {
        firstPersonOffset = originalFirstPersonOffset;
        Debug.Log("第一人称偏移已重置");
    }

    // 可选的调试方法：在Scene视图中显示相机位置
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, 4f); // 显示角色半径

        if (isFirstPerson)
        {
            // 显示第一人称相机位置
            Vector3 desiredPosition = target.position +
                                      Vector3.up * firstPersonOffset.y +
                                      target.forward * firstPersonOffset.z;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(desiredPosition, 0.2f);
            Gizmos.DrawLine(target.position, desiredPosition);

            // 显示视野方向
            Gizmos.color = Color.red;
            Gizmos.DrawRay(desiredPosition, transform.forward * 5f);
        }
        else
        {
            Vector3 direction = new Vector3(0, 0, -thirdPersonDistance);
            Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);
            Vector3 desiredPosition = target.position + rotation * direction + Vector3.up * thirdPersonHeight;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(desiredPosition, 0.5f);
            Gizmos.DrawLine(target.position + Vector3.up * thirdPersonHeight * 0.3f, desiredPosition);
        }
    }
}