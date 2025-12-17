using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using TMPro;

public class MemoryAnimationWithVoice : MonoBehaviour
{
    [Header("===== 图片UI =====")]
    public RawImage[] memoryImgs; // Img_01~Img_07

    [Header("===== 特效UI =====")]
    public RawImage flashWhite;
    public RawImage flashBlack;
    public RawImage spiritLight;
    public RawImage crack;

    [Header("===== 字幕UI =====")]
    public TMP_Text subtitleText;
    [Tooltip("打字机音效时长（秒），需和裁剪后的音效一致")]
    public float typewriterSoundLength = 0.1f;
    [Tooltip("打字速度（秒/字），必须和音效时长一致")]
    public float typewriterSpeed = 0.1f;

    [Header("===== 声音素材 =====")]
    public AudioSource bgmSource;
    public AudioClip bgmClip; // 新增：BGM音频clip，手动拖入
    public AudioClip typewriterKey; // 打字机单按键音效
    public AudioClip voice_07;      // 最后一张图人声
    public AudioClip soldierShout;  // 场景音效
    public AudioClip spiritRoar;
    public AudioClip breath;
    public AudioClip wind;
    public AudioClip drawSword;

    [Header("===== 配置参数 =====")]
    public float bgmBaseVolume = 1f;
    [Tooltip("第7张图BGM淡出时长（秒）")]
    public float bgmFadeOutDuration = 2f; // BGM逐渐变小的时长，可调整
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
        typewriterSource.volume = 0.5f;

