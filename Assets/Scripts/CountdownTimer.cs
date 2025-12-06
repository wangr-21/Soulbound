using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    public TextMeshProUGUI countdownText;
    private float remainingTime;
    private bool isCounting = false;

    // 启动倒计时（参数：总秒数）
    public void StartCountdown(float totalSec)
    {
        remainingTime = totalSec;
        isCounting = true;
        UpdateCountdownText(); // 初始化显示时间
        Debug.Log("倒计时启动，总秒数：" + totalSec); // 调试日志
    }

    // 停止倒计时
    public void StopCountdown()
    {
        isCounting = false;
        Debug.Log("倒计时停止"); // 调试日志
    }

    void Update()
    {
        // 仅在计时中且文本组件有效时更新
        if (isCounting && countdownText != null && remainingTime > 0)
        {
            // 使用 unscaledDeltaTime：避免游戏暂停（如切面板）时倒计时停止
            remainingTime -= Time.unscaledDeltaTime;
            UpdateCountdownText();
        }
        // 时间到，触发关闭谜题
        else if (isCounting)
        {
            isCounting = false;
            Debug.Log("倒计时结束"); // 调试日志
            PuzzleManager.Instance.OnTimeEnd();
        }
    }

    // 单独更新文本，确保显示正确
    private void UpdateCountdownText()
    {
        if (countdownText == null)
        {
            Debug.LogError("CountdownText 未关联！");
            return;
        }

        // 确保文本组件激活（避免被误禁用）
        if (!countdownText.gameObject.activeSelf)
            countdownText.gameObject.SetActive(true);

        // 计算分秒，确保不出现负数
        int min = Mathf.Max(0, Mathf.FloorToInt(remainingTime / 60));
        int sec = Mathf.Max(0, Mathf.FloorToInt(remainingTime % 60));
        countdownText.text = $"倒计时 {min:D2}:{sec:D2}";
    }
}