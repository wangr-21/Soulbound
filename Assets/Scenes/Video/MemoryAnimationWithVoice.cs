using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using TMPro;

public class MemoryAnimationWithVoice : MonoBehaviour
{
    [Header("===== 图片UI =====")]
    public RawImage[] memoryImgs; // Img_01~Img_07（主图）

    [Header("===== 片段2分帧图片（单独赋值） =====")]
    public Texture2D[] memory02Frames; // 片段2的3张分帧图（拖入即可）

    [Header("===== 特效UI =====")]
    public RawImage flashWhite;
    public RawImage flashBlack;
    public RawImage spiritLight;
    public RawImage crack;

    [Header("===== 字幕UI =====")]
    public TMP_Text subtitleText;
    [Tooltip("打字机音效时长（秒），需和裁剪后的音效一致")]
    public float typewriterSoundLength = 0.1f;

    [Header("===== 声音素材 =====")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    public AudioClip typewriterKey;
    public AudioClip voice_07;
    public AudioClip soldierShout;
    public AudioClip spiritRoar;
    public AudioClip breath;
    public AudioClip wind;
    public AudioClip drawSword;

    [Header("===== 音量配置 - 特效声音单独加大 =====")]
    [Range(0f, 1f)]
    public float bgmBaseVolume = 0.7f; // BGM音量，避免盖过人声
    [Range(0f, 1f)]
    public float typewriterVolume = 0.4f; // 打字机音量适中
    [Range(0f, 1f)]
    public float voice07Volume = 0.9f; // 最后人声，稍高但不最大

    [Header("===== 特效声音（单独加大） =====")]
    [Range(1f, 3f)]
    public float effectVolumeMultiplier = 2.0f; // 特效声音乘数（可调1-3倍）
    [Range(0f, 1f)]
    public float soldierShoutVolume = 0.9f; // 基础音量
    [Range(0f, 1f)]
    public float spiritRoarVolume = 0.9f; // 基础音量
    [Range(0f, 1f)]
    public float breathVolume = 1f; // 基础音量
    [Range(0f, 1f)]
    public float windVolume = 1f; // 基础音量
    [Range(0f, 1f)]
    public float drawSwordVolume = 1f; // 基础音量

    [Header("===== 动画配置 =====")]
    [Tooltip("第7张图BGM淡出时长（秒）")]
    public float bgmFadeOutDuration = 2f;
    [Tooltip("片段间过渡时长（秒），控制流畅度，0=无缝")]
    public float transitionSmoothTime = 0.1f; // 过渡时长，建议0.1-0.2
    [Tooltip("第三张图灵体光效最大透明度（0-1），调大更明显")]
    public float spiritLightMaxAlpha = 1.0f; // 原0.8，增大为1.0
    [Tooltip("第一张→第二张闪白特效时长（秒）")]
    public float flashWhiteDuration = 0.08f; // 闪白持续时间

    [Header("===== 调试选项 =====")]
    public bool enableDebugLog = true; // 是否启用调试日志
    public bool testAllSoundsOnStart = false; // 开始时测试所有声音

    public string[] subtitleContents = new string[]
    {
        "我最开心的一天，是国王将守护国家的使命托付于我。那一刻，我感到前所未有的荣耀，也在心中立下誓言——此生追随陛下，至死不渝！",
        "当国王宣布采纳邻国的建议、让我们协助外来的帮手治理罪恶时，尽管心存疑惑，我仍毫不犹豫地选择服从。",
        "但…我没想到，这群帮手是一群怪物！他…他们掠夺百姓的灵魂，将他们变成一具具傀儡！",
        "站在遍地无魂的躯体之间，听着百姓的哀嚎，我第一次对自己誓死效忠的国王产生了动摇与恐惧。",
        "那天我如常立于哨所，却面对一个已然陌生的国家，心中只剩下无法回答的疑问与迷惘。",
        "当我明白活下去只会沦为怪物或傀儡时，便抚摸这把曾象征荣耀的剑，将它刺入自己的身体，用死亡守住最后的尊严。"
    };

    private AudioSource typewriterSource; // 专门播放打字机音效的AudioSource

    void Start()
    {
        // 初始化打字机AudioSource
        typewriterSource = gameObject.AddComponent<AudioSource>();
        typewriterSource.clip = typewriterKey;
        typewriterSource.volume = typewriterVolume;

        HideAllUI();
        // 初始化BGM
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.volume = bgmBaseVolume;
            bgmSource.loop = true;
            bgmSource.Play();
            if (enableDebugLog) Debug.Log($"BGM开始播放：{bgmClip.name}，音量：{bgmBaseVolume}");
        }
        else
        {
            Debug.LogError("BGM未赋值！请在Inspector面板拖入bgmSource和bgmClip");
        }

        // 测试所有声音
        if (testAllSoundsOnStart)
        {
            StartCoroutine(TestAllSounds());
        }
        else
        {
            StartCoroutine(PlayFullAnimation());
        }
    }

