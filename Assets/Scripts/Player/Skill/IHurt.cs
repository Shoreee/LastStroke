
using UnityEngine;

public interface IHurt
{
    void Hurt(Skill_HitData hitData, ISkillOwner hurtSource);
}
