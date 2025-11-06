using UnityEngine;

// 这是一个接口，不是普通的类
public interface IPossessable
{
    // 当被附身时调用
    void OnPossess();

    // 当脱离时调用  
    void OnRelease();

    // 返回这个对象的能力描述（用于UI提示）
    string GetAbilityDescription();

    // 当被附身时的每帧更新
    void PossessedUpdate();
}