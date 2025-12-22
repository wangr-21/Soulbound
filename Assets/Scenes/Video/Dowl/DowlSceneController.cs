using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DowlSceneController : MonoBehaviour
{
    public float memoryDuration = 8f; // æÁ«È ±≥§£®√Î£©

    private void Start()
    {
        StartCoroutine(BackToMainScene());
    }

    IEnumerator BackToMainScene()
    {
        yield return new WaitForSeconds(memoryDuration);
        SceneManager.LoadScene("SampleScene");
    }
}
