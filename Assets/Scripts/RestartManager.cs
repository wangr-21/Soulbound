// RestartManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class RestartManager : MonoBehaviour
{
    public static RestartManager Instance { get; private set; }

    [Header("游戏结束UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public float showGameOverTime = 2f;

    private bool isRestarting = false;
    private string currentGameScene = "GameScene"; // 修改为你的游戏场景名

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 显示游戏结束并重启
    public void ShowGameOverAndRestart(string message = "Over!")
    {
        if (isRestarting) return;

        StartCoroutine(GameOverRestartSequence(message));
    }

    private IEnumerator GameOverRestartSequence(string message)
    {
        isRestarting = true;

        // 1. 显示游戏结束UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        if (gameOverText != null)
        {
            gameOverText.text = message;
        }

        // 2. 暂停游戏（可选）
        Time.timeScale = 0.2f;

        // 3. 等待显示时间
        yield return new WaitForSecondsRealtime(showGameOverTime);

        // 4. 重置UI管理器
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResetGameUI();
        }

        // 5. 重置游戏管理器
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }

        // 6. 重置玩家状态
        if (PlayerSoulController.Instance != null)
        {
            PlayerSoulController.Instance.ResetPlayerState();
        }

        // 7. 重新加载场景
        Time.timeScale = 1f;
        SceneManager.LoadScene(currentGameScene);

        isRestarting = false;
    }
}