using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SubtitlePanelController : MonoBehaviour
{
    [Header("关联组件（拖入当前物体下的子组件）")]
    public TMP_Text subtitleText; // 拖入SubtitlePanel下的SubtitleText
    public Image background;      // 拖入SubtitlePanel下的Background

    private void Awake()
    {
        // 自动查找子组件（如果没手动拖入）
        if (subtitleText == null) subtitleText = GetComponentInChildren<TMP_Text>();
        if (background == null) background = GetComponentInChildren<Image>();

        // 初始状态：隐藏面板和背景
        HideSubtitlePanel();
    }

    private void Update()
    {
        // 实时检测文本的显示状态，同步控制背景
        if (subtitleText != null && background != null)
        {
            // 文本启用 → 显示背景；文本禁用 → 隐藏背景
            bool isTextActive = subtitleText.enabled && !string.IsNullOrEmpty(subtitleText.text);
            background.enabled = isTextActive;
            gameObject.SetActive(isTextActive);
        }
    }

    /// <summary>
    /// 手动隐藏字幕面板（可选，用于强制隐藏）
    /// </summary>
    public void HideSubtitlePanel()
    {
        if (subtitleText != null) subtitleText.enabled = false;
        if (background != null) background.enabled = false;
        gameObject.SetActive(false);
    }
}