using UnityEngine;
using UnityEngine.UI;

public class PanelController : MonoBehaviour
{
    // 引用UI元素，需要在Inspector面板中赋值
    [Header("UI元素引用")]
    public GameObject contentPanel; // 要显示/隐藏的面板
    public Button showPanelButton;  // 主按钮（点击显示面板）
    public Button returnButton;     // 面板上的返回按钮

    void Start()
    {
        // 初始隐藏面板
        contentPanel.SetActive(false);

        // 绑定按钮点击事件
        showPanelButton.onClick.AddListener(ShowPanel);
        returnButton.onClick.AddListener(HidePanel);
    }

    /// <summary>
    /// 显示面板的方法
    /// </summary>
    public void ShowPanel()
    {
        contentPanel.SetActive(true);
        // 可选：显示面板时可以禁用主按钮，避免重复点击
        // showPanelButton.interactable = false;
    }

    /// <summary>
    /// 隐藏面板的方法
    /// </summary>
    public void HidePanel()
    {
        contentPanel.SetActive(false);
        // 可选：隐藏面板后恢复主按钮可用状态
        // showPanelButton.interactable = true;
    }
}