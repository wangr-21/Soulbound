using UnityEngine;

public class PossessionDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== 附身系统诊断 ===");
        Debug.Log($"诊断对象: {gameObject.name}");
        Debug.Log($"对象位置: {transform.position}");

        // 1. 检查所有组件
        Component[] allComponents = GetComponents<Component>();
        Debug.Log($"组件总数: {allComponents.Length}");

        foreach (Component comp in allComponents)
        {
            Debug.Log($"- {comp.GetType().Name}: {comp.GetType().FullName}");
        }

        // 2. 专门检查LeopardController
        LeopardController leopard = GetComponent<LeopardController>();
        if (leopard != null)
        {
            Debug.Log($"? 找到LeopardController组件");
            Debug.Log($"  objectName: {leopard.objectName}");
            Debug.Log($"  abilityDescription: {leopard.abilityDescription}");

            // 检查继承关系
            Debug.Log($"  LeopardController继承自: {leopard.GetType().BaseType}");
        }
        else
        {
            Debug.LogError("? 没有找到LeopardController组件!");

            // 检查是否有其他PossessableBase
            PossessableBase[] possessables = GetComponents<PossessableBase>();
            Debug.Log($"PossessableBase组件数量: {possessables.Length}");
        }

        // 3. 检查IPossessable接口
        IPossessable possessable = GetComponent<IPossessable>();
        if (possessable != null)
        {
            Debug.Log($"? 直接获取到IPossessable接口");
        }
        else
        {
            Debug.LogError("? 无法直接获取IPossessable接口");

            // 尝试通过类型转换
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script is IPossessable)
                {
                    Debug.Log($"  通过类型转换找到: {script.GetType().Name} 实现了IPossessable");
                }
            }
        }

        // 4. 检查碰撞体
        Collider[] colliders = GetComponents<Collider>();
        Debug.Log($"碰撞体数量: {colliders.Length}");
        foreach (Collider col in colliders)
        {
            Debug.Log($"- {col.GetType().Name}: isTrigger={col.isTrigger}, enabled={col.enabled}");
        }

        Debug.Log("=== 诊断完成 ===");
    }
}