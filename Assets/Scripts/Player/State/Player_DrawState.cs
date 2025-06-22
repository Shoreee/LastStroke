using UnityEngine;
using Cinemachine;
using System.Collections;

public class Player_DrawState : PlayerStateBase
{
    private string IDtemp;
    private enum DrawChildState
    {
        Start,
        Draw,
        DrawIdle,
        Stop
    }

    private DrawChildState drawState;
    private Coroutine activeTransition; // ���ڷ�ֹЭ���ص�
    private float rotateSpeedScale=1.5f;
    private float shiftScale;
    private Coroutine _delayedRecognizeCoroutine;


    private DrawChildState CurrentState
    {
        get => drawState;
        set
        {
            drawState = value;
            player.Player_Model.SetRootMotionAction(OnDrawRootMotion);
            switch (drawState)
            {
                case DrawChildState.Start:
                    player.PlayAnimation("DrawStart");
                    break;
                case DrawChildState.Draw:
                    player.PlayAnimation("Draw");
                    break;
                case DrawChildState.DrawIdle:
                    player.PlayAnimation("DrawIdle");
                    break;
                case DrawChildState.Stop:
                    player.PlayAnimation("DrawStop");
                    break;
            }
        }
    }

    public override void Enter()
    {
        IDtemp = player.roleID;
        if (activeTransition != null)
        {
            Player_Controller.Instance.StopCoroutine(activeTransition);
            activeTransition = null; // ���Э������
        }
        // �����������Э�̣��� FreeLook �� 2.5D��
        //Player_Controller.Instance.StartCoroutine(TransitionTo2Point5D(Player_Controller.Instance.twoPointFiveDVCam,Player_Controller.Instance.freeLookVCam,Player_Controller.Instance.cameraTransitionDuration));
        CurrentState = DrawChildState.Start;

        player.verticalVelocity = 0;

    }

    public override void Update()
    {
                if (player.CharacterController == null) return;
        HandleJump();
                        // �������
        //if (player.CharacterController.isGrounded == false)
        //{
        //    player.ChangeState(PlayerState.AirDown);
        //    return;
        //}

        if(player.isCollidingWithTornado)
        {
            CurrentState = DrawChildState.Stop;
        }
        if (Input.GetKeyDown(KeyCode.Tab) && CurrentState != DrawChildState.Stop)
        {
            CurrentState = DrawChildState.Stop;
            return;
        }
                    // �����ߵ���
        if (Input.GetKey(KeyCode.LeftShift))
        {
                shiftScale = player.shiftScale;
        }
            else // �ܵ���
        {

                shiftScale = 1f;
        }


        switch (CurrentState)
        {
            case DrawChildState.Start:
                HandleStartState();
                break;
            case DrawChildState.Draw:
                HandleDrawState();
                break;
            case DrawChildState.DrawIdle:
                HandleDrawIdleState();
                break;
            case DrawChildState.Stop:
                HandleStopState();
                break;
        }
    }

    private void HandleStartState()
    {
        // ��� DrawStart �����Ƿ�ӽ�����
        if (CheckAnimatorStateName("DrawStart", out float normalizedTime))
        {
            if (normalizedTime >= 0.95f)
            {
                // ����������״̬�л��� Draw �� DrawIdle
                if (Input.GetMouseButton(0))
                {
                    CurrentState = DrawChildState.Draw;
                }
                else
                {
                    CurrentState = DrawChildState.DrawIdle;
                }
            }
        }
        HandleRotation();
    }

    private void HandleDrawState()
{
    if (player.inkParticle != null)
        player.inkParticle.Play();

    if (!Input.GetMouseButton(0))
    {
        if (player.inkParticle != null)
            player.inkParticle.Stop();

        // 2) �����ӳ�ʶ��Э�̣����Ѵ�������ֹͣ��
        if (_delayedRecognizeCoroutine != null)
            player.StopCoroutine(_delayedRecognizeCoroutine);
        _delayedRecognizeCoroutine = player.StartCoroutine(DelayedRecognizeAfterParticlesDie());

        //player.WolfMagicColse();
        CurrentState = DrawChildState.DrawIdle;
        return;
    }

    HandleRotation();
}

    // �ȴ�����ϵͳ�����������������ٵ���ʶ��
    private IEnumerator DelayedRecognizeAfterParticlesDie()
    {
        // �ȴ�ֱ��û�л����ӣ��������ϵͳ
        while (player.inkParticle.IsAlive(false)) 
        {
            yield return null;
        }

        // һ��������������������ʶ��
        if (player.strokeRecognizor != null)
        {
            if (player.photonView.IsMine)
            {
                player.strokeRecognizor.AutoRecognize(IDtemp);
            }
        }

    }

    private void HandleDrawIdleState()
    {
        // ������������ʱ�л��� Draw ״̬���������ӣ�
        if (Input.GetMouseButtonDown(0))
        {
            CurrentState = DrawChildState.Draw;
            return;
        }
        HandleRotation();
    }

