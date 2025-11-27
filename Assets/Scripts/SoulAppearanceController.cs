using UnityEngine;

public class SoulAppearanceController : MonoBehaviour
{
    [Header("粒子系统引用")]
    public ParticleSystem bodyParticles;
    public ParticleSystem coreParticles;

    [Header("动态效果参数")]
    public float normalEmissionRate = 30f;
    public float scaredEmissionRate = 60f;
    public float normalSize = 0.3f;
    public float scaredSize = 0.15f;

    private ParticleSystem.EmissionModule bodyEmission;
    private ParticleSystem.MainModule bodyMain;
    private ParticleSystem.MainModule coreMain;

    void Start()
    {
        bodyEmission = bodyParticles.emission;
        bodyMain = bodyParticles.main;
        coreMain = coreParticles.main;
    }

    void Update()
    {
        // 基础的呼吸效果
        // float breath = Mathf.Sin(Time.time * 1f) * 0.05f;
        //bodyMain.startSize = normalSize + breath;
    }

    // 受到惊吓时的收缩效果
    public void OnScared()
    {
        bodyMain.startSize = scaredSize;
        bodyEmission.rateOverTime = scaredEmissionRate;

        // 0.5秒后恢复
        Invoke("ReturnToNormal", 0.5f);
    }

    private void ReturnToNormal()
    {
        bodyMain.startSize = normalSize;
        bodyEmission.rateOverTime = normalEmissionRate;
    }

    // 改变核心颜色（用于游戏进度）
    public void ChangeCoreColor(Color newColor)
    {
        coreMain.startColor = newColor;
    }

    // 改变核心亮度
    public void ChangeCoreBrightness(float intensity)
    {
        var coreEmission = coreParticles.GetComponent<Renderer>().material;
        coreEmission.SetColor("_EmissionColor", coreMain.startColor.color * intensity);
    }
}