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

    // 不再缓存模块，改为缓存粒子系统引用
    private bool referencesInitialized = false;
    private float currentBreathOffset = 0f;

    void Start()
    {
        // 初始化引用
        InitializeReferences();

        // 初始状态
        currentBreathOffset = Random.Range(0f, 2f * Mathf.PI); // 随机相位，让不同灵魂的呼吸不同步
    }

    void Update()
    {
        // 基础呼吸效果
        if (bodyParticles != null && bodyParticles.gameObject.activeSelf)
        {
            // 直接通过粒子系统获取Main模块
            var mainModule = bodyParticles.main;
            float breath = Mathf.Sin((Time.time + currentBreathOffset) * breathSpeed) * breathIntensity;
            mainModule.startSize = normalSize + breath;
        }
    }

    // 初始化引用
    private void InitializeReferences()
    {
        if (referencesInitialized) return;

        // 尝试自动查找粒子系统
        if (bodyParticles == null || coreParticles == null)
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>(true); // 包括未激活的

            // 如果没有指定，使用第一个找到的粒子系统作为body，第二个作为core
            if (allParticles.Length > 0)
            {
                if (bodyParticles == null) bodyParticles = allParticles[0];
                if (coreParticles == null && allParticles.Length > 1) coreParticles = allParticles[1];
            }
        }

        // 验证引用
        if (bodyParticles == null)
        {
            Debug.LogWarning($"SoulAppearanceController ({name}): 未找到bodyParticles引用");
        }
        else
        {
            // 确保body粒子系统初始状态正确
            var emission = bodyParticles.emission;
            var main = bodyParticles.main;

            emission.rateOverTime = normalEmissionRate;
            main.startSize = normalSize;
        }

        if (coreParticles == null)
        {
            Debug.LogWarning($"SoulAppearanceController ({name}): 未找到coreParticles引用");
        }

        referencesInitialized = true;
    }

    // 延迟初始化（如果需要）
    private void EnsureInitialized()
    {
        if (!referencesInitialized)
        {
            InitializeReferences();
        }
    }

    // 隐藏灵魂（附身时调用）
    public void HideSoul()
    {
        EnsureInitialized();

        if (bodyParticles != null)
        {
            // 先停止发射，然后再禁用
            bodyParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // 等待一帧确保粒子停止，或者使用协程
            if (bodyParticles.gameObject.activeSelf)
            {
                bodyParticles.gameObject.SetActive(false);
            }
        }

        if (coreParticles != null)
        {
            coreParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (coreParticles.gameObject.activeSelf)
            {
                coreParticles.gameObject.SetActive(false);
            }
        }

        Debug.Log($"灵魂粒子系统已隐藏: {name}");
    }

    // 显示灵魂（脱离附身时调用）
    public void ShowSoul()
    {
        EnsureInitialized();

        // 确保位置正确（在父物体位置）
        Vector3 currentPosition = transform.position;

        if (bodyParticles != null)
        {
            bodyParticles.transform.position = currentPosition;
            if (!bodyParticles.gameObject.activeSelf)
            {
                bodyParticles.gameObject.SetActive(true);
            }

            // 重置到正常状态
            ReturnToNormalImmediate();

            // 如果没在播放，开始播放
            if (!bodyParticles.isPlaying)
            {
                bodyParticles.Play();
            }
        }

        if (coreParticles != null)
        {
            coreParticles.transform.position = currentPosition;
            if (!coreParticles.gameObject.activeSelf)
            {
                coreParticles.gameObject.SetActive(true);
            }

            if (!coreParticles.isPlaying)
            {
                coreParticles.Play();
            }
        }

        Debug.Log($"灵魂粒子系统已显示在位置: {currentPosition}, 对象: {name}");
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
        EnsureInitialized();

        if (bodyParticles == null) return;

        var main = bodyParticles.main;
        var emission = bodyParticles.emission;

        main.startSize = scaredSize;
        emission.rateOverTime = scaredEmissionRate;

        // 0.5秒后恢复
        Invoke(nameof(ReturnToNormal), 0.5f);

        Debug.Log($"灵魂受到惊吓: {name}");
    }

    // 恢复到正常状态
    private void ReturnToNormal()
    {
        EnsureInitialized();

        if (bodyParticles == null) return;

        var main = bodyParticles.main;
        var emission = bodyParticles.emission;

        main.startSize = normalSize;
        emission.rateOverTime = normalEmissionRate;
    }

    // 立即恢复正常状态
    public void ReturnToNormalImmediate()
    {
        CancelInvoke(nameof(ReturnToNormal));
        ReturnToNormal();
    }

    // 改变核心颜色（用于游戏进度）
    public void ChangeCoreColor(Color newColor)
    {
        EnsureInitialized();

        if (coreParticles == null) return;

        var main = coreParticles.main;
        main.startColor = newColor;

        Debug.Log($"核心颜色已改为: {newColor}, 对象: {name}");
    }

    // 改变核心亮度
    public void ChangeCoreBrightness(float intensity)
    {
        EnsureInitialized();

        if (coreParticles == null) return;

        Renderer coreRenderer = coreParticles.GetComponent<Renderer>();
        if (coreRenderer != null && coreRenderer.material != null)
        {
            // 获取当前颜色
            var main = coreParticles.main;
            Color currentColor = main.startColor.color;

            // 设置自发光颜色
            coreRenderer.material.SetColor("_EmissionColor", currentColor * intensity);

            // 确保自发光启用
            coreRenderer.material.EnableKeyword("_EMISSION");

            // 为了确保修改生效，标记材质为已更改
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(coreRenderer.material);
#endif
        }
    }

    // 设置粒子系统整体强度
    public void SetIntensity(float intensity)
    {
        EnsureInitialized();

        // 调整发射率
        if (bodyParticles != null)
        {
            var emission = bodyParticles.emission;
            emission.rateOverTime = normalEmissionRate * Mathf.Clamp(intensity, 0.1f, 5f);
        }

        // 调整核心亮度
        ChangeCoreBrightness(intensity);
    }

    // 获取当前灵魂是否可见
    public bool IsSoulVisible()
    {
        bool bodyVisible = bodyParticles != null && bodyParticles.gameObject.activeSelf && bodyParticles.isPlaying;
        bool coreVisible = coreParticles != null && coreParticles.gameObject.activeSelf && coreParticles.isPlaying;

        return bodyVisible || coreVisible;
    }

    // 获取灵魂当前位置
    public Vector3 GetSoulPosition()
    {
        if (bodyParticles != null)
        {
            return bodyParticles.transform.position;
        }
        return transform.position;
    }

    // 重置灵魂状态
    public void ResetSoul()
    {
        CancelInvoke();
        ReturnToNormalImmediate();

        if (bodyParticles != null)
        {
            bodyParticles.Clear();
        }
        if (coreParticles != null)
        {
            coreParticles.Clear();
        }
    }

    // 用于调试：在Scene视图中显示粒子系统位置
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (bodyParticles != null)
        {
            Gizmos.DrawWireSphere(bodyParticles.transform.position, 0.5f);
        }
        else if (coreParticles != null)
        {
            Gizmos.DrawWireSphere(coreParticles.transform.position, 0.3f);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    // 清理：取消所有Invoke调用
    void OnDestroy()
    {
        CancelInvoke();
    }

    void OnValidate()
    {
        // 在编辑器模式中确保引用设置正确
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (bodyParticles == null || coreParticles == null)
            {
                ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>(true);

                // 尝试自动分配
                if (allParticles.Length > 0 && bodyParticles == null)
                {
                    bodyParticles = allParticles[0];
                    Debug.Log($"已自动分配bodyParticles: {bodyParticles.name}");
                }

                if (allParticles.Length > 1 && coreParticles == null)
                {
                    coreParticles = allParticles[1];
                    Debug.Log($"已自动分配coreParticles: {coreParticles.name}");
                }
            }
        }
#endif
    }
}