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

    private IEnumerator HideErrorCoroutine(float delay = 1f)
    {
        yield return new WaitForSeconds(delay);
        errorText.gameObject.SetActive(false);
        // 协程执行完毕后清空引用
        hideErrorCoroutine = null;
    }

    // 关闭谜题面板
    public void ClosePuzzle()
    {
        puzzlePanel.SetActive(false);
        currentChest.LockChest();
        timer.StopCountdown();
    }

    // 倒计时结束时调用
    public void OnTimeEnd()
    {
        ClosePuzzle();
    }
}