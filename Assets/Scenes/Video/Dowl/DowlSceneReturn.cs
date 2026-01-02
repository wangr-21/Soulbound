using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DowlSceneReturn : MonoBehaviour
{
    [Header("返回按钮")]
    public Button returnButton;

    void Awake()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(ReturnToMainScene);
        }
        else
        {
            Debug.LogError("DowlSceneReturn脚本缺少返回按钮引用");
        }
    }

    void ReturnToMainScene()
    {
        // 恢复时间
        Time.timeScale = 1f;

        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 淡入主场景BGM
        MainBGMController.Instance?.FadeIn(1f, 0.5f);

        // 重置所有RecallTrigger状态
        ResetAllRecallTriggers();

        // 卸载回忆场景，并添加回调
        SceneManager.UnloadSceneAsync("DowlScene").completed += OnDowlSceneUnloaded;
    }

    // 添加场景卸载完成回调
    private void OnDowlSceneUnloaded(AsyncOperation operation)
    {
        // 场景卸载后，确保 PlayerSoulController 恢复计时
        PlayerSoulController soulController = PlayerSoulController.Instance;
        if (soulController != null)
        {
            // 调用公共方法来处理场景切换后的状态恢复
            soulController.OnReturnFromDowlScene();
        }
    }

    // 重置所有RecallTrigger的状态
    private void ResetAllRecallTriggers()
    {
        // 查找场景中的所有RecallTrigger
        var recallTriggers = FindObjectsOfType<RecallTrigger>();
        foreach (var trigger in recallTriggers)
        {
            trigger.ResetRecallState();
        }

        Debug.Log($"已重置 {recallTriggers.Length} 个RecallTrigger的状态");
    }
}