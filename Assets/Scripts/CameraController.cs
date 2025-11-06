using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("相机目标")]
    public Transform target; // 要跟随的目标（灵魂或被附身对象）

    [Header("第一人称设置")]
    public Vector3 firstPersonOffset = new Vector3(0, 0.5f, 0); // 第一人称相机偏移

    [Header("第三人称设置")]
    public float thirdPersonDistance = 5f; // 第三人称相机距离
    public float thirdPersonHeight = 2f;   // 第三人称相机高度
    public float mouseSensitivity = 2f;    // 鼠标灵敏度

    [Header("其他设置")]
    public float smoothSpeed = 8f;         // 相机移动平滑度

    private bool isFirstPerson = false;    // 当前视角模式
    private float mouseX = 0f;             // 鼠标X轴累积旋转
    private float mouseY = 0f;             // 鼠标Y轴累积旋转
    private bool isMouseLocked = true;     // 鼠标是否锁定

    void Start()
    {
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
        isFirstPerson = !isFirstPerson;
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

    // 更新第一人称相机
    void UpdateFirstPersonCamera()
    {
        // 设置目标物体的旋转（只影响水平旋转，Y轴）
        target.rotation = Quaternion.Euler(0, mouseX, 0);

        // 计算相机位置（在目标位置加上偏移）
        Vector3 desiredPosition = target.position + target.TransformDirection(firstPersonOffset);

        // 平滑移动相机
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 设置相机旋转（包含水平和垂直旋转）
        transform.rotation = Quaternion.Euler(mouseY, mouseX, 0);
    }

    // 更新第三人称相机
    void UpdateThirdPersonCamera()
    {
        // 设置目标物体的旋转（只影响水平旋转，Y轴）
        target.rotation = Quaternion.Euler(0, mouseX, 0);

        // 计算相机应该的位置（在目标后方一定距离）
        Vector3 direction = new Vector3(0, 0, -thirdPersonDistance);
        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);
        Vector3 desiredPosition = target.position + rotation * direction + Vector3.up * thirdPersonHeight;

        // 检测相机和目标之间是否有障碍物
        RaycastHit hit;
        Vector3 rayDirection = desiredPosition - target.position;
        if (Physics.Raycast(target.position, rayDirection.normalized, out hit, rayDirection.magnitude))
        {
            // 如果有障碍物，将相机移动到障碍物前面一点
            desiredPosition = hit.point - rayDirection.normalized * 0.3f;
        }

        // 平滑移动相机
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 让相机看向目标
        transform.LookAt(target.position + Vector3.up * thirdPersonHeight * 0.5f);
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

}