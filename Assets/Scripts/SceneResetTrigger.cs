// SceneResetTrigger.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneResetTrigger : MonoBehaviour
{
    void Start()
    {
        // 确保UIManager在场景开始时正确重置
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResetGameUI();
        }

        // 确保时间缩放正确
        Time.timeScale = 1f;
    }
}