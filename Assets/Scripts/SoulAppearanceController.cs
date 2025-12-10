using UnityEngine;

public class SoulAppearanceController : MonoBehaviour
{
    [Header("粒子系统引用")]
    public ParticleSystem bodyParticles;
    public ParticleSystem coreParticles;

    [Header("动态效果参数")]
    public float normalEmissionRate = 30f;
    public float scaredEmissionRate = 60f;
    public float normalSize = 0.8f;  // 增大了基础尺寸
    public float scaredSize = 0.4f;

    [Header("呼吸效果")]
    public float breathIntensity = 0.1f;
    public float breathSpeed = 1f;

    private ParticleSystem.EmissionModule bodyEmission;
    private ParticleSystem.MainModule bodyMain;
    private ParticleSystem.MainModule coreMain;

    void Start()
    {
        // 确保引用存在
        InitializeReferences();

        // 初始隐藏（如果默认应该显示，可以注释掉这行）
        // ShowSoul();
    }

    void Update()
    {
        // 基础呼吸效果
        if (bodyParticles != null && bodyParticles.gameObject.activeSelf)
        {
            float breath = Mathf.Sin(Time.time * breathSpeed) * breathIntensity;
            bodyMain.startSize = normalSize + breath;
        }
    }

    // 初始化引用
    private void InitializeReferences()
    {
        // 尝试自动查找粒子系统
        if (bodyParticles == null)
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in allParticles)
            {
                if (ps.gameObject.name.Contains("Body") || ps.gameObject.name.Contains("body"))
                {
                    bodyParticles = ps;
                }
                else if (ps.gameObject.name.Contains("Core") || ps.gameObject.name.Contains("core"))
                {
                    coreParticles = ps;
                }
            }
        }

        // 获取粒子系统模块
        if (bodyParticles != null)
        {
            bodyEmission = bodyParticles.emission;
            bodyMain = bodyParticles.main;
        }

        if (coreParticles != null)
        {
            coreMain = coreParticles.main;
        }

        // 如果仍然没有找到，警告
        if (bodyParticles == null)
        {
            Debug.LogWarning("SoulAppearanceController: 未找到bodyParticles引用");
        }
        if (coreParticles == null)
        {
            Debug.LogWarning("SoulAppearanceController: 未找到coreParticles引用");
        }
    }

    // 隐藏灵魂（附身时调用）
    public void HideSoul()
    {
        if (bodyParticles != null && bodyParticles.gameObject.activeSelf)
        {
            bodyParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            bodyParticles.gameObject.SetActive(false);
        }

        if (coreParticles != null && coreParticles.gameObject.activeSelf)
        {
            coreParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            coreParticles.gameObject.SetActive(false);
        }

        Debug.Log("灵魂粒子系统已隐藏");
    }

    // 显示灵魂（脱离附身时调用）
    public void ShowSoul()
    {
        // 确保位置正确（在父物体位置）
        Vector3 currentPosition = transform.position;

        if (bodyParticles != null)
        {
            bodyParticles.transform.position = currentPosition;
            bodyParticles.gameObject.SetActive(true);
            bodyParticles.Play();

            // 重置到正常状态
            ReturnToNormal();
        }

        if (coreParticles != null)
        {
            coreParticles.transform.position = currentPosition;
            coreParticles.gameObject.SetActive(true);
            coreParticles.Play();
        }

        Debug.Log("灵魂粒子系统已显示在位置: " + currentPosition);
    }

    // 显示灵魂在指定位置（可选的精确位置控制）
    public void ShowSoulAtPosition(Vector3 position)
    {
        transform.position = position;
        ShowSoul();
    }

    // 受到惊吓时的收缩效果
    public void OnScared()
    {
        if (bodyParticles == null) return;

        bodyMain.startSize = scaredSize;
        bodyEmission.rateOverTime = scaredEmissionRate;

        // 0.5秒后恢复
        Invoke("ReturnToNormal", 0.5f);

        Debug.Log("灵魂受到惊吓");
    }

    // 恢复到正常状态
    private void ReturnToNormal()
    {
        if (bodyParticles == null) return;

        bodyMain.startSize = normalSize;
        bodyEmission.rateOverTime = normalEmissionRate;
    }

    // 立即恢复正常状态
    public void ReturnToNormalImmediate()
    {
        CancelInvoke("ReturnToNormal");
        ReturnToNormal();
    }

    // 改变核心颜色（用于游戏进度）
    public void ChangeCoreColor(Color newColor)
    {
        if (coreParticles == null) return;

        coreMain.startColor = newColor;
        Debug.Log("核心颜色已改为: " + newColor);
    }

    // 改变核心亮度
    public void ChangeCoreBrightness(float intensity)
    {
        if (coreParticles == null) return;

        Renderer coreRenderer = coreParticles.GetComponent<Renderer>();
        if (coreRenderer != null && coreRenderer.material != null)
        {
            // 尝试设置自发光颜色
            coreRenderer.material.SetColor("_EmissionColor", coreMain.startColor.color * intensity);

            // 启用自发光
            coreRenderer.material.EnableKeyword("_EMISSION");
        }
    }

    // 设置粒子系统整体强度
    public void SetIntensity(float intensity)
    {
        // 调整发射率
        if (bodyParticles != null)
        {
            bodyEmission.rateOverTime = normalEmissionRate * intensity;
        }

        // 调整核心亮度
        ChangeCoreBrightness(intensity);
    }

    // 获取当前灵魂是否可见
    public bool IsSoulVisible()
    {
        bool bodyVisible = bodyParticles != null && bodyParticles.gameObject.activeSelf;
        bool coreVisible = coreParticles != null && coreParticles.gameObject.activeSelf;

        return bodyVisible || coreVisible;
    }

    // 用于调试：在Scene视图中显示粒子系统位置
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (bodyParticles != null)
        {
            Gizmos.DrawWireSphere(bodyParticles.transform.position, 0.5f);
        }
    }

    // 清理：取消所有Invoke调用
    void OnDestroy()
    {
        CancelInvoke();
    }
}