        HideAllUI();
        // 初始化BGM（直接用手动拖入的clip，删掉Resources.Load）
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.volume = bgmBaseVolume;
            bgmSource.loop = true; // 开启循环，确保BGM一直播
            bgmSource.Play(); // 初始黑屏阶段就开始播放
            Debug.Log("BGM开始播放：" + bgmClip.name);
        }
        else
        {
            Debug.LogError("BGM未赋值！请在Inspector面板拖入bgmSource和bgmClip");
        }

        StartCoroutine(PlayFullAnimation());
    }

    void HideAllUI()
    {
        foreach (var img in memoryImgs) if (img != null) img.enabled = false;
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
            // 播放打字机音效（精准同步）
            if (typewriterKey != null)
            {
                typewriterSource.PlayOneShot(typewriterKey, 0.5f);
            }

            // 显示1个字
            sb.Append(content[i]);
            subtitleText.text = sb.ToString();

            // 等待和音效时长一致的时间
            yield return new WaitForSeconds(typewriterSoundLength);
        }
    }

    /// <summary>
    /// BGM淡出效果：音量从当前值逐渐降到0
    /// </summary>
    /// <param name="duration">淡出时长</param>
    IEnumerator FadeOutBGM(float duration)
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 线性插值：从startVolume降到0
            bgmSource.volume = Mathf.Lerp(startVolume, 0, timer / duration);
            yield return null;
        }

        // 淡出完成后，强制音量为0并停止BGM
        bgmSource.volume = 0;
        bgmSource.Stop();
    }

    IEnumerator PlayFullAnimation()
    {
        // 0秒：初始黑屏（BGM已在Start中播放）
        yield return new WaitForSeconds(1f);

        // 1秒：片段1 - 荣耀时刻
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

        // 打字机字幕
        if (subtitleContents.Length > 0)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[0]));
        }
        yield return new WaitForSeconds(1f);
        if (memoryImgs[0] != null) memoryImgs[0].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // 3.5秒：转场1 - 闪白
        if (flashWhite != null)
        {
            flashWhite.enabled = true;
            flashWhite.color = new Color(1, 1, 1, 0);
            timer = 0;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                float alpha = timer < 0.1f ? timer / 0.1f : 1 - (timer - 0.1f) / 0.1f;
                flashWhite.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
            flashWhite.enabled = false;
        }

        // 3.7秒：片段2 - 责任
        for (int i = 0; i < 3; i++)
        {
            if (memoryImgs[1] != null)
            {
                memoryImgs[1].texture = Resources.Load<Texture2D>($"Textures/Memory_02_Part{i + 1}");
                memoryImgs[1].enabled = true;
                if (i == 0 && soldierShout != null)
                {
                    AudioSource.PlayClipAtPoint(soldierShout, Vector3.zero, 0.7f);
                }
                yield return new WaitForSeconds(0.5f);
                memoryImgs[1].enabled = false;
            }
        }

        if (memoryImgs[1] != null)
        {
            memoryImgs[1].texture = Resources.Load<Texture2D>("Textures/Memory_02");
            memoryImgs[1].enabled = true;
        }
        if (subtitleContents.Length > 1)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[1]));
        }
        yield return new WaitForSeconds(1f);
        if (memoryImgs[1] != null) memoryImgs[1].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // 6.2秒：转场2 - 闪黑
        if (flashBlack != null)
        {
            flashBlack.enabled = true;
            flashBlack.color = new Color(0, 0, 0, 0);
            timer = 0;
            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                float alpha = timer < 0.15f ? timer / 0.15f : 1 - (timer - 0.15f) / 0.15f;
                flashBlack.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            flashBlack.enabled = false;
        }

        // 6.5秒：片段3 - 灾难
        if (memoryImgs[2] != null) memoryImgs[2].enabled = true;
        if (spiritLight != null) spiritLight.enabled = true;

        timer = 0;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            if (spiritLight != null)
            {
                float lightAlpha = Mathf.PingPong(timer * 3, 0.8f);
                spiritLight.color = new Color(1, 1, 1, lightAlpha);
            }
            if (memoryImgs[2] != null)
            {
                float imgAlpha = Mathf.Lerp(1f, 0.7f, timer / 1f);
                memoryImgs[2].color = new Color(1, 1, 1, imgAlpha);
            }
            yield return null;
        }

        if (spiritRoar != null) AudioSource.PlayClipAtPoint(spiritRoar, Vector3.zero, 0.9f);
        if (subtitleContents.Length > 2)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[2]));
        }
        yield return new WaitForSeconds(1.5f);
        if (memoryImgs[2] != null) memoryImgs[2].enabled = false;
        if (spiritLight != null) spiritLight.enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // 10秒：片段4 - 绝望
        if (memoryImgs[3] != null) memoryImgs[3].enabled = true;
        if (crack != null) crack.enabled = true;

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

        if (breath != null) AudioSource.PlayClipAtPoint(breath, Vector3.zero, 1.6f);
        if (subtitleContents.Length > 3)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[3]));
        }
        yield return new WaitForSeconds(1f);
        if (memoryImgs[3] != null) memoryImgs[3].enabled = false;
        if (crack != null) crack.enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // 12.5秒：转场3 - 渐亮
        if (flashWhite != null)
        {
            flashWhite.enabled = true;
            flashWhite.color = new Color(1, 1, 1, 1);
            timer = 0;
            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                float alpha = 1 - timer / 0.5f;
                flashWhite.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
            flashWhite.enabled = false;
        }

        // 13秒：片段5 - 坚守
        if (memoryImgs[4] != null) memoryImgs[4].enabled = true;

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

        if (wind != null) AudioSource.PlayClipAtPoint(wind, Vector3.zero, 0.5f);
        if (subtitleContents.Length > 4)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[4]));
        }
        yield return new WaitForSeconds(1.5f);
        if (memoryImgs[4] != null) memoryImgs[4].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // 16.5秒：片段6 - 决心
        if (memoryImgs[5] != null) memoryImgs[5].enabled = true;

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

        if (drawSword != null) AudioSource.PlayClipAtPoint(drawSword, Vector3.zero, 2f);
        if (subtitleContents.Length > 5)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[5]));
        }
        yield return new WaitForSeconds(1f);
        if (memoryImgs[5] != null) memoryImgs[5].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // 19.5秒：片段7 - 结尾（黑色图）
        if (memoryImgs[6] != null) memoryImgs[6].enabled = true;

        // 启动BGM淡出协程（逐渐变小）
        StartCoroutine(FadeOutBGM(bgmFadeOutDuration));

        if (voice_07 != null) AudioSource.PlayClipAtPoint(voice_07, Vector3.zero, 1f);
        if (subtitleText != null)
        {
            subtitleText.enabled = true;
            subtitleText.text = "我的国王啊！快醒醒吧…";
        }

        // 等待淡出完成+人声播放完毕（总时长=淡出时长+1秒缓冲）
        yield return new WaitForSeconds(bgmFadeOutDuration + 1f);
        if (memoryImgs[6] != null) memoryImgs[6].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        Debug.Log("动画播放完成！");
        // 可选：返回主菜单
        // SceneManager.LoadScene("MainMenu");
    }
}