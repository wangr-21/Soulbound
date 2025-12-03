using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("相机目标")]
    public Transform target; // 要跟随的目标（灵魂或被附身对象）

    [Header("第一人称设置")]
    public Vector3 firstPersonOffset = new Vector3(0, 8f, 12f); // 第一人称相机偏移（增加Y和Z值）
    public float firstPersonFOV = 75f; // 第一人称视野角度
    private float originalFOV; // 原始视野角度

    [Header("第三人称设置")]
    public float thirdPersonDistance = 20f; // 第三人称相机距离进一步增加到20f
    public float thirdPersonHeight = 8f;   // 第三人称相机高度增加到8f
    public float mouseSensitivity = 2f;    // 鼠标灵敏度

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
        Debug.Log("V键被按下，开始切换视角"); // 添加这行
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
        mouseY = Mathf.Clamp(mouseY, -90f, 90f);
    }

    // 更新第一人称相机 - 提高视角并向后移动
    void UpdateFirstPersonCamera()
    {
        // 设置目标物体的旋转（只影响水平旋转，Y轴）
        Quaternion targetRotation = Quaternion.Euler(0, mouseX, 0);

        // 平滑旋转目标
        if (target.rotation != targetRotation)
        {
            target.rotation = Quaternion.Slerp(target.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }

        // 计算相机位置
        // 第一人称相机应该像从角色"眼睛"位置看向外面，所以需要向上和向后偏移
        Vector3 desiredPosition = target.position +
                                  target.TransformDirection(new Vector3(firstPersonOffset.x, 0, 0)) +
                                  Vector3.up * firstPersonOffset.y +
                                  target.forward * -Mathf.Abs(firstPersonOffset.z); // 向后移动

        // 使用SmoothDamp平滑移动相机
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref cameraVelocity, 0.08f, Mathf.Infinity, Time.deltaTime);

        // 设置相机旋转
        // 第一人称应该看向角色前方
        Vector3 lookDirection = target.forward;
        lookDirection = Quaternion.Euler(mouseY, 0, 0) * lookDirection;
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }

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

        // 第一人称相机应该在角色上方并向后，可以看到角色的大部分
        firstPersonOffset.y = Mathf.Max(characterRadius * 1.5f, 6f);
        firstPersonOffset.z = -Mathf.Max(characterRadius * 2f, 8f); // 负值表示向后

        cameraCollisionRadius = characterRadius * 0.5f;

        Debug.Log($"相机已调整：第三人称距离={thirdPersonDistance}, 高度={thirdPersonHeight}");
        Debug.Log($"第一人称：高度={firstPersonOffset.y}, 距离={Mathf.Abs(firstPersonOffset.z)}");
    }

    // 可选的调试方法：在Scene视图中显示相机位置
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, 4f); // 显示角色半径

        if (isFirstPerson)
        {
            Vector3 desiredPosition = target.position +
                                      Vector3.up * firstPersonOffset.y +
                                      target.forward * firstPersonOffset.z;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(desiredPosition, 0.5f);
            Gizmos.DrawLine(target.position, desiredPosition);
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