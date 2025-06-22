using UnityEngine;

public class PlayerStateBase : StateBase
{
    protected Player_Controller player;
    public override void Init(IStateMachineOwner owner)
    {
        base.Init(owner);
        player = (Player_Controller)owner;
    }

    protected virtual bool CheckAnimatorStateName(string stateName,out float normalizedtime)
    {
        AnimatorStateInfo info= player.Player_Model.Animator.GetCurrentAnimatorStateInfo(0);
        normalizedtime = info.normalizedTime;
        return info.IsName(stateName);
    }
}
