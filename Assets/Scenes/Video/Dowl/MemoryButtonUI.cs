using UnityEngine;
using UnityEngine.SceneManagement;

public class MemoryButtonUI : MonoBehaviour
{
    public void EnterMemoryScene()
    {
        SceneManager.LoadScene("DowlScene");
    }
}
