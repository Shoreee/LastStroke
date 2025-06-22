using UnityEngine;

public class Boss_JumpState : BossStateBase
{
    public float jumpAniSpeed = 1f;
    private float shiftScale;
    private float IsMoving = 1;


    private bool hasAppliedInitialJump; // 新增：标记初始跳跃力是否已应用
    private float peakTime;   
    private float flightTimeThreshold = 0.5f;

    private float currentAirTime;
    public AnimationCurve jumpCurve = new AnimationCurve(
        new Keyframe(0, 12f),
        new Keyframe(0.7f, 8f),
        new Keyframe(1f, 0f)
    );

    public override void Enter()
    {
        boss.verticalVelocity = 0;
        hasAppliedInitialJump = false; // 重置标记
        peakTime = Mathf.Sqrt(boss.jumpPower / (0.5f * -boss.gravity));
        currentAirTime = 0f;
        IsMoving = 0f;

        boss.PlayAnimation("JumpStart");
        boss.Boss_Model.Animator.speed = jumpAniSpeed;
        boss.Boss_Model.SetRootMotionAction(OnRootMotion);
    }

    public override void Update()
    {
        if (boss.CharacterController == null) return;
        HandleAirMovement();
        if(CheckAnimatorStateName("JumpStart", out float animationTime))
        {
            // 仅在动画开始时应用一次跳跃初速度
            if (!hasAppliedInitialJump && animationTime > 0.1f)
            {
                boss.verticalVelocity = boss.jumpPower;
                hasAppliedInitialJump = true;
            }

            // 持续应用重力
            boss.verticalVelocity +=2* boss.gravity * Time.deltaTime;

            // 状态切换逻辑保持不变
            if (animationTime>=1)
            {
                boss.ChangeState(BossState.AirDown);
                return;
            }
            

        }
    }

    private void HandleAirMovement()
    {
        shiftScale = Input.GetKey(KeyCode.LeftShift) ? boss.shiftScale : 1f;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0, v);

        if (input.magnitude > 0)
        {
            float yaw = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 targetDir = Quaternion.Euler(0, yaw, 0) * input;
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);

            boss.Boss_Model.transform.rotation = Quaternion.Slerp(
                boss.Boss_Model.transform.rotation,
                targetRotation,
                Time.deltaTime
            );
            IsMoving = 1f;
        }
        else
        {
            IsMoving = 0f;
        }
    }

    public override void Exit()
    {
        currentAirTime=0;
        boss.Boss_Model.Animator.speed = 1f;
        boss.Boss_Model.ClearRootMotionAction();
    }

    private void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        Vector3 horizontalMove = boss.Boss_Model.transform.forward * 
                               boss.moveSpeedForJump * 
                               shiftScale * 
                               IsMoving * 
                               Time.deltaTime;

        // 修正垂直移动计算
        Vector3 verticalMove = Vector3.up * boss.verticalVelocity * Time.deltaTime;

        boss.CharacterController.Move(horizontalMove + verticalMove + deltaPosition);
    }
}