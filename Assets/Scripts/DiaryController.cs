using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DiaryController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject passwordPanel;      // 密码面板（DiaryPasswordPanel）
    [SerializeField] private GameObject contentPanel;       // 日记内容父面板（包含3个子页面）
    [SerializeField] private TMP_InputField passwordInputField; // 密码输入框
    [SerializeField] private TMP_Text errorHintText; //绑定新建的ErrorHintText

    [Header("日记页面")]
    [SerializeField] private List<GameObject> diaryPages;   // 按顺序添加：第一页、第二页、第三页
    [Header("Settings")]
    [SerializeField] private string correctPassword = "8541"; // 正确密码（可修改）
    [SerializeField] private float interactionDistance = 3f; // 玩家交互距离（可调整）
    [SerializeField] private float errorHintDuration = 2f; // 错误提示显示时长（默认2秒，可调整）
    [SerializeField] private Color errorColor = Color.red; // 错误提示颜色（默认红色）


    private bool isPlayerNear = false;
    private bool isUnlocked = false; // 日记是否已解锁
    private int currentPageIndex = 0; // 当前页面索引
    private Transform playerTransform;

    void Start()
    {
        // 获取玩家引用（确保玩家标签设为"Player"）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // 初始隐藏所有UI面板
        passwordPanel?.SetActive(false);
        contentPanel?.SetActive(false);

        // 初始化日记页面（隐藏所有，激活第一页备用）
        InitializePages();

        // 提前设置密码输入框限制（4位数字）
        SetPasswordInputLimit();
        InitializeErrorHintText();
    }

    //初始化错误提示文本（确保初始隐藏、颜色正确）
    void InitializeErrorHintText()
    {
        if (errorHintText == null) return;
        errorHintText.text = ""; // 初始为空
        errorHintText.color = errorColor; // 设为红色
        errorHintText.gameObject.SetActive(false); // 初始隐藏
    }

    void Update()
    {
        CheckPlayerDistance();

        // 玩家靠近时按O键交互
        if (isPlayerNear && Input.GetKeyDown(KeyCode.O))
        {
            if (!isUnlocked)
                OpenPasswordPanel(); // 未解锁→打开密码面板
            else
                OpenContentPanel(); // 已解锁→直接打开日记内容
        }
    }

    // 检查玩家与日记本的距离
    void CheckPlayerDistance()
    {
        if (playerTransform == null) return;
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = distance <= interactionDistance;
        // 可选：添加靠近提示（比如显示"按O键查看日记"）
        // if (isPlayerNear && !isUnlocked) ShowHint("按O键查看日记");
    }

    // 初始化日记页面（隐藏所有，激活第一页）
    void InitializePages()
    {
        foreach (var page in diaryPages)
            page.SetActive(false);

        if (diaryPages.Count > 0)
        {
            currentPageIndex = 0;
            diaryPages[currentPageIndex].SetActive(true);
        }
    }

    // 打开密码面板（暂停游戏+解锁鼠标）
    public void OpenPasswordPanel()
    {
        passwordPanel?.SetActive(true);
        Time.timeScale = 0; // 暂停游戏
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 清空输入框并聚焦
        passwordInputField?.Select();
        passwordInputField?.ActivateInputField();
        passwordInputField.text = "";
    }

    // 关闭密码面板（返回场景，恢复游戏）
    public void ClosePasswordPanel()
    {
        passwordPanel?.SetActive(false);
        ResumeGame(); // 恢复游戏状态
    }

    // 验证密码（正确则解锁日记）
    public void ValidatePassword()
    {
        if (passwordInputField == null) return;

        string inputPassword = passwordInputField.text;
        if (inputPassword == correctPassword)
        {
            isUnlocked = true;
            ClosePasswordPanel();
            OpenContentPanel();
            Debug.Log("密码正确，日记解锁！");
        }
        else
        {
            Debug.Log("密码错误！");
            StartCoroutine(ShakeInputField()); // 原有抖动效果保留
            StartCoroutine(ShowErrorHint()); // 新增：显示错误提示
        }
    }

    //错误提示显示协程（显示2秒后自动隐藏，不影响原有提示）
    System.Collections.IEnumerator ShowErrorHint()
    {
        if (errorHintText == null) yield break;

        // 显示错误提示
        errorHintText.gameObject.SetActive(true);
        errorHintText.text = "密码错误！请重新输入！";

        // 等待指定时长（用UnscaledTime，因为游戏暂停时Time.timeScale=0）
        yield return new WaitForSecondsRealtime(errorHintDuration);

        // 隐藏错误提示
        errorHintText.gameObject.SetActive(false);
        errorHintText.text = "";
    }

    // 密码错误时输入框抖动效果
    System.Collections.IEnumerator ShakeInputField()
    {
        if (passwordInputField == null) yield break;

        RectTransform rect = passwordInputField.GetComponent<RectTransform>();
        Vector3 originalPos = rect.localPosition;
        float shakeTime = 0.5f;
        float shakeStrength = 10f;

        for (float t = 0; t < shakeTime; t += Time.unscaledDeltaTime)
        {
            float x = Random.Range(-1f, 1f) * shakeStrength;
            float y = Random.Range(-1f, 1f) * shakeStrength;
            rect.localPosition = originalPos + new Vector3(x, y, 0);
            yield return null;
        }

        rect.localPosition = originalPos;
    }

    // 打开日记内容面板（显示第一页）
    void OpenContentPanel()
    {
        contentPanel?.SetActive(true);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowPage(0); // 默认显示第一页
    }

    // 切换到指定页面（核心页面切换逻辑）
    void ShowPage(int pageIndex)
    {
        // 边界检查（防止索引越界）
        if (pageIndex < 0 || pageIndex >= diaryPages.Count) return;

        // 隐藏当前页面，激活目标页面
        diaryPages[currentPageIndex].SetActive(false);
        currentPageIndex = pageIndex;
        diaryPages[currentPageIndex].SetActive(true);
    }

    // 下一页（绑定第二页的下一页按钮）
    public void NextPage()
    {
        if (currentPageIndex < diaryPages.Count - 1)
        {
            ShowPage(currentPageIndex + 1);
        }
    }

    // 上一页（绑定第二/三页的上一页按钮）
    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            ShowPage(currentPageIndex - 1);
        }
    }

    // 关闭日记（绑定第三页的关闭按钮）
    public void CloseDiary()
    {
        contentPanel?.SetActive(false);
        ResumeGame();
    }

    // 恢复游戏状态（解锁鼠标+恢复时间缩放）
    void ResumeGame()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 限制密码输入框：只能输入4位数字
    void SetPasswordInputLimit()
    {
        if (passwordInputField == null) return;
        passwordInputField.contentType = TMP_InputField.ContentType.IntegerNumber; // 仅允许数字
        passwordInputField.characterLimit = 4; // 限制4位输入
    }

    // 场景视图中显示交互范围（黄色球体，方便调试）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}