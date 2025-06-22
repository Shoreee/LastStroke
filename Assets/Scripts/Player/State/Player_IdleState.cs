using UnityEngine;

public class Player_IdleState : PlayerStateBase
{
    public override void Enter()
    {
        player.PlayAnimation("Idle");
    }

    public override void Update()
    {
        if (player.CharacterController == null) return;
                        // 检测下落
        if (player.CharacterController.isGrounded == false)
        {
            player.ChangeState(PlayerState.AirDown);
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.Tab)&&!player.isCollidingWithTornado)
        {
            player.ChangeState(PlayerState.Draw);
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space) && player.CharacterController.isGrounded)
        {
            player.ChangeState(PlayerState.Jump);
        }
        // 检测是否需要切换到 Move 状态
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h != 0 || v != 0)
        {
            player.ChangeState(PlayerState.Move);
            return;
        }

        // 保持 Idle 状态的重力效果
        player.CharacterController.Move(new Vector3(0, player.gravity * Time.deltaTime, 0));
    }
}