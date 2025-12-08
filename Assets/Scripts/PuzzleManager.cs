using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // 新增：用于协程

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance; // 单例模式，方便调用

    public TMP_InputField input1, input2, input3, input4; // 4个输入框
    public TextMeshProUGUI countdownText, errorText; // 倒计时/错误提示
    public Button confirmButton; // 确认按钮
    public GameObject puzzlePanel; // 谜题面板
    private ChestController currentChest; // 当前打开的宝箱
    private CountdownTimer timer;
    private string correctAnswer = "2056"; // 比如根据箭头推理出的数字
    private Coroutine hideErrorCoroutine;   //存储当前运行的隐藏协程

    void Awake()
    {
        Instance = this;
        timer = GetComponent<CountdownTimer>();
        if (timer == null)
        {
            timer = FindObjectOfType<CountdownTimer>(true); // true：包括隐藏物体
            if (timer != null)
                Debug.Log("找到场景中的 CountdownTimer");
            else
                Debug.LogError("场景中未找到 CountdownTimer 组件！");
        }
        confirmButton.onClick.AddListener(CheckAnswer);

        // 初始隐藏面板和错误提示
        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);
        if (errorText != null)
            errorText.gameObject.SetActive(false);

        // 强制关联倒计时文本（双重保险）
        if (timer != null && countdownText != null)
        {
            timer.countdownText = countdownText;
            Debug.Log("倒计时文本关联成功");
        }
        else
        {
            Debug.LogError("倒计时文本或计时器未关联！");
        }
    }

    // 开始谜题（由宝箱调用）
    public void StartPuzzle(ChestController chest)
    {
        currentChest = chest;
        // 重置输入和提示
        input1.text = input2.text = input3.text = input4.text = "";
        errorText.gameObject.SetActive(false);
        // 启动3分钟倒计时
        timer.StartCountdown(180);

        // 新增：打开UI时，解锁鼠标并显示（方便操作拼图/按钮）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 验证答案
    void CheckAnswer()
    {
        string userAns = input1.text + input2.text + input3.text + input4.text;
        if (userAns.Length < 4)
        {
            errorText.text = "请输入4位数字！";
            ShowErrorThenHide(1f); // 显示1秒后隐藏
            return;
        }

        if (userAns == correctAnswer)
        {
            Debug.Log("答案正确");
            ClosePuzzle();
        }
        else
        {
            errorText.text = "错误！请重新输入！";
            ShowErrorThenHide(1f); // 显示1秒后隐藏
        }
    }

    // 显示错误提示并延迟隐藏
    private void ShowErrorThenHide(float delay)
    {
        errorText.gameObject.SetActive(true);
        // 停止之前正在运行的协程（如果存在）
        if (hideErrorCoroutine != null)
        {
            StopCoroutine(hideErrorCoroutine);
        }
        // 启动新协程并保存引用
        hideErrorCoroutine = StartCoroutine(HideErrorCoroutine(delay));
    }

    // 修改隐藏错误提示的协程，使用不受时间缩放影响的等待方式
    private IEnumerator HideErrorCoroutine(float delay = 1f)
    {
        // 用WaitForSecondsRealtime替代WaitForSeconds，确保延迟不受游戏时间缩放影响
        yield return new WaitForSecondsRealtime(delay);
        if (errorText != null) // 增加空引用检查，避免异常
        {
            errorText.gameObject.SetActive(false);
        }
        hideErrorCoroutine = null;
    }

    // 关闭谜题面板
    public void ClosePuzzle()
    {
        puzzlePanel.SetActive(false);
        currentChest.LockChest();
        timer.StopCountdown();

        // 新增：关闭UI后，重新锁定鼠标并隐藏（恢复玩家移动控制）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 倒计时结束时调用
    public void OnTimeEnd()
    {
        ClosePuzzle();
    }
}