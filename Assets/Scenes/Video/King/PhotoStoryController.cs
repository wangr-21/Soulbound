using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class PhotoStoryController : MonoBehaviour
{
    [Header("==== 音频源分离配置 ====")]
    [Tooltip("专门播放照片剧情配音的音频源（自动创建）")]
    private AudioSource photoAudioSource;
    [Tooltip("专门播放BGM的音频源（可在编辑器拖入，或自动创建）")]
    public AudioSource bgmAudioSource; // 新增：BGM独立音频源
    [Tooltip("你的循环BGM音频文件")]
    public AudioClip bgmClip; // 新增：配置BGM文件

    [Header("==== 照片与音频配置 ====")]
    public GameObject[] photos = new GameObject[10];
    public AudioClip[] photoAudios = new AudioClip[10];
    public string[] photoSubtitles = new string[9];

    [Header("==== UI配置 ====")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;
    public Button finishButton;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("==== 时间配置 ====")]
    public float[] photoDurations = new float[9];
    public float minWaitTime = 2f;

    [Header("==== 游戏结束配置 ====")]
    public string mainMenuSceneName = "StartScene";
    public float endDelay = 3f;

    void Start()
    {
        // 1. 初始化两个音频源（分离）
        InitAudioSources();

        // 2. 启动BGM循环播放（你的核心需求）
        PlayBGM();

        // 3. 校验配置 + 初始化UI + 启动剧情
        ValidateConfig();
        ResetAllUI();
        StartCoroutine(PlayPhotoStory());
    }

    /// <summary>
    /// 初始化分离的音频源：照片音频源 + BGM音频源
    /// </summary>
    void InitAudioSources()
    {
        // 初始化照片音频源（专门播剧情配音，不循环）
        photoAudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        photoAudioSource.loop = false; // 强制关闭循环（剧情配音只播一次）
        photoAudioSource.playOnAwake = false;

        // 初始化BGM音频源（专门播循环背景音乐）
        if (bgmAudioSource == null)
        {
            // 如果没手动拖入，自动创建一个子物体挂载BGM音频源（避免和照片音频源冲突）
            GameObject bgmObj = new GameObject("BGMAudioSource");
            bgmObj.transform.SetParent(transform); // 挂到当前物体下，方便管理
            bgmAudioSource = bgmObj.AddComponent<AudioSource>();
        }
        bgmAudioSource.loop = true; // BGM强制循环（满足你的需求）
        bgmAudioSource.playOnAwake = false;
    }

    /// <summary>
    /// 播放循环BGM（独立控制，不影响照片音频）
    /// </summary>
    void PlayBGM()
    {
        if (bgmClip != null && bgmAudioSource != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("BGM音频文件未配置，跳过BGM播放", this);
        }
    }

    /// <summary>
    /// 校验配置，输出错误日志
    /// </summary>
    void ValidateConfig()
    {
        // 校验照片
        for (int i = 0; i < photos.Length; i++)
        {
            if (photos[i] == null)
            {
                Debug.LogError($"【配置错误】第{i + 1}张照片未赋值！", this);
            }
        }

        // 校验UI组件
        if (subtitlePanel == null) Debug.LogError("【配置错误】字幕面板未赋值！", this);
        if (subtitleText == null) Debug.LogError("【配置错误】字幕文本未赋值！", this);
        if (finishButton == null) Debug.LogError("【配置错误】确认按钮未赋值！", this);
        if (resultPanel == null) Debug.LogError("【配置错误】结果面板未赋值！", this);
        if (resultText == null) Debug.LogError("【配置错误】结果文本未赋值！", this);
    }

    void ResetAllUI()
    {
        foreach (var photo in photos)
        {
            if (photo != null) photo.SetActive(false);
        }
        subtitlePanel?.SetActive(false);
        finishButton?.gameObject.SetActive(false);
        resultPanel?.SetActive(false);
    }

    IEnumerator PlayPhotoStory()
    {
        Debug.Log("开始播放前9张照片", this);
        // 前9张
        for (int i = 0; i < 9; i++)
        {
            if (photos[i] == null)
            {
                Debug.LogWarning($"跳过第{i + 1}张照片（对象为空）", this);
                continue;
            }

            // 显示照片+音频+字幕
            photos[i].SetActive(true);
            PlayPhotoAudio(i); // 现在用photoAudioSource播放，和BGM无关
            ShowSubtitle(i);

            // 等待时长（最小minWaitTime秒）
            float waitTime = Mathf.Max(photoDurations[i], minWaitTime);
            Debug.Log($"第{i + 1}张照片等待{waitTime}秒", this);
            yield return new WaitForSeconds(waitTime);

            // 音频等待（只判断照片音频源，BGM循环不影响）
            if (photoAudioSource.isPlaying)
            {
                Debug.Log($"等待第{i + 1}张照片音频播放完毕", this);
                float audioWaitTime = 0;
                while (photoAudioSource.isPlaying && audioWaitTime < 10f)
                {
                    audioWaitTime += Time.deltaTime;
                    yield return null;
                }
                if (audioWaitTime >= 10f)
                {
                    Debug.LogWarning($"第{i + 1}张照片音频播放超时，强制停止", this);
                    photoAudioSource.Stop(); // 只停照片音频，BGM继续
                }
            }

            // 隐藏当前内容
            photos[i].SetActive(false);
            subtitlePanel?.SetActive(false);
            Debug.Log($"第{i + 1}张照片播放完毕，切换下一张", this);
        }

        // 第10张
        Debug.Log("开始播放第10张照片", this);
        if (photos[9] != null)
        {
            photos[9].SetActive(true);
            PlayPhotoAudio(9);
            finishButton.gameObject.SetActive(true);
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(OnFinishButtonClick);
        }
        else
        {
            Debug.LogError("第10张照片未赋值！", this);
        }
    }

    /// <summary>
    /// 播放照片专属音频（只用photoAudioSource，不影响BGM）
    /// </summary>
    void PlayPhotoAudio(int index)
    {
        if (photoAudioSource == null) return;

        // 停止当前照片音频，避免叠加（不影响BGM）
        if (photoAudioSource.isPlaying) photoAudioSource.Stop();

        if (photoAudios[index] != null)
        {
            photoAudioSource.clip = photoAudios[index];
            photoAudioSource.Play();
        }
        else
        {
            Debug.LogWarning($"第{index + 1}张照片无音频", this);
        }
    }

    void ShowSubtitle(int index)
    {
        if (subtitlePanel == null || subtitleText == null) return;

        subtitlePanel.SetActive(true);
        subtitleText.text = !string.IsNullOrEmpty(photoSubtitles[index])
            ? photoSubtitles[index]
            : $"第{index + 1}张照片";
    }

    void OnFinishButtonClick()
    {
        Debug.Log("点击确认按钮，显示结果面板", this);
        // 禁用按钮，防止重复点击
        finishButton.interactable = false;
        finishButton.gameObject.SetActive(false);

        // 只停止照片音频，BGM可以选择继续播或停止（根据你的需求）
        if (photoAudioSource != null && photoAudioSource.isPlaying)
        {
            photoAudioSource.Stop();
        }
        // 可选：如果想在结局时停止BGM，取消下面注释
        // if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        // {
        //     bgmAudioSource.Stop();
        // }

        resultPanel?.SetActive(true);
        SetResultTextByFragmentCount();

        StartCoroutine(ReturnToMainMenuCoroutine());
    }

    void SetResultTextByFragmentCount()
    {
        int fragmentCount = 0;
        if (FragmentManager.Instance != null)
        {
            fragmentCount = FragmentManager.Instance.currentFragmentCount;
        }

        Debug.Log($"当前碎片数：{fragmentCount}", this);

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

    IEnumerator ReturnToMainMenuCoroutine()
    {
        yield return new WaitForSeconds(endDelay);
        Debug.Log($"延迟{endDelay}秒后，返回主菜单：{mainMenuSceneName}", this);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SkipToLastPhoto()
    {
        Debug.Log("执行跳过操作，直接显示第10张照片", this);
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
        else
        {
            Debug.LogError("跳过失败：第10张照片未赋值！", this);
        }
    }

    private void OnValidate()
    {
        mainMenuSceneName = "StartScene";
        // 确保时长数组长度为9
        if (photoDurations.Length != 9)
        {
            System.Array.Resize(ref photoDurations, 9);
        }
    }
}