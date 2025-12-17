using UnityEngine;

public class CharacterControllerAdjuster : MonoBehaviour
{
    public CharacterController controller;
    public MeshRenderer modelRenderer; // 拖入Leopard_Hybrid的MeshRenderer

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("没有找到CharacterController！");
            return;
        }

        Debug.Log("=== CharacterController 当前设置 ===");
        Debug.Log($"Center: {controller.center}");
        Debug.Log($"Height: {controller.height}");
        Debug.Log($"Radius: {controller.radius}");

        // 如果有模型渲染器，计算包围盒
        if (modelRenderer != null)
        {
            Bounds bounds = modelRenderer.bounds;
            Debug.Log($"模型包围盒:");
            Debug.Log($"  中心: {bounds.center}");
            Debug.Log($"  大小: {bounds.size}");
            Debug.Log($"  最小点: {bounds.min}");
            Debug.Log($"  最大点: {bounds.max}");

            // 计算相对于父对象的包围盒
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            Debug.Log($"  相对于父对象的中心: {localCenter}");
        }
    }

    void Update()
    {
        // 按数字键调整，方便测试
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            controller.center = new Vector3(0, 1, 0);
            Debug.Log($"设置Center为: {controller.center}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            controller.height = 2f;
            Debug.Log($"设置Height为: {controller.height}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            controller.radius = 0.5f;
            Debug.Log($"设置Radius为: {controller.radius}");
        }
    }

    void OnDrawGizmos()
    {
        if (controller == null) return;

        // 绘制CharacterController的线框
        Gizmos.color = Color.green;
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // 考虑对象的旋转和缩放
        Gizmos.matrix = Matrix4x4.TRS(
            transform.position + transform.rotation * controller.center,
            transform.rotation,
            transform.lossyScale
        );

        // 绘制胶囊体
        DrawWireCapsule(Vector3.zero, controller.height, controller.radius);

        Gizmos.matrix = oldMatrix;
    }

    void DrawWireCapsule(Vector3 center, float height, float radius)
    {
        float halfHeight = height * 0.5f - radius;

        // 绘制中间圆柱体
        Vector3 top = center + Vector3.up * halfHeight;
        Vector3 bottom = center - Vector3.up * halfHeight;

        // 绘制顶部半球
        Gizmos.DrawWireSphere(top, radius);
        // 绘制底部半球
        Gizmos.DrawWireSphere(bottom, radius);

        // 绘制四条垂直线
        Gizmos.DrawLine(
            center + new Vector3(radius, halfHeight, 0),
            center + new Vector3(radius, -halfHeight, 0)
        );
        Gizmos.DrawLine(
            center + new Vector3(-radius, halfHeight, 0),
            center + new Vector3(-radius, -halfHeight, 0)
        );
        Gizmos.DrawLine(
            center + new Vector3(0, halfHeight, radius),
            center + new Vector3(0, -halfHeight, radius)
        );
        Gizmos.DrawLine(
            center + new Vector3(0, halfHeight, -radius),
            center + new Vector3(0, -halfHeight, -radius)
        );
    }
}