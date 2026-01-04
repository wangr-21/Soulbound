//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;

//public class RestartButton : MonoBehaviour
//{
//    void Start()
//    {
//        // 获取Button组件
//        Button button = GetComponent<Button>();

//        // 如果没有Button组件，添加一个
//        if (button == null)
//        {
//            button = gameObject.AddComponent<Button>();
//        }

//        // 移除所有现有监听器
//        button.onClick.RemoveAllListeners();

//        // 绑定点击事件
//        button.onClick.AddListener(OnRestartButtonClick);
//    }

//    void OnRestartButtonClick()
//    {
//        Debug.Log("RestartButton: 按钮被点击");

//        // 优先使用UIManager的重新开始方法
//        if (UIManager.Instance != null)
//        {
//            UIManager.Instance.RestartGame();
//        }
//        else
//        {
//            // 备用方案：直接重新加载场景
//            Debug.LogWarning("RestartButton: UIManager实例未找到，使用备用重新开始方案");
//            RestartLevel();
//        }
//    }

//    void RestartLevel()
//    {
//        Debug.Log("RestartButton: 直接重新加载场景");

//        // 确保时间正常
//        Time.timeScale = 1f;

//        // 重新加载当前场景
//        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
//    }
//}