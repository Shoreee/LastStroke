using UnityEngine;

public class Player_JumpState : PlayerStateBase
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
        player.verticalVelocity = 0;
        hasAppliedInitialJump = false; // 重置标记
        peakTime = Mathf.Sqrt(player.jumpPower / (0.5f * -player.gravity));
        currentAirTime = 0f;
        IsMoving = 0f;

        player.PlayAnimation("JumpStart");
        player.Player_Model.Animator.speed = jumpAniSpeed;
        player.Player_Model.SetRootMotionAction(OnRootMotion);
    }

    public override void Update()
    {
        if (player.CharacterController == null) return;
        HandleAirMovement();
        if(CheckAnimatorStateName("JumpStart", out float animationTime))
        {
            // 仅在动画开始时应用一次跳跃初速度
            if (!hasAppliedInitialJump && animationTime > 0.1f)
            {
                player.verticalVelocity = player.jumpPower;
                hasAppliedInitialJump = true;
            }

            // 持续应用重力
            player.verticalVelocity +=2* player.gravity * Time.deltaTime;

            // 状态切换逻辑保持不变
            if (animationTime>=1)
            {
                player.ChangeState(PlayerState.AirDown);
                return;
            }
            

        }
    }

    private void HandleAirMovement()
    {
        shiftScale = Input.GetKey(KeyCode.LeftShift) ? player.shiftScale : 1f;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0, v);

        if (input.magnitude > 0)
        {
            float yaw = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 targetDir = Quaternion.Euler(0, yaw, 0) * input;
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);

            player.Player_Model.transform.rotation = Quaternion.Slerp(
                player.Player_Model.transform.rotation,
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
        player.Player_Model.Animator.speed = 1f;
        player.Player_Model.ClearRootMotionAction();
    }

    private void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        Vector3 horizontalMove = player.Player_Model.transform.forward * 
                               player.moveSpeedForJump * 
                               shiftScale * 
                               IsMoving * 
                               Time.deltaTime;

        // 修正垂直移动计算
        Vector3 verticalMove = Vector3.up * player.verticalVelocity * Time.deltaTime;

        player.CharacterController.Move(horizontalMove + verticalMove + deltaPosition);
    }
}