using UnityEngine;

// 单段剧情的数据结构（可在检视面板中编辑）
[System.Serializable]
public class StorySegment
{
    public Sprite bgSprite;       // 背景图片
    public string storyText;      // 剧情文本
    public float textSpeed = 0.05f; // 打字机速度
}

// 整段剧情的配置管理器
public class StoryData : MonoBehaviour
{
    public StorySegment[] storySegments; 
}