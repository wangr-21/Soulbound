using UnityEngine;

public class Stone : MonoBehaviour
{
    public int stoneIndex; // 石头编号（0-4对应A-E）
    public ParticleSystem glowEffect; // 发光粒子效果
    public Material normalMat; // 普通材质
    public Material glowMat; // 发光材质
    private Renderer stoneRenderer;

    private void Awake()
    {
        stoneRenderer = GetComponent<Renderer>();
        // 初始禁用发光效果
        if (glowEffect != null)
            glowEffect.gameObject.SetActive(false);
    }

    // 激活发光效果
    public void ActivateGlow()
    {
        if (glowEffect != null)
        {
            glowEffect.gameObject.SetActive(true);
            glowEffect.Play();
        }
        else if (stoneRenderer != null && glowMat != null)
        {
            stoneRenderer.material = glowMat;
        }
    }

    // 关闭发光效果
    public void DeactivateGlow()
    {
        if (glowEffect != null)
        {
            glowEffect.Stop();
            glowEffect.gameObject.SetActive(false);
        }
        else if (stoneRenderer != null && normalMat != null)
        {
            stoneRenderer.material = normalMat;
        }
    }

    // 玩家踩上石头时调用
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StonePuzzleManager.Instance.OnPlayerStepOnStone(stoneIndex);
        }
    }
}