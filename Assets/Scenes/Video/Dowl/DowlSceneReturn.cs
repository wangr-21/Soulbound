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
            Debug.LogError("DowlSceneReturn：未绑定返回按钮！");
        }
    }

    void ReturnToMainScene()
    {
        // 恢复时间
        Time.timeScale = 1f;

        // 恢复鼠标（如果你主场景需要锁定）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ★ 主场景音乐淡入
        MainBGMController.Instance?.FadeIn(1f, 0.5f);

        // 卸载回忆场景
        SceneManager.UnloadSceneAsync("DowlScene");
    }
}
