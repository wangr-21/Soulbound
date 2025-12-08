using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 如果需要显示UI文本

public class AutoSceneChanger : MonoBehaviour
{
    [Header("跳转设置")]
    [Tooltip("要跳转到的场景名称")]
    public string nextSceneName = "SampleScene"; // 默认跳转到SampleScene

    [Tooltip("等待时间（秒）")]
    public float waitTime = 10.0f; // 等待10秒后跳转

    [Header("UI显示（可选）")]
    public Text countdownText; // 显示倒计时的UI文本
    public GameObject skipPrompt; // "按任意键跳过"提示

    private float timer;
    private bool canSkip = false;

    void Start()
    {
        // 初始化计时器
        timer = waitTime;

        // 显示开始信息
        Debug.Log($"[开场动画] 开始播放");
        Debug.Log($"[开场动画] {waitTime}秒后跳转到: {nextSceneName}");

        // 显示跳过提示（延迟显示）
        if (skipPrompt != null)
        {
            skipPrompt.SetActive(false);
            Invoke("ShowSkipPrompt", 2f); // 2秒后显示跳过提示
        }

        // 更新倒计时UI
        UpdateCountdownText();
    }

    void Update()
    {
        // 更新计时器
        timer -= Time.deltaTime;

        // 每1秒更新一次UI（节省性能）
        if (Time.frameCount % 60 == 0) // 大约每秒更新一次
        {
            UpdateCountdownText();
        }

        // 检查是否到时间
        if (timer <= 0)
        {
            LoadNextScene();
        }

        // 跳过功能
        if (canSkip && Input.anyKeyDown)
        {
            Debug.Log("玩家跳过开场动画");
            LoadNextScene();
        }
    }

    void UpdateCountdownText()
    {
        if (countdownText != null)
        {
            int secondsLeft = Mathf.CeilToInt(timer);
            countdownText.text = $"开场动画 ({secondsLeft}秒后开始游戏...)";
        }
    }

    void ShowSkipPrompt()
    {
        canSkip = true;
        if (skipPrompt != null)
        {
            skipPrompt.SetActive(true);
            Debug.Log("现在可以按任意键跳过");
        }
    }

    void LoadNextScene()
    {
        Debug.Log($"正在跳转到: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    // 提供给UI按钮的跳过方法
    public void SkipNow()
    {
        LoadNextScene();
    }
}