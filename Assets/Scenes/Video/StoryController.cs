using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class StoryController : MonoBehaviour
{
    [Header("UI 组件引用")]
    public Image bgImage;                // 背景图片
    public TextMeshProUGUI storyText;    // 剧情文本
    public TextMeshProUGUI skipTip;      // 跳过提示
    public AudioSource bgmAudioSource;   // 背景音乐播放器
    public Slider loadProgressBar;       // 加载进度条（可选）
    public TextMeshProUGUI progressText; // 进度百分比文本（可选）

    [Header("剧情配置")]
    public StoryData storyData;          // 剧情数据
    public float imageFadeTime = 0.5f;   // 图片淡入淡出时长（秒）
    public string mainSceneName = "GameMain"; // 主游戏场景名称（必须和Build中一致）
    public float bgmFadeDuration = 1f;   // 背景音乐淡入淡出时长（秒）

    private int currentSegmentIndex = 0; // 当前剧情段索引
    private Coroutine textCoroutine;     // 打字机协程
    private bool isTextFinished = false; // 文本是否打印完成
    private bool isSkipping = false;     // 是否正在跳过
    private bool isLongSkipping = false; // 是否正在长按跳过

    void Start()
    {
        // 初始化 UI 状态（隐藏加载进度条）
        if (loadProgressBar != null)
        {
            loadProgressBar.gameObject.SetActive(false);
            progressText.gameObject.SetActive(false);
        }

        // 初始化图片透明度（透明）
        storyText.text = "";
        bgImage.color = new Color(1, 1, 1, 0);

        // 播放背景音乐（淡入效果）
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = 0;
            bgmAudioSource.Play();
            StartCoroutine(FadeBgmVolume(0, bgmAudioSource.volume, bgmFadeDuration));
        }

        // 开始播放第一段剧情
        StartCoroutine(PlayStorySegment(currentSegmentIndex));
    }

    void Update()
    {
        // 右键单击：跳过当前剧情段
        if (Input.GetMouseButtonDown(1) && !isLongSkipping)
        {
            StartCoroutine(CheckLongPress());
        }

        // 右键松开：取消长按判断
        if (Input.GetMouseButtonUp(1))
        {
            isLongSkipping = false;
        }
    }

    // 播放单段剧情
    private IEnumerator PlayStorySegment(int index)
    {
        // 所有剧情播放完毕，加载主场景
        if (index >= storyData.storySegments.Length)
        {
            StartCoroutine(LoadMainScene());
            yield break;
        }

        // 重置状态
        isTextFinished = false;
        isSkipping = false;
        storyText.text = "";

        // 获取当前剧情段数据
        StorySegment currentSegment = storyData.storySegments[index];

        // 1. 背景图片淡入
        yield return StartCoroutine(FadeImage(bgImage, currentSegment.bgSprite, true));

        // 2. 打字机效果显示文本
        textCoroutine = StartCoroutine(TypeText(currentSegment.storyText, currentSegment.textSpeed));

        // 等待文本打印完成或被跳过
        while (!isTextFinished && !isSkipping)
            yield return null;

        // 3. 停留 0.5 秒，进入下一段
        yield return new WaitForSeconds(0.5f);
        currentSegmentIndex++;
        StartCoroutine(PlayStorySegment(currentSegmentIndex));
    }

    // 打字机效果
    private IEnumerator TypeText(string content, float speed)
    {
        char[] chars = content.ToCharArray();
        storyText.text = "";

        for (int i = 0; i < chars.Length; i++)
        {
            if (isSkipping || isLongSkipping)
                break; // 跳过则直接显示全部文本
            storyText.text += chars[i];
            yield return new WaitForSeconds(speed);
        }

        // 确保显示完整文本
        storyText.text = content;
        isTextFinished = true;
    }

    // 图片淡入/淡出效果
    private IEnumerator FadeImage(Image image, Sprite targetSprite, bool isFadeIn)
    {
        // 切换图片（有目标图片则赋值）
        if (targetSprite != null)
        {
            image.sprite = targetSprite;
            image.SetNativeSize(); // 适配图片原始大小
            image.preserveAspect = true; // 保持宽高比
        }

        float targetAlpha = isFadeIn ? 1 : 0; // 淡入到100%透明，淡出到0%
        float currentAlpha = image.color.a;
        float timer = 0;

        // 渐变过程
        while (timer < imageFadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(currentAlpha, targetAlpha, timer / imageFadeTime);
            image.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // 确保最终状态正确
        image.color = new Color(1, 1, 1, targetAlpha);
        if (!isFadeIn)
            image.sprite = null; // 淡出后清空图片，避免残留
    }

    // 跳过当前剧情段
    private void SkipCurrentSegment()
    {
        if (isSkipping || isLongSkipping)
            return;

        isSkipping = true;
        // 停止打字机协程，显示全部文本
        if (textCoroutine != null)
        {
            StopCoroutine(textCoroutine);
            storyText.text = storyData.storySegments[currentSegmentIndex].storyText;
        }
        isTextFinished = true;
    }

    // 检测右键长按（1秒跳过全部）
    private IEnumerator CheckLongPress()
    {
        float pressTime = 0;
        isLongSkipping = true;

        while (Input.GetMouseButton(1))
        {
            pressTime += Time.deltaTime;
            // 长按1秒，直接加载主场景
            if (pressTime >= 1f)
            {
                // 停止打字机协程
                if (textCoroutine != null)
                    StopCoroutine(textCoroutine);
                // 显示进度条，加载主场景
                StartCoroutine(LoadMainScene());
                yield break;
            }
            yield return null;
        }

        // 长按不足1秒，视为普通跳过当前段
        if (pressTime < 1f)
        {
            SkipCurrentSegment();
        }

        isLongSkipping = false;
    }

    // 加载主游戏场景
    private IEnumerator LoadMainScene()
    {
        // 背景音乐淡出停止
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            StartCoroutine(FadeBgmVolume(bgmAudioSource.volume, 0, bgmFadeDuration));
            yield return new WaitForSeconds(bgmFadeDuration);
            bgmAudioSource.Stop();
        }

        // 显示加载进度条（如果有）
        if (loadProgressBar != null)
        {
            loadProgressBar.gameObject.SetActive(true);
            progressText.gameObject.SetActive(true);
            loadProgressBar.value = 0;
            progressText.text = "0%";
        }

        // 异步加载主场景（不卡顿）
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainSceneName);
        asyncLoad.allowSceneActivation = false; // 先不激活，等进度100%

        // 实时更新加载进度
        while (!asyncLoad.isDone)
        {
            // Unity 加载进度到 0.9 即代表资源加载完成
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            // 更新进度条和文本
            if (loadProgressBar != null)
            {
                loadProgressBar.value = progress;
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }

            // 进度100%后，激活主场景
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // 背景音乐淡入淡出效果
    private IEnumerator FadeBgmVolume(float startVol, float targetVol, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVol, targetVol, timer / duration);
            yield return null;
        }
        bgmAudioSource.volume = targetVol;
    }
}