using UnityEngine;

public class CollectibleFragment : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("收集到场景碎片，数量+1！");
            FragmentManager.Instance.AddFragment(); // 场景碎片单独+1
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }
}