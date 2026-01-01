using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneSwitchTrigger : MonoBehaviour
{
    // TMP提示文本引用
    public TMP_Text enterTipText;
    // 目标场景名称
    public string targetSceneName = "KingScene";
    // 标记玩家是否在触发区内
    private bool isPlayerInTrigger = false;

    void Start()
    {
        // 初始隐藏提示文本
        if (enterTipText != null)
        {
            enterTipText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 玩家在触发区且按B键时跳转场景
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.B))
        {
            SwitchScene();
        }
    }

    // 玩家进入触发区：显示提示+临时解锁光标
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            // 显示提示文本
            if (enterTipText != null)
            {
                enterTipText.gameObject.SetActive(true);
            }
            // 临时解锁光标，方便玩家看到光标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // 玩家离开触发区：隐藏提示
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            // 隐藏提示文本
            if (enterTipText != null)
            {
                enterTipText.gameObject.SetActive(false);
            }
            // 可选：离开触发区后恢复光标锁定（根据游戏需求决定是否启用）
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
        }
    }

    // 场景跳转：核心修复——解锁光标+显示光标
    private void SwitchScene()
    {
        // 关键：解锁光标，允许交互UI（删除了错误的raycastTarget行）
        Cursor.lockState = CursorLockMode.None; // 解锁光标移动
        Cursor.visible = true; // 显示光标

        // 加载目标场景
        SceneManager.LoadScene(targetSceneName);
    }

    // 编辑器验证场景名
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("请设置目标场景名称！", this);
        }
    }
}