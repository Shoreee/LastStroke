
using UnityEngine;
public interface ISkillOwner
{
    Transform ModelTransform { get; }
    void StartSkillHit(int weaponIndex);

    void StopSkillHit(int weaponIndex);

    void SkillCanSwitch();

    void OnHit(IHurt target, Vector3 hitPostion);
}