    IEnumerator TestAllSounds()
    {
        Debug.Log("=== 开始测试所有声音 ===");

        // 测试打字机
        TestSound("打字机", typewriterKey, typewriterVolume, false);
        yield return new WaitForSeconds(0.5f);

        // 测试特效声音
        TestSound("士兵呐喊", soldierShout, soldierShoutVolume, true);
        yield return new WaitForSeconds(1f);

        TestSound("灵体咆哮", spiritRoar, spiritRoarVolume, true);
        yield return new WaitForSeconds(1f);

        TestSound("呼吸声", breath, breathVolume, true);
        yield return new WaitForSeconds(1f);

        TestSound("风声", wind, windVolume, true);
        yield return new WaitForSeconds(1f);

        TestSound("拔剑声", drawSword, drawSwordVolume, true);
        yield return new WaitForSeconds(1f);

        // 测试人声
        TestSound("最后人声", voice_07, voice07Volume, false);
        yield return new WaitForSeconds(2f);

        Debug.Log("=== 声音测试完成，开始动画 ===");
        StartCoroutine(PlayFullAnimation());
    }

    void HideAllUI()
    {
        foreach (var img in memoryImgs)
        {
            if (img != null)
            {
                img.enabled = false;
                img.color = Color.white;
                img.rectTransform.localScale = Vector3.one;
                img.rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        if (flashWhite != null) flashWhite.enabled = false;
        if (flashBlack != null) flashBlack.enabled = false;
        if (spiritLight != null) spiritLight.enabled = false;
        if (crack != null) crack.enabled = false;
        if (subtitleText != null)
        {
            subtitleText.enabled = false;
            subtitleText.text = "";
        }
    }

    /// <summary>
    /// 播放音效 - 特效声音可单独加大
    /// </summary>
    void PlaySoundWithMultiplier(AudioClip clip, float baseVolume, string soundName, bool isEffectSound = false)
    {
        if (clip != null)
        {
            float finalVolume = baseVolume;

            // 如果是特效声音，应用乘数
            if (isEffectSound)
            {
                finalVolume = baseVolume * effectVolumeMultiplier;
                // 确保不超过1.0
                finalVolume = Mathf.Clamp01(finalVolume);
            }

            AudioSource.PlayClipAtPoint(clip, Vector3.zero, finalVolume);

            if (enableDebugLog)
            {
                if (isEffectSound)
                {
                    Debug.Log($"播放特效音效【{soundName}】：音量={finalVolume:F2} (基础={baseVolume} × {effectVolumeMultiplier:F1}倍)");
                }
                else
                {
                    Debug.Log($"播放音效【{soundName}】：音量={finalVolume:F2}");
                }
            }
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"音效【{soundName}】未赋值！");
        }
    }

    /// <summary>
    /// 测试声音
    /// </summary>
    void TestSound(string name, AudioClip clip, float volume, bool isEffectSound)
    {
        PlaySoundWithMultiplier(clip, volume, name, isEffectSound);
    }

    /// <summary>
    /// 打字机字幕：逐字+精准匹配音效
    /// </summary>
    IEnumerator PlayTypewriterSubtitle(string content)
    {
        if (subtitleText == null || string.IsNullOrEmpty(content)) yield break;

        subtitleText.enabled = true;
        subtitleText.text = "";
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < content.Length; i++)
        {
            // 播放打字机音效（非特效声音，不应用乘数）
            if (typewriterKey != null)
            {
                PlaySoundWithMultiplier(typewriterKey, typewriterVolume, "打字机", false);
            }

            sb.Append(content[i]);
            subtitleText.text = sb.ToString();
            yield return new WaitForSeconds(typewriterSoundLength);
        }
    }

