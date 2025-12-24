using UnityEngine;

public class BearMemoryTrigger : MonoBehaviour
{
    public GameObject memoryButtonUI; // ªÿ“‰∞¥≈•UI

    private void Start()
    {
        memoryButtonUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            memoryButtonUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            memoryButtonUI.SetActive(false);
        }
    }
}
