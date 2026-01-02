using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoldierSceneReturn : MonoBehaviour
{
    [Header("返回按钮")]
    public Button returnButton;

    void Awake()
    {
        // 确保返回按钮监听正确
        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(ReturnToMainScene);
        }
        else
        {
            Debug.LogError("SoldierSceneReturn：缺少返回按钮引用！");
        }
    }

    void ReturnToMainScene()
    {
        // 1. 恢复时间缩放
        Time.timeScale = 1f;

        // 2. 恢复光标锁定
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. 淡入背景音乐
        MainBGMController.Instance?.FadeIn(1f, 0.5f);

        // 4. 异步卸载SoldierScene，完成后重置状态
        var unloadOp = SceneManager.UnloadSceneAsync("SoldierScene");
        unloadOp.completed += OnSoldierSceneUnloaded;
    }

    // 场景卸载完成回调
    private void OnSoldierSceneUnloaded(AsyncOperation operation)
    {
        // 重置场景中的SoldierRecallTrigger状态
        var soldierTriggers = Object.FindObjectsOfType<SoldierRecallTrigger>();
        foreach (var trigger in soldierTriggers)
        {
            trigger.ResetRecallState();
        }

        // 通知PlayerSoulController恢复计时器（使用独立的方法）
        PlayerSoulController soulController = PlayerSoulController.Instance;
        if (soulController != null)
        {
            soulController.OnReturnFromSoldierScene();
        }
    }
}