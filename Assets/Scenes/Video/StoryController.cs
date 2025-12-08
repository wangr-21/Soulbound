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

    [Header("剧情配置")]
    public StoryData storyData;          // 剧情数据
    public float imageFadeTime = 1.0f;   // 图片淡入淡出时长（秒）
    public string mainSceneName = "GameMain"; // 主游戏场景名称
    public float bgmFadeDuration = 1.5f; // 背景音乐淡入淡出时长（秒）

    [Header("文字框设置")]
    public float textBoxMarginLeft = 0.05f;   // 左边距5%
    public float textBoxMarginRight = 0.05f;  // 右边距5%
    public float textBoxMarginBottom = 0.1f;  // 底边距10%
    public float textBoxHeightPercent = 0.35f; // 高度占屏幕35%

    [Header("调试选项")]
    public bool debugMode = true;
    public bool forceTextVisible = true;

    private int currentSegmentIndex = 0; // 当前剧情段索引
    private Coroutine textCoroutine;     // 打字机协程
    private bool isTextFinished = false; // 文本是否打印完成
    private bool isSkipping = false;     // 是否正在跳过
    private bool isLongSkipping = false; // 是否正在长按跳过
    private bool isInitialized = false;  // 

    void Start()
    {
        if (debugMode)
        {
            Debug.Log("=== StoryController 启动 ===");
            Debug.Log($"字体组件原始设置 - 大小: {storyText?.fontSize}, 颜色: {storyText?.color}");
        }

        // 确保只初始化一次
        if (!isInitialized)
        {
            // 初始化所有UI组件
            InitializeAllComponents();
            isInitialized = true;
        }

        // 开始播放第一段剧情
        StartCoroutine(PlayStorySegment(currentSegmentIndex));
    }

    void InitializeAllComponents()
    {
        if (debugMode) Debug.Log("开始初始化所有组件...");

        // 1. 强制Canvas更新
        Canvas.ForceUpdateCanvases();

        // 2. 等待一帧确保组件加载完成
        StartCoroutine(DelayedInitialization());
    }

    IEnumerator DelayedInitialization()
    {
        yield return null; // 重要：等待一帧，让Unity完成组件初始化

        // 3. 初始化背景图片
        InitializeBackground();

        // 4. 初始化文本组件
        InitializeTextComponent();

        // 5. 初始化跳过提示
        InitializeSkipTip();

        // 6. 初始化音频
        InitializeAudio();

        // 7. 最终验证
        ValidateComponents();
    }

    void InitializeBackground()
    {
        if (bgImage == null)
        {
            Debug.LogError("错误: BgImage 未分配!");
            enabled = false;
            return;
        }

        RectTransform bgRect = bgImage.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgRect.pivot = new Vector2(0.5f, 0.5f);

        bgImage.type = Image.Type.Simple;
        bgImage.preserveAspect = true;
        bgImage.color = new Color(1, 1, 1, 0);

        if (debugMode) Debug.Log("背景图片初始化完成");
    }

    void InitializeTextComponent()
    {
        if (storyText == null)
        {
            Debug.LogError("错误: StoryText 未分配!");
            enabled = false;
            return;
        }

        // 关键修复：检查并修复CanvasRenderer
        CanvasRenderer textRenderer = storyText.GetComponent<CanvasRenderer>();
        if (textRenderer != null)
        {
            if (debugMode) Debug.Log($"CanvasRenderer状态 - Cull: {textRenderer.cullTransparentMesh}, Alpha: {textRenderer.GetAlpha()}");

            // 强制设置为可见状态
            textRenderer.cullTransparentMesh = false;
            textRenderer.SetAlpha(1f);
        }
        else
        {
            Debug.LogWarning("StoryText没有CanvasRenderer组件!");
        }

        // 只设置文字框位置和大小，不设置字体样式
        SetupTextArea();

        // 强制确保字体自动缩放关闭
        storyText.enableAutoSizing = false;

        // 强制文本重新渲染
        storyText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        if (forceTextVisible)
        {
            // 强制显示测试文本
            StartCoroutine(TestTextVisibility());
        }

        if (debugMode)
        {
            Debug.Log($"文本组件初始化完成");
            Debug.Log($"字体设置 - 大小: {storyText.fontSize}, 颜色: {storyText.color}");
            Debug.Log($"自动缩放状态: {storyText.enableAutoSizing}");
        }
    }

    void SetupTextArea()
    {
        // 只设置位置和大小，不设置字体样式
        RectTransform textRect = storyText.rectTransform;

        // 计算文字框的位置
        float anchorMinX = textBoxMarginLeft;
        float anchorMinY = textBoxMarginBottom;
        float anchorMaxX = 1f - textBoxMarginRight;
        float anchorMaxY = textBoxMarginBottom + textBoxHeightPercent;

        // 设置锚点
        textRect.anchorMin = new Vector2(anchorMinX, anchorMinY);
        textRect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        if (debugMode)
        {
            Debug.Log($"文字框设置: 位置({anchorMinX:F2},{anchorMinY:F2}) 大小({anchorMaxX - anchorMinX:F2},{anchorMaxY - anchorMinY:F2})");
        }
    }

    IEnumerator TestTextVisibility()
    {
        yield return new WaitForEndOfFrame();

        if (debugMode) Debug.Log("开始文本可见性测试");

        // 保存当前文本和样式
        string originalText = storyText.text;
        Color originalColor = storyText.color;
        float originalSize = storyText.fontSize;

        //// 测试文本是否可见（使用醒目的红色和大字体）
        //storyText.text = "字体测试 - 应显示红色大字";
        //storyText.color = Color.red;
        //storyText.fontSize = 120; // 测试更大的字体
        //storyText.ForceMeshUpdate();

        if (debugMode)
        {
            Debug.Log($"测试文本: {storyText.text}");
            Debug.Log($"测试颜色: {storyText.color}");
            Debug.Log($"测试大小: {storyText.fontSize}");
        }

        yield return new WaitForSeconds(2.0f);

        // 恢复组件原始设置
        storyText.text = originalText;
        storyText.color = originalColor;
        storyText.fontSize = originalSize;

        storyText.ForceMeshUpdate();

        if (debugMode) Debug.Log("文本可见性测试完成");
    }

    void InitializeSkipTip()
    {
        if (skipTip == null)
        {
            if (debugMode) Debug.LogWarning("SkipTip 未分配，跳过初始化");
            return;
        }

        skipTip.text = "右键跳过 / 长按1秒跳过全部";
        skipTip.color = new Color(1, 1, 0.7f, 0.8f);
        skipTip.fontSize = 48;
        skipTip.alignment = TextAlignmentOptions.Right;

        RectTransform skipRect = skipTip.rectTransform;
        skipRect.anchorMin = new Vector2(0.75f, 0.9f);
        skipRect.anchorMax = new Vector2(0.95f, 0.95f);
        skipRect.offsetMin = Vector2.zero;
        skipRect.offsetMax = Vector2.zero;

        //if (debugMode) Debug.Log("跳过提示初始化完成");
    }

    void InitializeAudio()
    {
        if (bgmAudioSource == null)
        {
            if (debugMode) Debug.LogWarning("BgmAudioSource 未分配，跳过音频初始化");
            return;
        }

        if (bgmAudioSource.clip == null)
        {
            if (debugMode) Debug.LogWarning("BgmAudioSource 没有音频剪辑");
            return;
        }

        bgmAudioSource.volume = 0;
        bgmAudioSource.Play();
        StartCoroutine(FadeBgmVolume(0, 0.6f, bgmFadeDuration));

        if (debugMode) Debug.Log($"音频初始化: {bgmAudioSource.clip.name}");
    }

    void ValidateComponents()
    {
        bool allValid = true;

        if (storyText == null)
        {
            Debug.LogError("StoryText 未分配");
            allValid = false;
        }
        else if (storyText.font == null)
        {
            Debug.LogError("StoryText 字体资源未设置！请在Inspector中分配字体");
            allValid = false;
        }

        if (allValid && debugMode)
        {
            Debug.Log("所有组件验证通过");
            Debug.Log($"当前字体: {storyText.font?.name ?? "无"}, 大小: {storyText.fontSize}, 颜色: {storyText.color}");
        }
    }

    void Update()
    {
        // 右键单击：跳过当前剧情段
        if (Input.GetMouseButtonDown(1) && !isLongSkipping)
        {
            if (debugMode) Debug.Log("右键按下，开始检测长按");
            StartCoroutine(CheckLongPress());
        }

        if (Input.GetMouseButtonUp(1))
        {
            isLongSkipping = false;
        }

        // 调试快捷键
        if (debugMode)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugCurrentState();
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ForceRefreshText();
            }
        }
    }

    void ForceRefreshText()
    {
        if (storyText != null)
        {
            string currentText = storyText.text;
            storyText.text = "";
            storyText.text = currentText;
            storyText.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            Debug.Log("强制刷新文本显示");
        }
    }

    void DebugCurrentState()
    {
        Debug.Log("=== F1 调试信息 ===");
        Debug.Log($"当前字体大小: {storyText.fontSize}");
        Debug.Log($"当前字体颜色: {storyText.color}");
        Debug.Log($"字体透明度: {storyText.color.a}");
        Debug.Log($"字体资源: {storyText.font?.name ?? "无"}");
        Debug.Log($"自动缩放状态: {storyText.enableAutoSizing}");
        Debug.Log($"文本内容: {(storyText.text.Length > 50 ? storyText.text.Substring(0, 50) + "..." : storyText.text)}");

        // 检查CanvasRenderer
        CanvasRenderer renderer = storyText.GetComponent<CanvasRenderer>();
        if (renderer != null)
        {
            Debug.Log($"CanvasRenderer透明度: {renderer.GetAlpha()}");
            Debug.Log($"CullTransparentMesh: {renderer.cullTransparentMesh}");
        }
    }

    // 播放单段剧情
    private IEnumerator PlayStorySegment(int index)
    {
        if (debugMode) Debug.Log($"播放剧情段 {index + 1}/{storyData.storySegments.Length}");

        if (index >= storyData.storySegments.Length)
        {
            if (debugMode) Debug.Log("所有剧情播放完毕，加载主场景");
            StartCoroutine(LoadMainScene());
            yield break;
        }

        isTextFinished = false;
        isSkipping = false;
        storyText.text = "";

        StorySegment currentSegment = storyData.storySegments[index];

        if (currentSegment == null)
        {
            Debug.LogError($"剧情段 {index} 为 null，跳过");
            currentSegmentIndex++;
            StartCoroutine(PlayStorySegment(currentSegmentIndex));
            yield break;
        }

        // 1. 背景图片淡入
        if (currentSegment.bgSprite != null)
        {
            yield return StartCoroutine(FadeImage(bgImage, currentSegment.bgSprite, true));
        }
        else
        {
            if (debugMode) Debug.LogWarning($"剧情段 {index} 没有背景图片");
            yield return new WaitForSeconds(0.5f);
        }

        // 2. 打字机效果显示文本
        if (!string.IsNullOrEmpty(currentSegment.storyText))
        {
            storyText.enabled = true;
            storyText.ForceMeshUpdate();

            textCoroutine = StartCoroutine(TypeText(currentSegment.storyText, currentSegment.textSpeed));
        }
        else
        {
            if (debugMode) Debug.LogWarning($"剧情段 {index} 没有文本内容");
            isTextFinished = true;
        }

        while (!isTextFinished && !isSkipping)
        {
            yield return null;
        }

        yield return new WaitForSeconds(2.0f);
        currentSegmentIndex++;
        StartCoroutine(PlayStorySegment(currentSegmentIndex));
    }

    // 打字机效果
    private IEnumerator TypeText(string content, float speed)
    {
        if (debugMode) Debug.Log($"开始打字机效果，文本长度: {content.Length}");

        storyText.enabled = true;
        storyText.text = "";
        storyText.ForceMeshUpdate();

        char[] chars = content.ToCharArray();
        isTextFinished = false;

        for (int i = 0; i < chars.Length; i++)
        {
            if (isSkipping || isLongSkipping)
            {
                if (debugMode) Debug.Log("打字机被跳过");
                break;
            }

            storyText.text = content.Substring(0, i + 1);

            // 每显示一个字符都强制更新
            storyText.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();

            yield return new WaitForSeconds(speed);
        }

        storyText.text = content;
        isTextFinished = true;

        if (debugMode) Debug.Log("打字机完成");
    }

    // 图片淡入/淡出效果
    private IEnumerator FadeImage(Image image, Sprite targetSprite, bool isFadeIn)
    {
        if (image == null)
        {
            Debug.LogError("FadeImage: image 为 null");
            yield break;
        }

        if (targetSprite != null)
        {
            image.sprite = targetSprite;
            image.preserveAspect = true;
        }

        float targetAlpha = isFadeIn ? 1 : 0;
        float currentAlpha = image.color.a;
        float timer = 0;

        if (debugMode) Debug.Log($"图片淡入淡出: {targetSprite?.name ?? "null"} {(isFadeIn ? "淡入" : "淡出")}");

        while (timer < imageFadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(currentAlpha, targetAlpha, timer / imageFadeTime);
            image.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        image.color = new Color(1, 1, 1, targetAlpha);
    }

    // 跳过当前剧情段
    private void SkipCurrentSegment()
    {
        if (isSkipping || isLongSkipping)
            return;

        //if (debugMode) Debug.Log("跳过当前剧情段");

        isSkipping = true;

        if (textCoroutine != null)
        {
            StopCoroutine(textCoroutine);
        }

        if (storyData != null && currentSegmentIndex < storyData.storySegments.Length)
        {
            storyText.text = storyData.storySegments[currentSegmentIndex].storyText;
            storyText.ForceMeshUpdate();
        }

        isTextFinished = true;
    }

    // 检测右键长按（1秒跳过全部）
    private IEnumerator CheckLongPress()
    {
        float pressTime = 0;
        isLongSkipping = true;

        if (debugMode) Debug.Log("开始长按检测");

        while (Input.GetMouseButton(1))
        {
            pressTime += Time.deltaTime;

            if (pressTime >= 1f)
            {
                if (debugMode) Debug.Log("长按1秒，跳过全部剧情");

                if (textCoroutine != null)
                {
                    StopCoroutine(textCoroutine);
                }

                //if (storyText != null)
                //{
                //    storyText.text = "跳过全部剧情...";
                //    storyText.color = Color.yellow;
                //}

                StartCoroutine(LoadMainScene());
                yield break;
            }
            yield return null;
        }

        if (pressTime < 1f && pressTime > 0.1f)
        {
            if (debugMode) Debug.Log($"短按跳过 ({pressTime:F2}秒)");
            SkipCurrentSegment();
        }
        else if (debugMode)
        {
            Debug.Log("右键点击太短，忽略");
        }

        isLongSkipping = false;
    }

    // 加载主游戏场景
    private IEnumerator LoadMainScene()
    {
        if (debugMode) Debug.Log("=== 开始加载主场景 ===");

        // 安全检查：确保关键组件不为空
        if (storyText == null)
        {
            Debug.LogWarning("警告: StoryText 组件已丢失，无法显示加载信息。");
        }

        if (string.IsNullOrEmpty(mainSceneName))
        {
            string errorMsg = "错误: mainSceneName 为空！";
            Debug.LogError(errorMsg);
            if (storyText != null)
            {
                storyText.text = errorMsg;
                storyText.color = Color.red;
            }
            yield break;
        }

        if (debugMode) Debug.Log($"目标场景: {mainSceneName}");

        // 1. 背景音乐淡出停止 (增加空检查)
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            if (debugMode) Debug.Log("淡出背景音乐");
            yield return StartCoroutine(FadeBgmVolume(bgmAudioSource.volume, 0, bgmFadeDuration));
            bgmAudioSource.Stop();
        }

        // 2. 显示加载提示 (增加空检查)
        if (storyText != null)
        {
            storyText.text = "等待你解开事情的谜团，好运。";
            storyText.color = Color.yellow;
            storyText.alignment = TextAlignmentOptions.Center;
            storyText.ForceMeshUpdate();
        }

        // 3. 异步加载主场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainSceneName);
        asyncLoad.allowSceneActivation = false;

        if (debugMode) Debug.Log("开始异步加载");

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (storyText != null)
            {
                storyText.text = $"加载中... {Mathf.RoundToInt(progress * 100)}%";
                storyText.ForceMeshUpdate();
            }

            if (asyncLoad.progress >= 0.9f)
            {
                if (storyText != null)
                {
                    storyText.text = "加载完成！";
                    storyText.ForceMeshUpdate();
                }

                yield return new WaitForSeconds(0.5f);

                if (debugMode) Debug.Log("激活场景");
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        if (debugMode) Debug.Log("Loading");
    }

    // 背景音乐淡入淡出效果
    private IEnumerator FadeBgmVolume(float startVol, float targetVol, float duration)
    {
        if (bgmAudioSource == null)
            yield break;

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