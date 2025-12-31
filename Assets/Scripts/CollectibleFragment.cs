using UnityEngine;
using TMPro;
using System.Collections; // 新增：协程必需的命名空间
using System.Collections.Generic;

public class CollectibleFragment : MonoBehaviour
{
    public TextMeshProUGUI successHintText;
    private Coroutine hideSuccessCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("收集到场景碎片，数量+1！");
            FragmentManager.Instance.AddFragment(); // 场景碎片单独+1
            ShowSuccessThenHide(2f);
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }

    // 添加显示和隐藏成功提示的方法
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