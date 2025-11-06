using UnityEngine;

public class DeerController : PossessableBase
{
    [Header("鹿的设置")]
    public float moveSpeed = 10f;
    public float jumpForce = 7.0f;
    public float gravity = -20f;

    // 移动和跳跃相关变量
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool jumpTriggered = false;

    // 相机相关
    private CameraController cameraController;

    private void Start()
    {
        objectName = "鹿";
        abilityDescription = "快速移动";

        // 获取或添加 CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            // 调整 CharacterController 大小适合鹿
            characterController.height = 1.5f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0, 0.75f, 0);
        }

        // 获取相机控制器
        if (Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }
    }

    // 实现被附身时的更新
    public override void PossessedUpdate()
    {
        HandleInput();
        HandleMovementAndJump();
    }

    // 处理输入
    private void HandleInput()
    {
        // 只处理跳跃输入，E键由PlayerSoulController统一处理
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpTriggered = true;
        }
    }

    // 处理移动和跳跃
    private void HandleMovementAndJump()
    {
        if (characterController == null) return;

        // 检测是否着地
        isGrounded = characterController.isGrounded;

        // 如果着地并且垂直速度小于0，重置垂直速度
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // 获取移动输入
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 moveDirection = new Vector3(moveX, 0, moveZ);

        // 基于相机视角转换移动方向
        if (cameraController != null)
        {
            float cameraYRotation = cameraController.GetCurrentYRotation();
            Quaternion cameraRotation = Quaternion.Euler(0, cameraYRotation, 0);
            moveDirection = cameraRotation * moveDirection;
        }

        // 应用移动
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // 处理跳跃
        if (jumpTriggered && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpTriggered = false;
        }

        // 应用重力
        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    // 重写附身方法，添加视觉反馈
    public override void OnPossess()
    {
        base.OnPossess();

        // 启用 CharacterController（如果之前被禁用）
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        Debug.Log("已附身到鹿！使用 WASD 移动，空格键跳跃，E键脱离");
    }

    // 重写脱离方法
    public override void OnRelease()
    {
        base.OnRelease();
        Debug.Log("从鹿脱离");
    }
}