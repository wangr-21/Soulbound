using UnityEngine;
using System.Collections; // 新增：协程必需的命名空间
using System.Collections.Generic;
using TMPro; // 若用UI显示倒计时，需引入TextMeshPro

public class StonePuzzleManager : MonoBehaviour
{
    // 按触发顺序拖入所有石头
    public List<GameObject> stoneOrder;
    public float countdownDuration = 8f; // 倒计时时长
    // （可选）拖入UI文本显示剩余时间
    public TMP_Text countdownText;

    private int currentStoneIndex = 0;
    private float currentCountdown;
    private bool isCountingDown = false;

    void Start()
    {
        ResetPuzzle(); // 初始化谜题
    }


    void Update()
    {
        // 处理倒计时
        if (isCountingDown)
        {
            currentCountdown -= Time.deltaTime;
            UpdateCountdownUI(); // 更新UI显示

            if (currentCountdown <= 0)
            {
                Debug.Log("超时！谜题重置");
                ResetPuzzle();
            }
        }
    }


    // 玩家踩中石头时调用
    public void OnStoneStepped(GameObject steppedStone)
    {
        if (currentStoneIndex >= stoneOrder.Count) return;
        if (steppedStone != stoneOrder[currentStoneIndex]) return;

        ToggleStoneLight(steppedStone, false);
        isCountingDown = false;

        currentStoneIndex++;
        if (currentStoneIndex < stoneOrder.Count)
        {
            GameObject nextStone = stoneOrder[currentStoneIndex];
            nextStone.SetActive(true);
            ToggleStoneLight(nextStone, true);
            currentCountdown = countdownDuration;
            isCountingDown = true;
        }
        else
        {
            Debug.Log("石头谜题完成，获得1块碎片！");
            FragmentManager.Instance.AddFragment();
            // 替换为调用全局提示管理器
            UIMessageManager.Instance.ShowMessage("恭喜你获得1块碎片！", 1f);
            isCountingDown = false;
            UpdateCountdownUI(false);
        }
    }

    // 切换石头的发光状态
    private void ToggleStoneLight(GameObject stone, bool isOn)
    {
        Light stoneLight = stone.GetComponent<Light>();
        if (stoneLight != null)
        {
            stoneLight.enabled = isOn;
        }
        // （可选）若用材质自发光，替换为：
        // Renderer rend = stone.GetComponent<Renderer>();
        // rend.material.SetColor("_EmissionColor", isOn ? Color.white * 2 : Color.black);
    }


    // 重置谜题到初始状态
    private void ResetPuzzle()
    {
        currentStoneIndex = 0;
        isCountingDown = false;
        UpdateCountdownUI(false); // 隐藏倒计时UI

        // 关闭所有石头，仅激活第一个
        foreach (var stone in stoneOrder)
        {
            stone.SetActive(false);
            ToggleStoneLight(stone, false);
        }
        stoneOrder[currentStoneIndex].SetActive(true);
        ToggleStoneLight(stoneOrder[currentStoneIndex], true);
    }


    // （可选）更新倒计时UI
    private void UpdateCountdownUI(bool show = true)
    {
        if (countdownText == null) return;

        countdownText.gameObject.SetActive(show);
        if (show)
        {
            // 计算分和秒（例如8秒 → 00:08，12秒 → 00:12）
            int minutes = Mathf.FloorToInt(currentCountdown / 60);
            int seconds = Mathf.FloorToInt(currentCountdown % 60);
            // 格式化为“00:08”（不足2位补0）
            countdownText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }
    }
}