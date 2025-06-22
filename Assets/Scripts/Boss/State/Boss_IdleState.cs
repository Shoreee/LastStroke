using UnityEngine;

public class Boss_IdleState : BossStateBase
{
    public override void Enter()
    {
        boss.PlayAnimation("Idle");
    }

    public override void Update()
    {        
        if (boss.CharacterController == null) return;
                                // 检测下落
        if (boss.CharacterController.isGrounded == false)
        {
            boss.ChangeState(BossState.AirDown);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space) && boss.CharacterController.isGrounded)
        {
            boss.ChangeState(BossState.Jump);
        }
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            boss.ChangeState(BossState.Absorb);
            return;
        }
        boss.CharacterController.Move(new Vector3(0,boss.gravity*Time.deltaTime,0));
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if(h!=0||v!=0)
        {
            boss.ChangeState(BossState.Move);
            return;
        }
    }
}
