using UnityEngine;

// 可附身对象的基类，处理通用功能
public abstract class PossessableBase : MonoBehaviour, IPossessable
{
    [Header("附身设置")]
    public string objectName = "未知对象"; // 对象名称
    public string abilityDescription = "暂无描述"; // 能力描述

    protected bool isPossessed = false; // 是否被附身
    protected Material originalMaterial; // 原始材质
    protected Renderer objectRenderer;

    protected virtual void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        // 自动设置对象名称
        if (objectName == "未知对象")
        {
            objectName = gameObject.name;
        }
    }

    // 通用的附身方法
    public virtual void OnPossess()
    {
        isPossessed = true;
        Debug.Log($"附身到 {objectName}：{abilityDescription}");

        // 视觉反馈：改变颜色
        if (objectRenderer != null)
        {
            objectRenderer.material.color = Color.green;
        }
    }

    // 通用的脱离方法
    public virtual void OnRelease()
    {
        isPossessed = false;
        Debug.Log($"从 {objectName} 脱离");

        // 恢复原始外观
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material.color = Color.white;
        }
    }

    // 抽象方法，子类必须实现
    public abstract void PossessedUpdate();

    // 获取能力描述
    public virtual string GetAbilityDescription()
    {
        return abilityDescription;
    }
}