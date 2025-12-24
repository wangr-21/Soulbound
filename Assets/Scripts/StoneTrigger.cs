using UnityEngine;

public class StoneTrigger : MonoBehaviour
{
    public StonePuzzleManager puzzleManager; // 拖入场景中的管理器

    private void OnTriggerEnter(Collider other)
    {
        // 新增：打印碰撞到的物体信息，排查是否检测到玩家
        Debug.Log($"石头{gameObject.name}碰撞到了：{other.gameObject.name}，其Tag是：{other.tag}");

        // 仅响应“Player”标签的物体（需给玩家对象设Tag为Player）
        if (other.CompareTag("Player"))
        {
            puzzleManager.OnStoneStepped(this.gameObject);
        }
    }
}