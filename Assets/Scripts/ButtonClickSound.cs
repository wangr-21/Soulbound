using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // 确保挂载对象有Button组件
public class ButtonClickSound : MonoBehaviour
{
    [Header("音效设置")]
    [Tooltip("按钮点击时播放的音效文件")]
    public AudioClip clickSound; // 拖拽音效文件到这里

    [Tooltip("音效播放音量（0-1）")]
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    private Button _button; // 按钮组件引用
    private AudioSource _audioSource; // 音频播放组件引用

    void Awake()
    {
        // 获取按钮组件
        _button = GetComponent<Button>();

        // 添加AudioSource组件（如果没有的话）
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 配置AudioSource参数（不循环、不自动播放）
        _audioSource.loop = false;
        _audioSource.playOnAwake = false;
        _audioSource.volume = soundVolume;

        // 绑定按钮点击事件
        _button.onClick.AddListener(PlayClickSound);
    }

    /// <summary>
    /// 播放点击音效的核心方法
    /// </summary>
    private void PlayClickSound()
    {
        // 检查是否有指定音效文件，避免空引用报错
        if (clickSound != null)
        {
            // 播放一次性音效（无需手动停止）
            _audioSource.PlayOneShot(clickSound, soundVolume);
        }
        else
        {
            Debug.LogWarning("未给按钮指定点击音效文件！", this.gameObject);
        }
    }

    // 可选：如果需要在编辑器中实时调整音量
    void OnValidate()
    {
        if (_audioSource != null)
        {
            _audioSource.volume = soundVolume;
        }
    }
}