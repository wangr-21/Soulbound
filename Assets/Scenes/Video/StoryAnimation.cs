using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class AnimationStorySegment
{
    public Sprite storyImage;   // 剧情图片
    [TextArea(2, 5)]            // 文本区域
    public string storyText;    // 对应文字
    public AudioClip voiceOver; // 片段的语音
}

public class StoryAnimation : MonoBehaviour
{
    [Header("剧情资源")]
    public AnimationStorySegment[] storySegments;

    [Header("UI 引用")]
    public Image bgImage;               // 背景图片
    public TMP_Text subtitleText;       // 字幕文字
    public Button nextButton;           // 下一页按钮

    [Header("参数")]
    public float typeInterval = 0.05f;
    public bool autoPlay = false;
    public float autoPlayDelayAfterVoice = 1f;

    [Header("背景音乐设置")]
    public AudioClip backgroundMusic;   // 背景音乐文件
    [Range(0f, 1f)]
    public float bgmVolume = 0.3f;      // 背景音乐音量
    public bool stopBgmOnEnd = true;    // 剧情结束后是否停止BGM

    private AudioSource voiceSource;    // 语音音频源
    private AudioSource bgmSource;      // 背景音乐音频源
    private int currentIndex;
    private bool isTyping;
    private Coroutine typingCoroutine;

    void Awake()
    {
        // 初始化语音音频源
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.volume = 0.8f;

        // 初始化背景音乐音频源
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true; // BGM循环播放

        // 按钮始终激活（移除隐藏逻辑）
        nextButton.onClick.AddListener(OnNextButtonClick);
        nextButton.gameObject.SetActive(true);
    }

    void Start()
    {
        // 播放背景音乐（如果设置了）
        if (backgroundMusic != null)
        {
            bgmSource.clip = backgroundMusic;
            bgmSource.Play();
        }

        // 启动剧情
        if (storySegments.Length > 0)
            PlayStory(0);
        else
            Debug.LogWarning("未配置任何剧情片段！");
    }

    void PlayStory(int index)
    {
        // 剧情结束逻辑
        if (index >= storySegments.Length)
        {
            if (stopBgmOnEnd) bgmSource.Stop(); // 停止BGM
            nextButton.gameObject.SetActive(false); // 最后隐藏按钮
            return;
        }

        currentIndex = index;

        // 更新背景图（保留你的preserveAspect逻辑）
        if (bgImage != null && storySegments[index].storyImage != null)
        {
            bgImage.sprite = storySegments[index].storyImage;
            bgImage.preserveAspect = true;
        }

        // 重置文本并开始打字
        subtitleText.text = "";
        isTyping = true;

        // 停止上一段的打字协程
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // 启动新的打字协程
        typingCoroutine = StartCoroutine(TypeText(
            storySegments[index].storyText,
            storySegments[index].voiceOver
        ));
    }

    IEnumerator TypeText(string text, AudioClip voice)
    {
        // 播放当前段语音
        if (voice != null)
        {
            voiceSource.clip = voice;
            voiceSource.Play();
        }

        // 逐字打字逻辑
        for (int i = 0; i < text.Length; i++)
        {
            if (!isTyping) break; // 防止跳过打字后继续执行
            subtitleText.text += text[i];
            yield return new WaitForSeconds(typeInterval);
        }

        isTyping = false;

        // 自动播放下一段逻辑（保留原有）
        if (autoPlay && voice != null)
        {
            while (voiceSource.isPlaying)
                yield return null;

            yield return new WaitForSeconds(autoPlayDelayAfterVoice);
            PlayStory(currentIndex + 1);
        }
    }

    void OnNextButtonClick()
    {
        // 停止当前语音播放
        if (voiceSource.isPlaying)
            voiceSource.Stop();

        // 如果正在打字 → 直接显示完整文字
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        // 否则直接进入下一段
        PlayStory(currentIndex + 1);
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // 直接显示完整文本
        subtitleText.text = storySegments[currentIndex].storyText;
        isTyping = false;
    }

    void OnDestroy()
    {
        // 清理资源
        StopAllCoroutines();
        nextButton.onClick.RemoveListener(OnNextButtonClick);

        if (voiceSource != null) Destroy(voiceSource);
        if (bgmSource != null) Destroy(bgmSource);
    }

    // 外部调用接口（可选）：手动停止所有音频
    public void StopAllAudio()
    {
        voiceSource.Stop();
        bgmSource.Stop();
    }
}