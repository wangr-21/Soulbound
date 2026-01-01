using UnityEngine;

public class TestObjectController : PossessableBase
{
    [Header("测试对象设置")]
    public float rotationSpeed = 90f; // 度/秒

    private void Start()
    {
        objectName = "测试旋转对象";
        abilityDescription = "按AD键旋转";
    }

    public override void PossessedUpdate()
    {
        // 这个对象被附身时可以用AD键旋转
        float rotateInput = Input.GetAxis("Horizontal");
        transform.Rotate(0, rotateInput * rotationSpeed * Time.deltaTime, 0);
    }
}