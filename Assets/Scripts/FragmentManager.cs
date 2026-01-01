using UnityEngine;
using TMPro;

public class FragmentManager : MonoBehaviour
{
    public static FragmentManager Instance; // 单例实例

    [Header("UI 元素")]
    public TextMeshProUGUI fragmentCountText; // 显示碎片数量的文本

    public int currentFragmentCount { get; private set; } = 0; // 当前碎片数量（改为公共属性）
    private const int maxFragmentCount = 6; // 最大碎片数量

    void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保存
        }
        else
        {
            Destroy(gameObject);
        }

        // 初始化UI显示
        UpdateFragmentUI();
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
            fragmentCountText.text = $"碎片: {currentFragmentCount}/{maxFragmentCount}";
        }
        else
        {
            Debug.LogError("未赋值碎片显示文本fragmentCountText！");
        }
    }
}