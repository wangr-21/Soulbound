using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RecallTrigger : MonoBehaviour
{
    [Header("触发设置")]
    public float triggerRange = 3f;
    public TMP_Text promptText;

    private Transform player;
    private bool isInRange;
    private bool isRecalling;

    void Start()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null || promptText == null || isRecalling) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= triggerRange)
        {
            isInRange = true;
            promptText.gameObject.SetActive(true);
            promptText.text = "按 B 键进入回忆";

            if (Input.GetKeyDown(KeyCode.B))
            {
                EnterRecall();
            }
        }
        else if (isInRange)
        {
            isInRange = false;
            promptText.gameObject.SetActive(false);
        }
    }

    void EnterRecall()
    {
        isRecalling = true;

        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 冻结主场景
        Time.timeScale = 1f;

        // ★ 主场景音乐淡出
        MainBGMController.Instance?.FadeOut(0.5f);

        // Additive 加载回忆场景
        SceneManager.LoadScene("DowlScene", LoadSceneMode.Additive);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRange);
    }
}
