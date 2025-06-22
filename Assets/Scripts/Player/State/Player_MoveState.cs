using UnityEngine;

public class Player_MoveState : PlayerStateBase
{
    private enum MoveChildState
    {
        Move,
        Stop
    }

    private float walk2RunTransition; // 0~1
    private MoveChildState moveState;
    private float shiftScale;
    public float moveAniSpeed = 2f;
    public float runAniSpeed = 1f;
    private MoveChildState MoveState
    {
        get => moveState;
        set {
            moveState = value;
            // ״̬����
            switch (moveState)
            {
                case MoveChildState.Move:
                    player.PlayAnimation("Move");
                    break;
                case MoveChildState.Stop:
                    player.PlayAnimation("RunStop");
                    break;
            }
        }
    }

    private float stopTransition = 0f; // �����������ڿ��Ƽ�ͣʱ���𽥼���

    public override void Enter()
    {
        MoveState = MoveChildState.Move;
        player.Player_Model.SetRootMotionAction(OnRootMotion);
    }

    public override void Update()
    {
                if (player.CharacterController == null) return;
                // �������
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
        // �������лش���
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float rawH = Input.GetAxisRaw("Horizontal");
        float rawV = Input.GetAxisRaw("Vertical");
        if (rawH == 0 && rawV == 0) // �ɿ�����
        {
            if (walk2RunTransition > 0.4f)
            {
                // ���뼱ͣ
                MoveState = MoveChildState.Stop;
                return;
            }
            else if (h == 0 && v == 0)
            {
                player.ChangeState(PlayerState.Idle);
                return;
            }
        }
        else
        {
            // �����ߵ���
            if (Input.GetKey(KeyCode.LeftShift))
            {
                walk2RunTransition = Mathf.Clamp(walk2RunTransition + Time.deltaTime * player.walk2RunTransition, 0, 1); // Լ��
                shiftScale = player.shiftScale;
            }
            else // �ܵ���
            {
                walk2RunTransition = Mathf.Clamp(walk2RunTransition - Time.deltaTime * player.walk2RunTransition, 0, 1); // Լ��
                shiftScale = 1f;
            }

            player.Player_Model.Animator.SetFloat("Move", walk2RunTransition);
            // ���Ʋ����ٶ�
            player.Player_Model.Animator.speed = Mathf.Lerp(moveAniSpeed, runAniSpeed, walk2RunTransition);

            // ��ת
            Vector3 input = new Vector3(h, 0, v);
            float y = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 targetDir = Quaternion.Euler(0, y, 0) * input;
            player.Player_Model.transform.rotation = Quaternion.Slerp(player.Player_Model.transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * player.rotateSpeed);
        }
    }

    private void StopOnUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0, v);
        float y = Camera.main.transform.rotation.eulerAngles.y;
        Vector3 targetDir = Quaternion.Euler(0, y, 0) * input;
        player.Player_Model.transform.rotation = Quaternion.Slerp(player.Player_Model.transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * player.rotateSpeed);

        // ����ͣ���ʱ���������״̬
        if (CheckAnimatorStateName("RunStop", out float animationTime))
        {
            stopTransition = 1.2f *animationTime;//�����ٶ�˥��ϵ��Ϊ1.2
            if (animationTime >= 1)
            {
                player.ChangeState(PlayerState.Idle);
            }
        }
    }

    public override void Exit()
    {
        walk2RunTransition = 0;
        stopTransition = 0f; // �˳�ʱ���ü�ͣ����
        player.Player_Model.ClearRootMotionAction();
        player.Player_Model.Animator.speed = 1;
        shiftScale = 1f;
    }

    private void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        deltaPosition = player.Player_Model.transform.forward * player.moveSpeed * player.currentSpeedMultiplier* shiftScale * Time.deltaTime;
        deltaPosition.y = player.gravity * Time.deltaTime;
                // ������ڼ�ͣ���𽥼��� root motion
        if (stopTransition < 1f)
        {
            deltaPosition *= (1f - stopTransition); // ���˶�������ϵ��˥��
        }
        else
        {
            deltaPosition *= 0;
        }
        player.CharacterController.Move(deltaPosition);
    }
}
