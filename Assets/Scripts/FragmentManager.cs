using UnityEngine;
using TMPro;

public class FragmentManager : MonoBehaviour
{
    public static FragmentManager Instance; // 单例模式

    [Header("UI 组件")]
    public TextMeshProUGUI fragmentCountText; // 显示碎片数量的文本

    public int currentFragmentCount { get; private set; } = 0; // 当前碎片数量（默认为零）
    private const int maxFragmentCount = 6; // 最大碎片数量

    void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保留
        }
        else
        {
            Destroy(gameObject);
        }

        // 初始化UI显示
        UpdateFragmentUI();

        // 确保UI在右上角显示
        SetupUIToTopRight();
    }

    // 设置UI在右上角显示
    private void SetupUIToTopRight()
    {
        if (fragmentCountText != null)
        {
            RectTransform rectTransform = fragmentCountText.GetComponent<RectTransform>();

            // 设置锚点为右上角
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f); // 以右上角为基准点

            // 设置位置，距离屏幕边缘20像素
            rectTransform.anchoredPosition = new Vector2(-20f, -20f);

            // 设置对齐方式为右对齐
            fragmentCountText.alignment = TextAlignmentOptions.TopRight;
        }
    }

    // 增加碎片数量
    public void AddFragment()
    {
        if (currentFragmentCount < maxFragmentCount)
        {
            currentFragmentCount++;
            UpdateFragmentUI();
            Debug.Log($"碎片数量更新: {currentFragmentCount}/{maxFragmentCount}");
        }
    }

    // 更新UI显示
    private void UpdateFragmentUI()
    {
        if (fragmentCountText != null)
        {
            // 修改为“碎片数：X/Y”
            fragmentCountText.text = $"碎片数：{currentFragmentCount}/{maxFragmentCount}";
        }
        else
        {
            Debug.LogError("未分配碎片显示文本 fragmentCountText。");
        }
    }
}