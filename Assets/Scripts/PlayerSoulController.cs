using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSoulController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float possessionRange = 3f;

    [Header("跳跃设置")]
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    // 移动和跳跃相关变量
    private Vector3 playerVelocity;
    private bool isGrounded;
    private CharacterController characterController;
    private PlayerInputActions playerInputActions;

    // 输入相关变量
    private Vector2 currentMovementInput;
    private bool jumpTriggered = false;

    // 附身相关变量
    private GameObject currentPossessedObject;
    private bool isPossessing = false;
    private IPossessable currentPossessable;

    // 视角切换
    private CameraController cameraController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInputActions = new PlayerInputActions();
    }

    private void Start()
    {
        // 获取相机控制器
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
            Debug.Log("相机控制器找到并设置目标");
        }
        else
        {
            Debug.LogError("未找到相机控制器！请确保主相机上有 CameraController 组件");
        }
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        playerInputActions.Player.Move.performed += OnMove;
        playerInputActions.Player.Move.canceled += OnMove;
        playerInputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Move.performed -= OnMove;
        playerInputActions.Player.Move.canceled -= OnMove;
        playerInputActions.Player.Jump.performed -= OnJump;
        playerInputActions.Player.Disable();
    }

    // 移动输入回调
    private void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
    }

    // 跳跃输入回调
    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpTriggered = true;
        }
    }

    void Update()
    {
        // 附身/脱离输入检测 - 移到最前面，确保任何状态下都能检测
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isPossessing)
            {
                AttemptPossession();
            }
            else
            {
                ReleasePossession();
            }
        }

        // 如果正在附身，控制权交给被附身对象
        if (isPossessing && currentPossessable != null)
        {
            currentPossessable.PossessedUpdate();
        }
        else
        {
            // 如果没有附身，控制灵魂移动和跳跃
            HandleMovementAndJump();
        }
    }

    private void HandleMovementAndJump()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.5f;
        }

        // 获取相机的当前水平旋转角度
        float cameraYRotation = 0f;
        if (cameraController != null)
        {
            cameraYRotation = cameraController.GetCurrentYRotation();
        }

        // 将输入方向转换为相对于相机视角的方向
        Vector3 moveDirection = new Vector3(currentMovementInput.x, 0, currentMovementInput.y);

        // 创建基于相机Y轴旋转的旋转四元数
        Quaternion cameraRotation = Quaternion.Euler(0, cameraYRotation, 0);

        // 将移动方向转换为世界空间，相对于相机视角
        moveDirection = cameraRotation * moveDirection;

        // 应用移动
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        if (jumpTriggered && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpTriggered = false;
        }

        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    void AttemptPossession()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, possessionRange);
        GameObject closestObject = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            IPossessable possessable = hitCollider.GetComponent<IPossessable>();
            if (possessable != null)
            {
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = hitCollider.gameObject;
                }
            }
        }

        if (closestObject != null)
        {
            currentPossessedObject = closestObject;
            currentPossessable = currentPossessedObject.GetComponent<IPossessable>();

            if (currentPossessable != null)
            {
                currentPossessable.OnPossess();
                isPossessing = true;

                // 隐藏灵魂
                GetComponent<Renderer>().enabled = false;
                GetComponent<Collider>().enabled = false;
                characterController.enabled = false;

                // 切换相机目标到被附身的对象
                if (cameraController != null)
                {
                    cameraController.SetTarget(currentPossessedObject.transform);
                    Debug.Log("相机目标切换到: " + currentPossessedObject.name);
                }
            }
        }
    }

    void ReleasePossession()
    {
        if (currentPossessable != null)
        {
            currentPossessable.OnRelease();

            // 显示灵魂并移动到被附身对象的位置
            GetComponent<Renderer>().enabled = true;
            GetComponent<Collider>().enabled = true;
            transform.position = currentPossessedObject.transform.position;
            characterController.enabled = true;

            // 切换相机目标回灵魂
            if (cameraController != null)
            {
                cameraController.SetTarget(transform);
                Debug.Log("相机目标切换回灵魂");
            }

            currentPossessedObject = null;
            currentPossessable = null;
            isPossessing = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, possessionRange);
    }

    // 在 PlayerSoulController 类中添加这个方法
    public void ForceReleasePossession()
    {
        if (isPossessing)
        {
            ReleasePossession();
        }
    }
}