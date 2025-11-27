using UnityEngine;

public class SmokeTrailController : MonoBehaviour
{
    private ParticleSystem particleSystem;
    private Rigidbody playerRigidbody;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;

    [Header("呼吸效果")]
    public float breathIntensity = 0.1f;
    public float breathSpeed = 1f;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        playerRigidbody = GetComponentInParent<Rigidbody>();
        velocityModule = particleSystem.velocityOverLifetime;
    }

    void Update()
    {
        if (playerRigidbody != null)
        {
            // 根据玩家速度调整粒子行为
            float speed = playerRigidbody.velocity.magnitude;

            // 移动时增加粒子速度，创造拉丝效果
            if (speed > 0.1f)
            {
                velocityModule.x = 0.3f; // 增加横向流动
                particleSystem.startLifetime = 0.8f; // 缩短生命周期，更密集
            }
            else
            {
                velocityModule.x = 0.1f; // 平静状态
                particleSystem.startLifetime = 1.5f;
            }

            // 呼吸效果
            float breath = Mathf.Sin(Time.time * breathSpeed) * breathIntensity;
            particleSystem.startSize = 0.3f + breath;
        }
    }
}