    private void HandleStopState()
    {
        if (player.inkParticle != null)
        {
            player.inkParticle.Stop();
        }
        if (activeTransition != null)
        {
            Player_Controller.Instance.StopCoroutine(activeTransition);
            activeTransition = null; // ���Э������
        }
        // �����������Э�̣��� 2.5D �� FreeLook��
        //Player_Controller.Instance.StartCoroutine(TransitionToFreeLook(Player_Controller.Instance.freeLookVCam,Player_Controller.Instance.twoPointFiveDVCam,Player_Controller.Instance.cameraTransitionDuration));
        // ��� DrawStop �����Ƿ񲥷����
        if (CheckAnimatorStateName("DrawStop", out float normalizedTime))
        {
            if (normalizedTime >= 0.95f)
            {
                if (player.strokeRecognizor != null) player.strokeRecognizor.AutoRecognize(IDtemp);
                player.ChangeState(PlayerState.Idle);
            }
        }
        HandleRotation();
    }

    private void HandleRotation()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0, v);
        if (input.magnitude > 0)
        {
            float cameraY = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 targetDir = Quaternion.Euler(0, cameraY, 0) * input;
            Quaternion targetRotation = Quaternion.Slerp(
                player.Player_Model.transform.rotation,
                Quaternion.LookRotation(targetDir),
                Time.deltaTime * player.rotateSpeed *rotateSpeedScale);
            
            if (Physics.Raycast(player.Player_Model.transform.position, Vector3.down, out RaycastHit hit))
            {
                targetRotation = Quaternion.Slerp(
                    targetRotation,
                    Quaternion.FromToRotation(player.Player_Model.transform.up, hit.normal) * targetRotation,
                    5f * Time.deltaTime);
            }
            player.Player_Model.transform.rotation = targetRotation;
        }
    }

    private void OnDrawRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        float shiftScale = Input.GetKey(KeyCode.LeftShift) ? player.shiftScale : 1f;
        // �������λ��
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 verticalMove = Vector3.up * player.verticalVelocity * Time.deltaTime;
        Vector3 horizontalMove=new Vector3(0,0,0);

        Vector3 input = new Vector3(h, 0, v);
        if (input.magnitude > 0) 
        {
            horizontalMove = player.Player_Model.transform.forward * player.moveSpeed
                * player.currentSpeedMultiplier * shiftScale * Time.deltaTime;
        }
        player.CharacterController.Move(horizontalMove + verticalMove);
    }

    public override void Exit()
    {
        shiftScale = 1f;
        player.Player_Model.ClearRootMotionAction();
        player.inkParticle.Stop();
    }

private IEnumerator TransitionTo2Point5D(CinemachineVirtualCamera toVCam, 
                                          CinemachineFreeLook fromVCam,
                                          float duration)
{
    if (activeTransition != null) yield break;
    activeTransition = Player_Controller.Instance.StartCoroutine(TransitionRoutine());

    IEnumerator TransitionRoutine()
    {
        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        CinemachineBlendDefinition originalBlend = brain.m_DefaultBlend;
        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, duration);
        
        toVCam.Priority = 100;
        fromVCam.Priority = 0;
        
        yield return new WaitForSeconds(duration);

        // �������ڹ��ɽ���ʱͬ��FreeLook����ķ���2.5D����ĳ���
        Vector3 lookDirection = toVCam.transform.forward;
        lookDirection = Quaternion.Euler(0, 90, 0) * lookDirection;
        float targetHorizontal = Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;

        fromVCam.m_XAxis.Value = targetHorizontal;      // ͬ��ˮƽ��ת
        fromVCam.m_YAxis.Value = 0.7f;                   // ���ô�ֱ�ᵽ�м���
        
        brain.m_DefaultBlend = originalBlend;
        activeTransition = null;
    }
}

private IEnumerator TransitionToFreeLook(CinemachineFreeLook toVCam, 
                                           CinemachineVirtualCamera fromVCam,
                                           float duration)
{
    if (activeTransition != null) yield break;
    activeTransition = Player_Controller.Instance.StartCoroutine(TransitionRoutine());
    
    IEnumerator TransitionRoutine()
    {
        // ����ֻ�账�����ȼ��л��ͻ������
        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        CinemachineBlendDefinition originalBlend = brain.m_DefaultBlend;
        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, duration);
        
        toVCam.Priority = 100;
        fromVCam.Priority = 0;
        
        yield return new WaitForSeconds(duration);
        
        brain.m_DefaultBlend = originalBlend;
        activeTransition = null;
    }
}
    // ������Ծ������
    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && player.CharacterController.isGrounded)
        {
            StartJump();
            //player.OnWolfJump();
        }
        ApplyGravity();
    }

    private void StartJump()
    {
        player.verticalVelocity = player.jumpPower;
    }

    private void ApplyGravity()
    {
        if(player.verticalVelocity>player.gravity)
        {  
            // ����Ӧ������
            player.verticalVelocity +=2.2f* player.gravity * Time.deltaTime;
        }
        else
        {
            player.verticalVelocity = player.gravity;
        }
    }


}
