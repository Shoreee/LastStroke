using UnityEngine;

public class Boss_MoveState : BossStateBase
{
    private enum MoveChildState
    {
        Move,
        Stop
    }
    private float walk2RunTransition; // 0~1
    private MoveChildState moveState;
    private float shiftScale;
    public float moveAniSpeed = 1f;
    public float runAniSpeed = 1f;
    private MoveChildState MoveState
    {
        get => moveState;
        set {
            moveState = value;
            // 状态进入
            switch (moveState)
            {
                case MoveChildState.Move:
                    boss.PlayAnimation("Move");
                    break;
                case MoveChildState.Stop:
                    boss.PlayAnimation("RunStop");
                    break;
            }
        }
    }

    private float stopTransition = 0f; // 新增变量用于控制急停时的逐渐减速

    public override void Enter()
    {
        MoveState = MoveChildState.Move;
        boss.Boss_Model.SetRootMotionAction(OnRootMotion);
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

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            boss.ChangeState(BossState.Absorb);
            return;
        }

        switch (moveState)
        {
            case MoveChildState.Move:
                MoveOnUpdate();
                break;
            case MoveChildState.Stop:
                StopOnUpdate();
                break;
        }
    }

    private void MoveOnUpdate()
    {
        // 不输入切回待机
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float rawH = Input.GetAxisRaw("Horizontal");
        float rawV = Input.GetAxisRaw("Vertical");
        if (rawH == 0 && rawV == 0) // 松开按键
        {
            if (walk2RunTransition > 0.4f)
            {
                // 进入急停
                MoveState = MoveChildState.Stop;
                return;
            }
            else if (h == 0 && v == 0)
            {
                boss.ChangeState(BossState.Idle);
                return;
            }
        }
        else
        {
            // 处理走到跑
            if (Input.GetKey(KeyCode.LeftShift))
            {
                walk2RunTransition = Mathf.Clamp(walk2RunTransition + Time.deltaTime * boss.walk2RunTransition, 0, 1); // 约束
                shiftScale = boss.shiftScale;
            }
            else // 跑到走
            {
                walk2RunTransition = Mathf.Clamp(walk2RunTransition - Time.deltaTime * boss.walk2RunTransition, 0, 1); // 约束
                shiftScale = 1f;
            }

            boss.Boss_Model.Animator.SetFloat("Move", walk2RunTransition);
            // 控制播放速度
            boss.Boss_Model.Animator.speed = Mathf.Lerp(moveAniSpeed, runAniSpeed, walk2RunTransition);

            // 旋转
            Vector3 input = new Vector3(h, 0, v);
            float y = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 targetDir = Quaternion.Euler(0, y, 0) * input;
            boss.Boss_Model.transform.rotation = Quaternion.Slerp(boss.Boss_Model.transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * boss.rotateSpeed);
        }
    }

    private void StopOnUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h != 0 || v != 0)
        {
            Vector3 input = new Vector3(h, 0, v);
            float y = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 targetDir = Quaternion.Euler(0, y, 0) * input;
            boss.Boss_Model.transform.rotation = Quaternion.Slerp(boss.Boss_Model.transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * boss.rotateSpeed);
        }


        // 当急停完成时，进入待机状态
        if (CheckAnimatorStateName("RunStop", out float animationTime))
        {
            stopTransition = 1.2f *animationTime;//设置速度衰减系数为1.2
            if (animationTime >= 1)
            {
                boss.ChangeState(BossState.Idle);
            }
        }
    }

    public override void Exit()
    {
        walk2RunTransition = 0;
        stopTransition = 0f; // 退出时重置急停过渡
        boss.Boss_Model.ClearRootMotionAction();
        boss.Boss_Model.Animator.speed = 1;
        shiftScale = 1f;
    }

    private void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        deltaPosition = boss.Boss_Model.transform.forward * boss.moveSpeed * boss.currentSpeedMultiplier* shiftScale * Time.deltaTime;
        deltaPosition.y = boss.gravity * Time.deltaTime;
                // 如果正在急停，逐渐减少 root motion
        if (stopTransition < 1f)
        {
            deltaPosition *= (1f - stopTransition); // 根运动按过渡系数衰减
        }
        else
        {
            deltaPosition *= 0;
        }
        boss.CharacterController.Move(deltaPosition);
    }
}
