using UnityEngine;

public class Boss_AirDownState : BossStateBase
{
    private enum AirDownChildState
    { 
        Loop,
        End
    }

    private float playEndAnimationHeight = 2f; 
    // 增加下落速度检测阈值
    private float verticalVelocityThreshold = -3f; 
    private float moveSpeedForAirDown=2.8f;
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
                    boss.PlayAnimation("JumpLoop");
                    break;
                case AirDownChildState.End:
                    boss.PlayAnimation("JumpEnd");
                    break;
            }
        }
    }

    public override void Enter()
    {
        needEndAnimation = false;
        AirDownState = AirDownChildState.Loop;
        boss.Boss_Model.SetRootMotionAction(OnRootMotion);
    }

    public override void Update()
    {
        if (boss.CharacterController == null) return;
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            boss.ChangeState(BossState.Absorb);
            return;
        }
        
        if(boss.verticalVelocity>boss.gravity)
        {  
            // 持续应用重力
            boss.verticalVelocity +=2* boss.gravity * Time.deltaTime;
        }
        else
        {
            boss.verticalVelocity = boss.gravity;
        }
        Vector3 rayStart = boss.transform.position +
                          Vector3.up * 0.2f;
        needEndAnimation = Physics.SphereCast(rayStart, 0.3f, Vector3.down, out _, playEndAnimationHeight);
        switch (airDownState)
        {
            case AirDownChildState.Loop:
                //AirControll();
                if (needEndAnimation  && boss.verticalVelocity <= verticalVelocityThreshold )
                {
                    AirDownState = AirDownChildState.End;
                }
                else
                {
                    if (boss.CharacterController.isGrounded)
                    {
                        boss.ChangeState(BossState.Idle);
                        return;
                    }
                }
                //AirControll();

                break;
            case AirDownChildState.End:
                //AirControll();
                        // 此时依然踩空了，继续下坠
                        if (boss.CharacterController.isGrounded == false)
                        {
                            AirDownState = AirDownChildState.Loop;
                        }
                        else
                        {
                            boss.ChangeState(BossState.Idle);
                        }

                break;
        }
    }


    private void AirControll()
    {
        float shiftScale = Input.GetKey(KeyCode.LeftShift) ? boss.shiftScale : 1f;
        // 处理控制位移
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 motion = new Vector3(0, boss.verticalVelocity * Time.deltaTime, 0);
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
            boss.Boss_Model.transform.rotation = Quaternion.Slerp(boss.Boss_Model.transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * boss.rotateSpeed);
        }

        boss.CharacterController.Move(motion);
    }
    private void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        float shiftScale = Input.GetKey(KeyCode.LeftShift) ? boss.shiftScale : 1f;
        // 处理控制位移
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 motion = new Vector3(0, boss.verticalVelocity * Time.deltaTime, 0);
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
            boss.Boss_Model.transform.rotation = Quaternion.Slerp(boss.Boss_Model.transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * boss.rotateSpeed);
        }

        boss.CharacterController.Move(motion+ deltaPosition);
    }
}
