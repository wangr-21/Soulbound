using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthSystem : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth = 100f;
    public float currentHealth = 0f;

    [Header("UI元素")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    [Header("死亡效果")]
    public GameObject deathEffect;
    public AudioClip deathSound;

    [Header("受伤效果")]
    public Material hurtMaterial;
    public float hurtFlashTime = 0.1f;
    private Material originalMaterial;
    private Renderer objectRenderer;

    [Header("免疫时间")]
    public float invincibilityTime = 0.5f;
    private float lastDamageTime = 0f;
    private bool isInvincible = false;

    [Header("调试")]
    public bool showDebugInfo = false;

    public bool IsAlive => currentHealth > 0;

    void Start()
    {
        currentHealth = maxHealth;

        // 获取渲染器和材质
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        UpdateHealthUI();
    }

    void Update()
    {
        // 更新无敌状态
        if (isInvincible && Time.time - lastDamageTime > invincibilityTime)
        {
            isInvincible = false;
        }
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        // 如果处于无敌状态，忽略伤害
        if (isInvincible) return;

        currentHealth -= damage;
        lastDamageTime = Time.time;
        isInvincible = true;

        UpdateHealthUI();

        // 受伤效果
        StartCoroutine(FlashHurtEffect());

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} 受到 {damage} 点伤害，剩余生命值: {currentHealth}");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    /// <param name="amount">恢复量</param>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} 恢复 {amount} 点生命值，当前生命值: {currentHealth}");
        }
    }

    /// <summary>
    /// 死亡
    /// </summary>
    public void Die()
    {
        currentHealth = 0;

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} 死亡！");
        }

        // 播放死亡效果
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // 禁用对象
        gameObject.SetActive(false);

        // 可以在这里触发死亡事件
        // OnDeath?.Invoke();
    }

    /// <summary>
    /// 重置生命值
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        isInvincible = false;

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} 生命值已重置: {currentHealth}");
        }
    }

    /// <summary>
    /// 获取生命值百分比
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// 更新生命值UI
    /// </summary>
    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = GetHealthPercentage();
        }

        if (healthText != null)
        {
            healthText.text = $"生命值: {Mathf.Ceil(currentHealth)}/{maxHealth}";
        }
    }

    /// <summary>
    /// 受伤闪烁效果
    /// </summary>
    private System.Collections.IEnumerator FlashHurtEffect()
    {
        if (objectRenderer == null || hurtMaterial == null) yield break;

        objectRenderer.material = hurtMaterial;
        yield return new WaitForSeconds(hurtFlashTime);

        if (originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }
}