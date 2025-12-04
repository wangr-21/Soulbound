using UnityEngine;

[System.Serializable]
public class StorySegment
{
    public Sprite bgSprite;       // 背景图片
    public string storyText;      // 剧情文本
    public float textSpeed = 0.05f; // 打字机速度
}

// 改为继承 ScriptableObject
[CreateAssetMenu(fileName = "NewStoryData", menuName = "Game/Story Data")]
public class StoryData : ScriptableObject
{
    public StorySegment[] storySegments;
}