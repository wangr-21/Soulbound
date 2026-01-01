using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 照片剧情动画控制器
/// 核心规则：
/// 1. 前9张：自动播放+专属音频+专属字幕（固定时长）
/// 2. 第10张：无字幕、无固定时长，一直显示直到点击按钮
/// 3. 点击按钮：保留第10张照片，显示结果面板 → 3秒后返回主菜单StartScene
/// </summary>
public class PhotoStoryController : MonoBehaviour
{
    [Header("==== 照片与音频配置 ====")]
    [Tooltip("按顺序放入第1~10张照片（GameObject）")]
    public GameObject[] photos = new GameObject[10];
    [Tooltip("按顺序放入第1~10张照片的专属配音（每张对应一个）")]
    public AudioClip[] photoAudios = new AudioClip[10];
    [Tooltip("仅填写前9张照片的独立字幕（索引0-8）")]
    public string[] photoSubtitles = new string[9];

    [Header("==== UI配置 ====")]
    [Tooltip("前9张照片的字幕面板（半黑背景）")]
    public GameObject subtitlePanel;
    [Tooltip("字幕面板中的文本组件")]
    public TextMeshProUGUI subtitleText;
    [Tooltip("第10张照片显示的确认按钮（点击即结束）")]
    public Button finishButton;
    [Tooltip("点击按钮后弹出的结果面板（半黑背景，可自定义大小）")]
    public GameObject resultPanel;
    [Tooltip("结果面板中的文本组件（显示最低/中等/最高）")]
    public TextMeshProUGUI resultText;

    [Header("==== 时间配置 ====")]
    [Tooltip("前9张照片显示时长（秒），建议匹配对应配音长度")]
    public float[] photoDurations = new float[9]; // 仅前9张需要时长

    [Header("==== 游戏结束配置（已设为返回主菜单） ====")]
    [Tooltip("主菜单场景名称（已固定为StartScene）")]
    public string mainMenuSceneName = "StartScene";
    [Tooltip("显示结果后延迟几秒返回主菜单（默认3秒）")]
    public float endDelay = 3f;

    private AudioSource audioSource;

    void Start()
    {
        // 初始化音频源
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        // 初始化所有UI为隐藏状态
        ResetAllUI();
        // 开始播放照片剧情
        StartCoroutine(PlayPhotoStory());
    }

    /// <summary>
    /// 重置所有UI到初始隐藏状态
    /// </summary>
    void ResetAllUI()
    {
        // 隐藏所有照片
        foreach (var photo in photos)
        {
            if (photo != null) photo.SetActive(false);
        }
        // 隐藏字幕面板、按钮、结果面板
        subtitlePanel.SetActive(false);
        finishButton.gameObject.SetActive(false);
        resultPanel.SetActive(false);
    }

    /// <summary>
    /// 核心协程：按顺序播放10张照片
    /// </summary>
    IEnumerator PlayPhotoStory()
    {
        // 播放前9张照片（自动切换，显示字幕）
        for (int i = 0; i < 9; i++)
        {
            if (photos[i] == null) continue;

            // 显示当前照片
            photos[i].SetActive(true);
            // 播放专属音频
            PlayPhotoAudio(i);
            // 显示专属字幕
            ShowSubtitle(i);

            // 等待配置时长（无配置则默认5秒）
            float waitTime = photoDurations[i] > 0 ? photoDurations[i] : 5f;
            yield return new WaitForSeconds(waitTime);

            // 等待音频播放完毕再切换
            if (audioSource.isPlaying)
            {
                yield return new WaitUntil(() => !audioSource.isPlaying);
            }

            // 隐藏当前照片和字幕
            photos[i].SetActive(false);
            subtitlePanel.SetActive(false);
        }

        // 播放第10张照片（无固定时长、无字幕，直到点击按钮）
        if (photos[9] != null)
        {
            photos[9].SetActive(true);
            PlayPhotoAudio(9); // 播放第10张音频
            finishButton.gameObject.SetActive(true); // 显示结束按钮
            // 绑定按钮点击事件：显示结果 + 返回主菜单
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(OnFinishButtonClick);
        }
    }

