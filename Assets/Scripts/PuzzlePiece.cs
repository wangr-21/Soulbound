using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzlePiece : MonoBehaviour, IPointerClickHandler
{
    public int correctIndex; // 拼图块的正确位置索引（0~8，编辑模式赋值）
    public int currentIndex; // 拼图块的当前位置索引（动态更新）
    private Image pieceImage;
    private bool isSelected = false; // 是否被选中（用于交换）

    void Awake()
    {
        pieceImage = GetComponent<Image>();
    }

    // UI点击事件（必须实现IPointerClickHandler）
    public void OnPointerClick(PointerEventData eventData)
    {
        PuzzleManager1.Instance.OnPieceClicked(this); // 通知管理器处理点击
    }

    // 选中状态切换（高亮效果）
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        pieceImage.color = selected ? new Color(0.8f, 0.8f, 1f) : Color.white; // 选中时淡蓝高亮
    }

    // 交换拼图块的Sprite和索引（用于交换逻辑）
    public void SwapPiece(PuzzlePiece otherPiece)
    {
        // 交换Sprite
        Sprite tempSprite = pieceImage.sprite;
        pieceImage.sprite = otherPiece.pieceImage.sprite;
        otherPiece.pieceImage.sprite = tempSprite;

        // 交换正确索引（关键：确保验证时判断正确）
        int tempIndex = correctIndex;
        correctIndex = otherPiece.correctIndex;
        otherPiece.correctIndex = tempIndex;
    }
}