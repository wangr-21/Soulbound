using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoldierSceneReturn : MonoBehaviour
{
    [Header("返回按钮")]
    public Button returnButton;

    void Awake()
    {
        // 绑定返回按钮点击事件
        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(ReturnToMainScene);
        }
        else
        {
            Debug.LogError("SoldierSceneReturn：未分配返回按钮！");
        }
    }

    void ReturnToMainScene()
    {
        // 1. 重置时间缩放（防止时间异常）
        Time.timeScale = 1f;

        // 2. 重置光标状态（主界面默认锁定）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. 淡入背景音乐（恢复主界面音效）
        MainBGMController.Instance?.FadeIn(1f, 0.5f);

        // 4. 异步卸载SoldierScene（避免卡顿），卸载完成后重置触发状态
        var unloadOp = SceneManager.UnloadSceneAsync("SoldierScene");
        unloadOp.completed += (op) =>
        {
            // 找到主场景的SoldierRecallTrigger并重置状态（解决文本残留）
            var soldierTriggers = Object.FindObjectsOfType<SoldierRecallTrigger>();
            foreach (var trigger in soldierTriggers)
            {
                trigger.ResetRecallState();
            }
        };
    }
}