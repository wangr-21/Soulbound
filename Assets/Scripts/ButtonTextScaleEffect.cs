using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonTextScaleEffect : MonoBehaviour
{
    // 拖拽按钮下的Text组件到这里
    public TMPro.TextMeshProUGUI buttonText;
    // 缩小的比例（比如0.8表示缩小到80%）
    public float scaleDownRatio = 0.8f;
    // 动画持续时间
    public float effectDuration = 0.2f;

    // 按钮点击时触发的方法
    public void OnButtonClick()
    {
        StartCoroutine(ScaleTextEffect());
    }

    // 缩放动画的协程
    IEnumerator ScaleTextEffect()
    {
        // 记录原始缩放
        Vector3 originalScale = buttonText.transform.localScale;
        // 缩小文字
        buttonText.transform.localScale = originalScale * scaleDownRatio;
        // 等待指定时间
        yield return new WaitForSeconds(effectDuration);
        // 恢复原始缩放
        buttonText.transform.localScale = originalScale;
    }
}