using UnityEngine;

public class Player_AirDownState : PlayerStateBase
{
    private enum AirDownChildState
    { 
        Loop,
        End
    }

    private float playEndAnimationHeight = 2f; 
    // 增加下落速度检测阈值
    private float verticalVelocityThreshold = -5f; 
    private float moveSpeedForAirDown=2f;
    private bool needEndAnimation;
    private AirDownChildState airDownState;
    private AirDownChildState AirDownState
    {
        get => airDownState;
        set {
            airDownState = value;
            switch (airDownState)
            {
                case AirDownChildState.Loop:
                    player.PlayAnimation("JumpLoop");
                    break;
                case AirDownChildState.End:
                    //player.PlayAnimation("JumpEnd");
                    break;
            }
        }
    }

    public override void Enter()
    {
        needEndAnimation = false;
        AirDownState = AirDownChildState.Loop;
        player.Player_Model.SetRootMotionAction(OnRootMotion);
    }

    public override void Update()
    {
                if (player.CharacterController == null) return;
        if (Input.GetKeyDown(KeyCode.Tab)&&!player.isCollidingWithTornado)
        {
            player.ChangeState(PlayerState.Draw);
            return;
        }
        
        if(player.verticalVelocity>player.gravity)
        {  
            // 持续应用重力
            player.verticalVelocity +=2* player.gravity * Time.deltaTime;
        }
        else
        {
            player.verticalVelocity = player.gravity;
        }
        Vector3 rayStart = player.transform.position +
                          Vector3.up * 0.2f;
        needEndAnimation = Physics.SphereCast(rayStart, 0.3f, Vector3.down, out _, playEndAnimationHeight);
        switch (airDownState)
        {
            case AirDownChildState.Loop:
                if (needEndAnimation  && player.verticalVelocity <= verticalVelocityThreshold )
                {
                    AirDownState = AirDownChildState.End;
                }
                else
                {
                    if (player.CharacterController.isGrounded)
                    {
                        player.ChangeState(PlayerState.Idle);
                        return;
                    }
                }

                break;
            case AirDownChildState.End:
                        // 此时依然踩空了，继续下坠
                        if (player.CharacterController.isGrounded == false)
                        {
                            AirDownState = AirDownChildState.Loop;
                        }
                        else
                        {
                            player.ChangeState(PlayerState.Idle);
                        }

                break;
        }
    }


    private void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        float shiftScale = Input.GetKey(KeyCode.LeftShift) ? player.shiftScale : 1f;
        // 处理控制位移
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 motion = new Vector3(0, player.verticalVelocity * Time.deltaTime, 0);
        if (h != 0 || v != 0)
        {
            Vector3 input = new Vector3(h, 0, v);
            Vector3 dir = Camera.main.transform.TransformDirection(input);
            motion.x = moveSpeedForAirDown * Time.deltaTime * dir.x *shiftScale;
            motion.z = moveSpeedForAirDown * Time.deltaTime * dir.z *shiftScale;
            // 处理旋转
            // 获取相机的旋转值 y
            float y = Camera.main.transform.rotation.eulerAngles.y;
            // 让四元数和向量相乘：表示让这个向量按照这个四元数所表达的角度进行旋转后得到新的向量
            Vector3 targetDir = Quaternion.Euler(0, y, 0) * input;
            player.Player_Model.transform.rotation = Quaternion.Slerp(player.Player_Model.transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * player.rotateSpeed);
        }

        player.CharacterController.Move(motion+deltaPosition);
    }
}
