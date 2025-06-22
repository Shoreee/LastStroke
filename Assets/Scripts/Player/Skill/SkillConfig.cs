using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Skill")]
public class SkillConfig : ScriptableObject
{
    // 技能的动画名称
    public string AnimationName;
    public Skill_ReleaseData ReleaseData;
    public Skill_AttackData[] AttackData;
}

/// <summary>
/// 技能释放数据
/// </summary>
[Serializable]
public class Skill_ReleaseData
{
    // 播放粒子 / 产生的游戏物体
    public Skill_SpawnObj SpawnObj;
    // 音效
    public AudioClip AudioClip;
    // 技能运行时是否可以旋转
    public bool CanRotate;
}

/// <summary>
/// 技能攻击数据
/// </summary>
[Serializable]
public class Skill_AttackData
{
    // 播放粒子 / 产生的游戏物体
    public Skill_SpawnObj SpawnObj;
    // 音效
    public AudioClip AudioClip;

    // 命中数据
    public Skill_HitData HitData;
    // 屏幕震动
    public float ScreenImpulseValue;
    // 色差效果
    public float ChromaticAberrationValue;
    // 卡肉效果
    public float FreezeFrameTime;
    // 时停
    public float FreezeGameTime;
    // 命中效果
    public SkillHitEFConfig SkillHitEFConfig;
}


/// <summary>
/// 技能命中数据
/// </summary>
[Serializable]
public class Skill_HitData
{
    // 伤害数值
    public float DamgeValue;
    // 硬直时间
    public float HardTime;
    // 击飞、击退的程度
    public Vector3 RepelVelocity;
    // 是否击倒
    public bool Down;
    // 击飞、击退的过渡时间
    public float RepelTime;
}


/// <summary>
/// 技能产生物体
/// </summary>
[Serializable]
public class Skill_SpawnObj
{
    // 生成的预制体
    public GameObject Prefab;
    // 生成的音效
    public AudioClip AudioClip;
    // 位置
    public Vector3 Position;
    // 旋转
    public Vector3 Rotation;
    // 缩放
    public Vector3 Scale = Vector3.one;
    // 延迟时间
    public float Time;
}