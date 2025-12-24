using UnityEngine;
using System.Collections;

public class DiaryController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject passwordPanel;  // 密码面板
    [SerializeField] private GameObject contentPanel;   // 日记内容面板
    [SerializeField] private TMPro.TMP_InputField passwordInputField; // 密码输入框

    [Header("Settings")]
    [SerializeField] private string correctPassword = "8541"; // 正确密码
    [SerializeField] private float interactionDistance = 3f; // 交互距离

    private bool isPlayerNear = false;
    private bool isUnlocked = false; // 日记是否已解锁
    private Transform playerTransform;
    private Camera mainCamera;

    void Start()
    {
        // 获取玩家和相机引用
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        mainCamera = Camera.main;

        // 隐藏UI面板
        if (passwordPanel != null) passwordPanel.SetActive(false);
        if (contentPanel != null) contentPanel.SetActive(false);
    }

    void Update()
    {
        CheckPlayerDistance();

        if (isPlayerNear && Input.GetKeyDown(KeyCode.O))
        {
            if (!isUnlocked)
            {
                OpenPasswordPanel();
            }
            else
            {
                OpenContentPanel();
            }
        }
    }

    void CheckPlayerDistance()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = distance <= interactionDistance;
    }

    void OpenPasswordPanel()
    {
        if (passwordPanel != null)
        {
            passwordPanel.SetActive(true);
            Time.timeScale = 0; // 暂停游戏
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 清空输入框
            if (passwordInputField != null)
                passwordInputField.text = "";
        }
    }

    void OpenContentPanel()
    {
        if (contentPanel != null)
        {
            contentPanel.SetActive(true);
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // 验证密码方法（由UI按钮调用）
    public void ValidatePassword()
    {
        if (passwordInputField == null) return;

        string inputPassword = passwordInputField.text;

        if (inputPassword == correctPassword)
        {
            isUnlocked = true;
            ClosePasswordPanel();
            OpenContentPanel();
            Debug.Log("日记解锁成功！");
        }
        else
        {
            Debug.Log("密码错误！");
            // 可以添加密码错误反馈，如输入框抖动效果
        }
    }

    // 关闭密码面板
    public void ClosePasswordPanel()
    {
        if (passwordPanel != null)
        {
            passwordPanel.SetActive(false);
            ResumeGame();
        }
    }

    // 关闭内容面板
    public void CloseContentPanel()
    {
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
            ResumeGame();
        }
    }

    void ResumeGame()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 在Scene视图中显示交互范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}