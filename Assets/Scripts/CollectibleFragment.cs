using UnityEngine;
using TMPro;

public class CollectibleFragment : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("触发碎片收集，碎片+1");
            FragmentManager.Instance.AddFragment();
            // 替换为调用全局提示管理器
            UIMessageManager.Instance.ShowMessage("恭喜你获得1块碎片！", 1f);
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }
}