    /// <summary>
    /// 播放指定索引照片的专属配音
    /// </summary>
    void PlayPhotoAudio(int index)
    {
        if (photoAudios[index] != null)
        {
            audioSource.clip = photoAudios[index];
            audioSource.Play();
        }
    }

    /// <summary>
    /// 显示指定索引照片的独立字幕（仅前9张调用）
    /// </summary>
    void ShowSubtitle(int index)
    {
        subtitlePanel.SetActive(true);
        subtitleText.text = !string.IsNullOrEmpty(photoSubtitles[index])
            ? photoSubtitles[index]
            : $"第{index + 1}张照片";
    }

    /// <summary>
    /// 第10张按钮点击事件：保留照片，显示结果 + 延迟返回主菜单
    /// </summary>
    void OnFinishButtonClick()
    {
        // 仅隐藏按钮（核心修改：移除隐藏第10张照片的代码）
        finishButton.gameObject.SetActive(false);
        // 停止所有音频
        if (audioSource.isPlaying) audioSource.Stop();

        // 显示结果面板（按碎片数显示对应文本）
        resultPanel.SetActive(true);
        SetResultTextByFragmentCount();

        // 延迟3秒返回主菜单StartScene（让玩家看清结果）
        StartCoroutine(ReturnToMainMenuCoroutine());
    }

    /// <summary>
    /// 根据碎片数量区间设置结果文本（0-2→最低、3-4→中等、5-6→最高）
    /// </summary>
    void SetResultTextByFragmentCount()
    {
        int fragmentCount = 0;
        // 读取碎片数量（兼容FragmentManager为空的情况）
        if (FragmentManager.Instance != null)
        {
            fragmentCount = FragmentManager.Instance.currentFragmentCount;
        }

        // 区间判断逻辑
        if (fragmentCount >= 0 && fragmentCount <= 2)
        {
            resultText.text = "在真相面前，你选择了逃避。你的灵魂再次脱离国王的躯体，头也不回地飞离王城，永远在这个静止的世界里徘徊成为一个永恒的旁观者，一个不被任何生灵感知的幽灵。";
        }
        else if (fragmentCount >= 3 && fragmentCount <= 4)
        {
            resultText.text = "巨大的负罪感将你压垮。你无法面对这一切。你选择继续维持这个静止的王国，用净化者来维护这死寂的“秩序”。你成为了王座上唯一的、有意识的空壳，永远活在自己铸造的监狱里。";
        }
        else if (fragmentCount >= 5 && fragmentCount <= 6)
        {
            resultText.text = "你站起身，眼中不再是绝望，而是坚定地将手伸向天平。你调动起这一路上磨练出的、对所有生灵灵魂的深刻理解，以自身残存的王室血脉和全部灵魂为引，发动了反向驱动。强大的能量从你体内抽离，光芒笼罩整个王国。你看到净化者纷纷停机，看到森林的鸟儿振翅飞走，看到村庄里的母亲抱紧了苏醒的女儿……你的身体在王座上逐渐化为光尘，但在彻底消散前你看到了士兵欣慰的眼神，女孩一家开心的笑容和国家的安宁。";
        }
        else
        {
            resultText.text = "数据异常";
        }
    }

    /// <summary>
    /// 延迟返回主菜单协程
    /// </summary>
    IEnumerator ReturnToMainMenuCoroutine()
    {
        // 等待指定时间（让玩家看清结果）
        yield return new WaitForSeconds(endDelay);
        // 跳转到主菜单场景StartScene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// 跳过按钮：直接跳转到第10张照片（无字幕）
    /// </summary>
    public void SkipToLastPhoto()
    {
        StopAllCoroutines();
        ResetAllUI();

        if (photos[9] != null)
        {
            photos[9].SetActive(true);
            PlayPhotoAudio(9);
            finishButton.gameObject.SetActive(true);
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(OnFinishButtonClick);
        }
    }

    // 防呆：检查场景是否存在（编辑器模式下生效）
    private void OnValidate()
    {
        // 确保主菜单场景名称是StartScene
        mainMenuSceneName = "StartScene";
    }
}