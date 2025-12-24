using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance { get; private set; }
    public string LastSceneName { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 注册场景加载完成事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadSceneWithRecord(string sceneName)
    {
        // 保存当前场景状态
        SaveCurrentSceneState();
        // 记录上一个场景
        LastSceneName = SceneManager.GetActiveScene().name;
        // 加载新场景
        SceneManager.LoadScene(sceneName);
    }

    private void SaveCurrentSceneState()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("LastSavedScene", currentScene);

        // 保存玩家位置
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            PlayerPrefs.SetFloat($"{currentScene}_PlayerPosX", pos.x);
            PlayerPrefs.SetFloat($"{currentScene}_PlayerPosY", pos.y);
            PlayerPrefs.SetFloat($"{currentScene}_PlayerPosZ", pos.z);
        }

        PlayerPrefs.Save();
    }

    public void RestoreSceneState()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        string lastSavedScene = PlayerPrefs.GetString("LastSavedScene", "");

        if (currentScene != lastSavedScene) return;

        // 恢复玩家位置
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float x = PlayerPrefs.GetFloat($"{currentScene}_PlayerPosX", player.transform.position.x);
            float y = PlayerPrefs.GetFloat($"{currentScene}_PlayerPosY", player.transform.position.y);
            float z = PlayerPrefs.GetFloat($"{currentScene}_PlayerPosZ", player.transform.position.z);
            player.transform.position = new Vector3(x, y, z);
        }
    }

    // 新增场景加载完成时的回调
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 当加载的是之前保存过状态的场景时，自动恢复状态
        if (scene.name == PlayerPrefs.GetString("LastSavedScene", ""))
        {
            RestoreSceneState();
        }
    }

    private void OnDestroy()
    {
        // 移除事件监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}