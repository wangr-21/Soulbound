using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 点击按钮时执行的方法
    public void LoadSampleScene()
    {
        // 跳转到“SampleScene”场景
        SceneManager.LoadScene("SampleScene");
    }
}