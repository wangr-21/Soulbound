// UIMessageManager.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class UIMessageManager : MonoBehaviour
{
    public static UIMessageManager Instance;

    [SerializeField] private TextMeshProUGUI successHintText; // 拖拽UI文本组件到这里
    private Coroutine hideMessageCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保场景切换时不被销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 显示提示信息并自动隐藏
    public void ShowMessage(string message, float hideDelay = 1f)
    {
        successHintText.gameObject.SetActive(true);
        successHintText.text = message;

        // 停止之前的协程，避免冲突
        if (hideMessageCoroutine != null)
            StopCoroutine(hideMessageCoroutine);

        // 在当前管理器上启动协程（不会因其他物体销毁而中断）
        hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(hideDelay));
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // 不受时间缩放影响
        successHintText.gameObject.SetActive(false);
        hideMessageCoroutine = null;
    }
}