    /// <summary>
    /// BGM淡出效果
    /// </summary>
    IEnumerator FadeOutBGM(float duration)
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0, timer / duration);
            yield return null;
        }

        bgmSource.volume = 0;
        bgmSource.Stop();
        if (enableDebugLog) Debug.Log("BGM淡出完成");
    }

    /// <summary>
    /// 转场闪黑（无缝衔接版）
    /// </summary>
    IEnumerator PlayFlashBlack()
    {
        if (flashBlack == null) yield break;
        flashBlack.enabled = true;
        flashBlack.color = new Color(0, 0, 0, 0);

        float timer = 0;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            float alpha = timer < 0.15f ? timer / 0.15f : 1 - (timer - 0.15f) / 0.15f;
            flashBlack.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        flashBlack.enabled = false;
    }

    /// <summary>
    /// 转场渐亮（无缝衔接版）
    /// </summary>
    IEnumerator PlayFadeInWhite()
    {
        if (flashWhite == null) yield break;
        flashWhite.enabled = true;
        flashWhite.color = new Color(1, 1, 1, 1);

        float timer = 0;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float alpha = 1 - timer / 0.5f;
            flashWhite.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        flashWhite.enabled = false;
    }

    /// <summary>
    /// 瞬间闪白特效（用于第一张→第二张转场）
    /// </summary>
    IEnumerator PlayInstantFlashWhite()
    {
        if (flashWhite == null) yield break;
        flashWhite.enabled = true;
        flashWhite.color = new Color(1, 1, 1, 1); // 纯白不透明
        yield return new WaitForSeconds(flashWhiteDuration); // 闪白时长可在Inspector调整
        flashWhite.enabled = false;
    }

    IEnumerator PlayFullAnimation()
    {
        // 显示调试信息
        if (enableDebugLog)
        {
            Debug.Log("=== 动画开始播放 ===");
            Debug.Log($"音量设置：打字机={typewriterVolume}, 人声={voice07Volume}");
            Debug.Log($"特效声音乘数：{effectVolumeMultiplier:F1}倍");
            Debug.Log($"特效基础音量：士兵={soldierShoutVolume}, 咆哮={spiritRoarVolume}, 呼吸={breathVolume}, 风={windVolume}, 拔剑={drawSwordVolume}");
        }

        // 0秒：初始黑屏（BGM已播放）
        yield return new WaitForSeconds(1f);

        // ===================== 片段1：荣耀时刻 =====================
        if (memoryImgs[0] != null) memoryImgs[0].enabled = true;
        // 图片放大特效
        float timer = 0;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.1f, timer / 0.5f);
            if (memoryImgs[0] != null) memoryImgs[0].rectTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        // 字幕播放（无额外等待，播完直接过渡）
        if (subtitleContents.Length > 0)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[0]));
        }
        // 第一段→第二段：隐藏当前图 + 闪白特效 + 短过渡
        if (memoryImgs[0] != null) memoryImgs[0].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;
        yield return StartCoroutine(PlayInstantFlashWhite()); // 恢复闪白特效
        yield return new WaitForSeconds(transitionSmoothTime); // 仅0.1秒过渡，无空白

        // ===================== 片段2：责任 =====================
        if (memoryImgs[1] != null)
        {
            // 播放3张分帧图（无空白）
            for (int i = 0; i < 3; i++)
            {
                if (memory02Frames.Length > i && memory02Frames[i] != null)
                {
                    memoryImgs[1].texture = memory02Frames[i];
                    memoryImgs[1].enabled = true;
                    // 播放士兵呐喊（应用特效声音乘数）
                    if (i == 0)
                    {
                        PlaySoundWithMultiplier(soldierShout, soldierShoutVolume, "士兵呐喊", true);
                    }
                    yield return new WaitForSeconds(0.5f);
                    memoryImgs[1].enabled = false;
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            // 显示主图
            memoryImgs[1].enabled = true;
        }
        // 字幕播放
        if (subtitleContents.Length > 1)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[1]));
        }
        // 转场闪黑（和隐藏图片同步，无空白）
        yield return StartCoroutine(PlayFlashBlack());
        if (memoryImgs[1] != null) memoryImgs[1].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // ===================== 片段3：灾难 =====================
        if (memoryImgs[2] != null) memoryImgs[2].enabled = true;
        if (spiritLight != null) spiritLight.enabled = true;
        // 灵体光效（增大透明度，可在Inspector调整）
        timer = 0;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (spiritLight != null)
            {
                // 最大透明度改为可配置的spiritLightMaxAlpha（默认1.0）
                float lightAlpha = Mathf.PingPong(timer * 3, spiritLightMaxAlpha);
                spiritLight.color = new Color(1, 1, 1, lightAlpha);
            }
            if (memoryImgs[2] != null)
            {
                float imgAlpha = Mathf.Lerp(1f, 0.7f, timer / 1f);
                memoryImgs[2].color = new Color(1, 1, 1, imgAlpha);
            }
            yield return null;
        }
        // 播放灵体咆哮（应用特效声音乘数）
        PlaySoundWithMultiplier(spiritRoar, spiritRoarVolume, "灵体咆哮", true);
        // 字幕播放
        if (subtitleContents.Length > 2)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[2]));
        }
        // 无缝隐藏
        if (memoryImgs[2] != null) memoryImgs[2].enabled = false;
        if (spiritLight != null) spiritLight.enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;
        yield return new WaitForSeconds(transitionSmoothTime);

        // ===================== 片段4：绝望 =====================
        if (memoryImgs[3] != null) memoryImgs[3].enabled = true;
        if (crack != null) crack.enabled = true;
        // 抖动特效
        timer = 0;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float shakeX = Mathf.Sin(timer * 5) * 3f;
            if (memoryImgs[3] != null)
            {
                memoryImgs[3].rectTransform.anchoredPosition = new Vector2(shakeX, 0);
            }
            yield return null;
        }
        // 播放呼吸声（应用特效声音乘数）
        PlaySoundWithMultiplier(breath, breathVolume, "呼吸声", true);
        // 字幕播放
        if (subtitleContents.Length > 3)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[3]));
        }
        // 转场渐亮（同步隐藏，无空白）
        yield return StartCoroutine(PlayFadeInWhite());
        if (memoryImgs[3] != null) memoryImgs[3].enabled = false;
        if (crack != null) crack.enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // ===================== 片段5：坚守 =====================
        if (memoryImgs[4] != null) memoryImgs[4].enabled = true;
        // 淡入特效
        timer = 0;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (memoryImgs[4] != null)
            {
                float imgAlpha = Mathf.Lerp(0.7f, 1f, timer / 1f);
                memoryImgs[4].color = new Color(1, 1, 1, imgAlpha);
            }
            yield return null;
        }
        // 播放风声（应用特效声音乘数）
        PlaySoundWithMultiplier(wind, windVolume, "风声", true);
        // 字幕播放
        if (subtitleContents.Length > 4)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[4]));
        }
        // 无缝隐藏
        if (memoryImgs[4] != null) memoryImgs[4].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;
        yield return new WaitForSeconds(transitionSmoothTime);

        // ===================== 片段6：决心 =====================
        if (memoryImgs[5] != null) memoryImgs[5].enabled = true;
        // 放大特效
        timer = 0;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.3f, timer / 1f);
            if (memoryImgs[5] != null)
            {
                memoryImgs[5].rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
            yield return null;
        }
        // 播放拔剑声（应用特效声音乘数）
        PlaySoundWithMultiplier(drawSword, drawSwordVolume, "拔剑声", true);
        // 字幕播放
        if (subtitleContents.Length > 5)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[5]));
        }
        // 无缝隐藏
        if (memoryImgs[5] != null) memoryImgs[5].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;
        yield return new WaitForSeconds(transitionSmoothTime);

        // ===================== 片段7：结尾 =====================
        if (memoryImgs[6] != null) memoryImgs[6].enabled = true;
        // BGM淡出
        StartCoroutine(FadeOutBGM(bgmFadeOutDuration));
        // 播放最后人声（不应用特效乘数）
        PlaySoundWithMultiplier(voice_07, voice07Volume, "最后人声", false);
        if (subtitleText != null)
        {
            subtitleText.enabled = true;
            subtitleText.text = "我的国王啊！快醒醒吧…";
        }
        // 等待淡出完成
        yield return new WaitForSeconds(bgmFadeOutDuration + 1f);
        if (memoryImgs[6] != null) memoryImgs[6].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        if (enableDebugLog) Debug.Log("动画播放完成！");
    }

    /// <summary>
    /// 动态调整特效声音乘数
    /// </summary>
    public void SetEffectVolumeMultiplier(float multiplier)
    {
        effectVolumeMultiplier = Mathf.Clamp(multiplier, 0.5f, 5f);
        if (enableDebugLog) Debug.Log($"特效声音乘数调整为: {effectVolumeMultiplier:F1}倍");
    }

    /// <summary>
    /// 动态调整BGM音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmBaseVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmBaseVolume;
            if (enableDebugLog) Debug.Log($"BGM音量调整为: {bgmBaseVolume}");
        }
    }

    /// <summary>
    /// 重新播放动画
    /// </summary>
    public void RestartAnimation()
    {
        StopAllCoroutines();
        HideAllUI();

        // 重置BGM
        if (bgmSource != null)
        {
            bgmSource.volume = bgmBaseVolume;
            bgmSource.Play();
        }

        StartCoroutine(PlayFullAnimation());
    }
}