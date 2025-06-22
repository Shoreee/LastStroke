using UnityEngine;

public class BossStateBase : StateBase
{
    protected Boss_Controller boss;
    public override void Init(IStateMachineOwner owner)
    {
        base.Init(owner);
        boss = (Boss_Controller)owner;
    }

    protected virtual bool CheckAnimatorStateName(string stateName,out float normalizedtime)
    {
        AnimatorStateInfo info= boss.Boss_Model.Animator.GetCurrentAnimatorStateInfo(0);
        normalizedtime = info.normalizedTime;
        return info.IsName(stateName);
    }
}
