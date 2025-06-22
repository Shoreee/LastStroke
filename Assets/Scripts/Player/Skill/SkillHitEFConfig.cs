using UnityEngine;

[CreateAssetMenu(menuName = "Config/SkillHitEFConfig")]
public class SkillHitEFConfig:ScriptableObject
{
    // 产生的粒子物体
    public Skill_SpawnObj SpawnObject;
    // 命中时音效
    public AudioClip AudioClip;
}
