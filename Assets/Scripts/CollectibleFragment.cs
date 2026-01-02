using UnityEngine;
using TMPro;
using System.Collections;

public class CollectibleFragment : MonoBehaviour
{
    public TextMeshProUGUI successHintText;
    private Coroutine hideSuccessCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家接触到碎片，碎片+1！");
            FragmentManager.Instance.AddFragment(); // 碎片收集数量+1
            ShowSuccessThenHide(2f);
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }

    // 显示成功提示然后隐藏的方法
    private void ShowSuccessThenHide(float delay)
    {
        successHintText.gameObject.SetActive(true);
        successHintText.text = "恭喜你获得1块碎片！";
        if (hideSuccessCoroutine != null)
            StopCoroutine(hideSuccessCoroutine);
        hideSuccessCoroutine = StartCoroutine(HideSuccessCoroutine(delay));
    }

    private IEnumerator HideSuccessCoroutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        successHintText.gameObject.SetActive(false);
        hideSuccessCoroutine = null;
    }
}