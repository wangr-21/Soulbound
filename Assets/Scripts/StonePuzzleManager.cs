using UnityEngine;
using System.Collections;
using TMPro;

public class StonePuzzleManager : MonoBehaviour
{
    public static StonePuzzleManager Instance;

    public Stone[] stones; // 按顺序排列的石头数组
    public float flashDuration = 0.5f; // 每块石头闪光持续时间
    public float waitAfterSequence = 3f; // 序列结束后等待时间
    public float puzzleTimeLimit = 180f; // 3分钟倒计时
    public TextMeshProUGUI timerText; // 显示倒计时的UI
    public GameObject fragmentReward; // 碎片奖励预制体

    private int currentSequenceIndex = 0;
    private int playerSequenceIndex = 0;
    private bool isShowingSequence = true;
    private bool isPuzzleActive = false;
    private bool puzzleCompleted = false;
    private float remainingTime;
    private int[] correctSequence; // 正确的顺序

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // 初始化正确顺序：A(0)-C(2)-B(1)-A(0)-E(4)-D(3)-A(0)
        correctSequence = new int[] { 0, 2, 1, 0, 4, 3, 0 };

        // 开始显示序列
        StartCoroutine(ShowSequence());
    }

    private void Update()
    {
        if (isPuzzleActive && !puzzleCompleted)
        {
            UpdateTimer();
        }
    }

    // 显示石头闪光序列
    private IEnumerator ShowSequence()
    {
        while (isShowingSequence && !puzzleCompleted)
        {
            // 按新定义的顺序闪光（遍历correctSequence数组）
            for (int i = 0; i < correctSequence.Length; i++)
            {
                int stoneIndex = correctSequence[i]; // 获取当前步骤的石头索引
                stones[stoneIndex].ActivateGlow();   // 激活对应石头的发光
                yield return new WaitForSeconds(flashDuration);
                stones[stoneIndex].DeactivateGlow(); // 关闭发光
                yield return new WaitForSeconds(0.1f); // 闪光间隔
            }

            // 等待一段时间后重复序列
            yield return new WaitForSeconds(waitAfterSequence);
        }
    }

    // 玩家踩上石头时调用
    public void OnPlayerStepOnStone(int stoneIndex)
    {
        // 如果谜题已完成，不再响应
        if (puzzleCompleted)
            return;

        // 第一次踩第一块石头，开始谜题
        if (!isPuzzleActive && stoneIndex == correctSequence[0])
        {
            StartPuzzle();
            return;
        }

        // 谜题进行中，检查顺序
        if (isPuzzleActive)
        {
            // 检查是否按正确顺序踩石头
            if (stoneIndex == correctSequence[playerSequenceIndex])
            {
                // 正确，点亮当前石头
                stones[stoneIndex].ActivateGlow();
                StartCoroutine(DeactivateAfterDelay(stoneIndex, 0.5f));

                playerSequenceIndex++;

                // 检查是否完成整个序列
                if (playerSequenceIndex >= correctSequence.Length)
                {
                    CompletePuzzleSuccess();
                }
            }
            else
            {
                // 错误，重新开始
                playerSequenceIndex = 0;
                // 可以添加错误提示效果
            }
        }
    }

    // 延迟关闭石头发光
    private IEnumerator DeactivateAfterDelay(int stoneIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        stones[stoneIndex].DeactivateGlow();
    }

    // 开始谜题
    private void StartPuzzle()
    {
        isShowingSequence = false;
        isPuzzleActive = true;
        remainingTime = puzzleTimeLimit;
        playerSequenceIndex = 0;
        
        // 停止序列显示协程
        StopAllCoroutines();

        // 隐藏所有石头的发光
        foreach (var stone in stones)
        {
            stone.DeactivateGlow();
        }

        Debug.Log("石头谜题开始！请按照记忆中的顺序踩石头");
    }

    // 更新倒计时
    private void UpdateTimer()
    {
        if (remainingTime <= 0) return; // 防止重复触发

        remainingTime -= Time.deltaTime;

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = $"时间: {minutes:D2}:{seconds:D2}";
        }

        if (remainingTime <= 0)
        {
            remainingTime = 0; // 确保显示为00:00
            CompletePuzzleFailure();
        }
    }

    // 成功完成谜题
    private void CompletePuzzleSuccess()
    {
        isPuzzleActive = false;
        puzzleCompleted = true;
        Debug.Log("恭喜！成功完成石头谜题！");

        if (fragmentReward != null)
        {
            // 可以修改为在当前管理器位置生成或其他逻辑
            Instantiate(fragmentReward, transform.position, Quaternion.identity);
        }

        // 可以添加成功效果和音效
        GameManager.Instance.AddScore(100); // 增加分数
    }

    private void CompletePuzzleFailure()
    {
        isPuzzleActive = false;
        puzzleCompleted = true;
        Debug.Log("时间到！石头谜题失败");

        // 可以添加失败时的视觉效果（如所有石头闪烁红色）
        StartCoroutine(FailFeedback());

        foreach (var stone in stones)
        {
            stone.DeactivateGlow();
        }
    }

    // 失败反馈协程
    private IEnumerator FailFeedback()
    {
        for (int i = 0; i < 3; i++) // 闪烁3次
        {
            foreach (var stone in stones)
            {
                stone.ActivateGlow(); // 如果有红色材质可以在这里替换
            }
            yield return new WaitForSeconds(0.3f);
            foreach (var stone in stones)
            {
                stone.DeactivateGlow();
            }
            yield return new WaitForSeconds(0.3f);
        }
    }
}