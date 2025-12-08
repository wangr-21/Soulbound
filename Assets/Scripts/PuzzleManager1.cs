using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PuzzleManager1 : MonoBehaviour
{
    public static PuzzleManager1 Instance; // 单例模式，方便调用
    public PuzzlePiece[] puzzlePieces; // 赋值9块拼图（0~8索引）
    public TextMeshProUGUI countdownText, errorText; // 倒计时/错误提示
    public Button confirmButton; // 拼图确认按钮（ConfirmBtn1）
    public GameObject puzzlePanel1; // 拼图面板
    public GameObject fragmentPrefab; // 奖励碎片预制体（可选）
    private ChestController1 currentChest; // 当前触发的Chest1
    private CountdownTimer timer;
    private PuzzlePiece firstSelectedPiece; // 第一个选中的拼图块（用于交换）
    private Coroutine hideErrorCoroutine; // 错误提示隐藏协程

    void Awake()
    {
        // 单例初始化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 找到倒计时组件
        timer = GetComponent<CountdownTimer>();
        if (timer == null)
        {
            timer = FindObjectOfType<CountdownTimer>(true);
            if (timer == null) Debug.LogError("未找到CountdownTimer组件！");
        }

        // 绑定确认按钮事件
        confirmButton.onClick.AddListener(CheckPuzzle);

        // 初始状态设置
        if (puzzlePanel1 != null) puzzlePanel1.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);

        // 关联倒计时文本
        if (timer != null && countdownText != null)
        {
            timer.countdownText = countdownText;
            Debug.Log("拼图倒计时文本关联成功");
        }
    }

    // 启动拼图（由ChestController1调用）
    public void StartPuzzle(ChestController1 chest)
    {
        currentChest = chest;
        errorText.gameObject.SetActive(false);
        ShufflePuzzle(); // 打乱拼图
        timer.StartCountdown(180); // 4分钟倒计时（240秒）

        // 新增：打开UI时，解锁鼠标并显示（方便操作拼图/按钮）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 打乱拼图（Fisher-Yates洗牌算法，确保可解）
    private void ShufflePuzzle()
    {
        // 重置所有拼图块的当前索引
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            puzzlePieces[i].currentIndex = i;
        }

        // 随机交换100次（确保打乱且可解）
        for (int i = puzzlePieces.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            puzzlePieces[i].SwapPiece(puzzlePieces[randomIndex]);
        }
    }

    // 拼图块点击处理（交换逻辑）
    public void OnPieceClicked(PuzzlePiece clickedPiece)
    {
        if (firstSelectedPiece == null)
        {
            // 选中第一个拼图块（高亮）
            firstSelectedPiece = clickedPiece;
            firstSelectedPiece.SetSelected(true);
        }
        else if (firstSelectedPiece == clickedPiece)
        {
            // 点击已选中的拼图块，取消选中
            firstSelectedPiece.SetSelected(false);
            firstSelectedPiece = null;
        }
        else
        {
            // 选中第二个拼图块，交换两者
            firstSelectedPiece.SwapPiece(clickedPiece);
            firstSelectedPiece.SetSelected(false);
            firstSelectedPiece = null;
        }
    }

    // 验证拼图是否完成
    private void CheckPuzzle()
    {
        bool isCompleted = true;
        // 检查所有拼图块的当前索引是否等于正确索引
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            if (puzzlePieces[i].currentIndex != puzzlePieces[i].correctIndex)
            {
                isCompleted = false;
                break;
            }
        }

        if (isCompleted)
        {
            Debug.Log("拼图完成！获得碎片！");
            GiveFragmentReward(); // 发放碎片奖励
            ClosePuzzlePanel(); // 关闭面板
        }
        else
        {
            // 错误提示（1秒后消失）
            errorText.text = "拼图未完成！请继续调整！";
            ShowErrorThenHide(1f);
        }
    }

    // 发放碎片奖励（示例：在宝箱位置生成碎片预制体）
    private void GiveFragmentReward()
    {
        if (fragmentPrefab != null && currentChest != null)
        {
            Instantiate(fragmentPrefab, currentChest.transform.position + Vector3.up, Quaternion.identity);
        }
    }

    // 错误提示显示后延迟隐藏（复用密码谜题的协程逻辑）
    private void ShowErrorThenHide(float delay)
    {
        errorText.gameObject.SetActive(true);
        if (hideErrorCoroutine != null) StopCoroutine(hideErrorCoroutine);
        hideErrorCoroutine = StartCoroutine(HideErrorCoroutine(delay));
    }

    private IEnumerator HideErrorCoroutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (errorText != null) errorText.gameObject.SetActive(false);
        hideErrorCoroutine = null;
    }

    // 关闭拼图面板（完成/超时后调用）
    public void ClosePuzzlePanel()
    {
        puzzlePanel1.SetActive(false);
        currentChest.LockChest1(); // 锁定Chest1，不可再打开
        timer.StopCountdown(); // 停止倒计时
        firstSelectedPiece = null; // 重置选中状态

        // 新增：关闭UI后，重新锁定鼠标并隐藏（恢复玩家移动控制）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 倒计时结束回调（由CountdownTimer调用）
    public void OnTimeEnd()
    {
        Debug.Log("拼图倒计时结束！");
        ClosePuzzlePanel();
    }
}