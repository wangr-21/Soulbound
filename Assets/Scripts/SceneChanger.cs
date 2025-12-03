using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // 跳转到指定场景
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // 快捷返回开始界面
    public void BackToStart()
    {
        SceneManager.LoadScene("StartScene");
    }
}