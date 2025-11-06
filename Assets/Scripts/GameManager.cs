using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("游戏状态")]
    public bool isGamePaused = false;
    public string currentCheckpoint = "初始点";
    public int playerScore = 0;

    void Awake()
    {
        // 单例模式确保只有一个GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 切换场景时不销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 处理全局输入，比如暂停游戏
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0 : 1; // 暂停/恢复游戏时间
        Debug.Log(isGamePaused ? "游戏已暂停" : "游戏已继续");
    }

    // 保存游戏进度
    public void SaveGame(string checkpointName)
    {
        currentCheckpoint = checkpointName;
        Debug.Log($"游戏已保存到检查点: {checkpointName}");
    }

    // 加载游戏进度
    public void LoadGame()
    {
        Debug.Log($"从检查点加载游戏: {currentCheckpoint}");
    }

    // 添加分数（后续可用于解谜奖励）
    public void AddScore(int points)
    {
        playerScore += points;
        Debug.Log($"获得 {points} 分，当前总分: {playerScore}");
    }
}