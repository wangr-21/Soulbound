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
    public AudioClip typewriterKey; // 打字机单按键音效
    public AudioClip voice_07;      // 最后一张图人声
    public AudioClip soldierShout;  // 场景音效
    public AudioClip spiritRoar;
    public AudioClip breath;
    public AudioClip wind;
    public AudioClip drawSword;

    [Header("===== 配置参数 =====")]
    public float bgmBaseVolume = 0.3f;
    public string[] subtitleContents = new string[]
    {
        "这柄权杖，代表王国的信任",
        "我将以生命守护它",
        "你的王国，终将化为灰烬",
        "他们……都不在了？",
        "至少，还有这座塔",
        "我会夺回一切"
    };

    private AudioSource typewriterSource; // 专门播放打字机音效的AudioSource

    void Start()
    {
        // 初始化打字机AudioSource
        typewriterSource = gameObject.AddComponent<AudioSource>();
        typewriterSource.clip = typewriterKey;
        typewriterSource.volume = 0.5f;

        HideAllUI();
        bgmSource.volume = bgmBaseVolume;
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

    IEnumerator PlayFullAnimation()
    {
        // 0秒：初始黑屏
        yield return new WaitForSeconds(1f);

        // 1秒：片段1 - 荣耀时刻
        if (memoryImgs[0] != null) memoryImgs[0].enabled = true;
        bgmSource.clip = Resources.Load<AudioClip>("Audio/BGM_Calm");
        bgmSource.Play();

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
        bgmSource.clip = Resources.Load<AudioClip>("Audio/BGM_Noise");
        bgmSource.Play();
        bgmSource.volume = bgmBaseVolume + 0.1f;

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

        if (breath != null) AudioSource.PlayClipAtPoint(breath, Vector3.zero, 0.6f);
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
        bgmSource.clip = Resources.Load<AudioClip>("Audio/BGM_Piano");
        bgmSource.Play();
        bgmSource.volume = bgmBaseVolume;

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

        if (drawSword != null) AudioSource.PlayClipAtPoint(drawSword, Vector3.zero, 1f);
        if (subtitleContents.Length > 5)
        {
            yield return StartCoroutine(PlayTypewriterSubtitle(subtitleContents[5]));
        }
        yield return new WaitForSeconds(1f);
        if (memoryImgs[5] != null) memoryImgs[5].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;

        // 19.5秒：片段7 - 结尾（黑色图）
        if (memoryImgs[6] != null) memoryImgs[6].enabled = true;
        bgmSource.volume = bgmBaseVolume * 0.3f;

        if (voice_07 != null) AudioSource.PlayClipAtPoint(voice_07, Vector3.zero, 1f);
        if (subtitleText != null)
        {
            subtitleText.enabled = true;
            subtitleText.text = "这就是我的宿命吗？";
        }

        yield return new WaitForSeconds(3f);
        if (memoryImgs[6] != null) memoryImgs[6].enabled = false;
        if (subtitleText != null) subtitleText.enabled = false;
        bgmSource.Stop();

        Debug.Log("动画播放完成！");
        // 可选：返回主菜单
        // SceneManager.LoadScene("MainMenu");
    }
}