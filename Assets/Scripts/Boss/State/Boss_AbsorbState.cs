using System.Collections;
using UnityEngine;
public class Boss_AbsorbState : BossStateBase
{
    private enum AbsorbChildState
    {
        Start,
        Absorb,
        AbsorbIdle2Absorb,
        Absorb2AbsorbIdle,
        AbsorbIdle,
        End
    }

    private AbsorbChildState absorbState;
    private float absorbSpeed = 1f;
    private float shiftScale = 1f;
    private float shiftTransition;
    private ParticleSystem eraseParticles;
    private Coroutine transitionCoroutine;

    private float langAndNPCInterval = 2.0f;
    private float timer = 0f;

    private AbsorbChildState CurrentState
    {
        get => absorbState;
        set
        {
            absorbState = value;
            boss.Boss_Model.SetRootMotionAction(OnAbsorbRootMotion);
            switch (absorbState)
            {
                case AbsorbChildState.Start:
                    boss.PlayAnimation("AbsorbStart");
                    break;
                case AbsorbChildState.Absorb:
                    boss.PlayAnimation("Absorb");
                    break;
                case AbsorbChildState.AbsorbIdle2Absorb:
                    boss.PlayAnimation("AbsorbIdle2Absorb");
                    break;
                case AbsorbChildState.Absorb2AbsorbIdle:
                    boss.PlayAnimation("Absorb2AbsorbIdle");
                    break;
                case AbsorbChildState.AbsorbIdle:
                    boss.PlayAnimation("AbsorbIdle");
                    break;
                case AbsorbChildState.End:
                    boss.PlayAnimation("AbsorbEnd");
                    break;
            }
        }
    }

    public override void Enter()
    {
        eraseParticles = boss.eraseParticles;
        CurrentState = AbsorbChildState.Start;
        shiftTransition = 0f;
        absorbSpeed = 1f;
    }



    public override void Update()
    {
        if (boss.CharacterController == null) return;
        timer += Time.deltaTime;
        if (timer>=langAndNPCInterval&& CurrentState == AbsorbChildState.Absorb)
        {
            timer = 0f;
            AbsorbLogic();
        }
        HandleJump(); // 新增跳跃处理
        if (Input.GetKeyDown(KeyCode.Tab) && CurrentState != AbsorbChildState.End)
        {
            CurrentState = AbsorbChildState.End;
            return;
        }
        if (!Input.GetMouseButton(0)&& CurrentState == AbsorbChildState.AbsorbIdle2Absorb)
        {
            eraseParticles.Stop();
            if (transitionCoroutine != null)
            {
                boss.StopCoroutine(transitionCoroutine);
                transitionCoroutine = null; // 清除协程引用
            }
            transitionCoroutine = boss.StartCoroutine(TransitionAbsorb(0f));
            CurrentState = AbsorbChildState.Absorb2AbsorbIdle;
            return;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            shiftTransition = Mathf.Clamp(shiftTransition + Time.deltaTime * 2f, 0f, 1f);
            shiftScale = Mathf.Lerp(1f, 2f, shiftTransition);
        }
        else
        {
            shiftTransition = Mathf.Clamp(shiftTransition - Time.deltaTime * 2f, 0f, 1f);
            shiftScale = Mathf.Lerp(1f, 2f, shiftTransition);
        }

        switch (CurrentState)
        {
            case AbsorbChildState.Start:
                HandleStartState();
                break;
            case AbsorbChildState.Absorb:
                HandleAbsorbState();
                UpdateMaterials();
                break;
            case AbsorbChildState.AbsorbIdle2Absorb:
                HandleAbsorbIdle2AbsorbState();
                UpdateMaterials();
                break;
            case AbsorbChildState.Absorb2AbsorbIdle:
                HandleAbsorb2AbsorbIdleState();
                UpdateMaterials();
                break;
            case AbsorbChildState.AbsorbIdle:
                HandleAbsorbIdleState();
                UpdateMaterials();
                break;
            case AbsorbChildState.End:
                HandleEndState();
                break;
        }
    }
    // 新增跳跃处理方法
    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && boss.CharacterController.isGrounded)
        {
            StartJump();
            //boss.OnRedHatJump();
        }
        ApplyGravity();
    }

    private void StartJump()
    {
        boss.verticalVelocity = boss.jumpPower;
    }

    private void ApplyGravity()
    {
        if(boss.verticalVelocity>boss.gravity)
        {  
            // 持续应用重力
            boss.verticalVelocity +=2.2f* boss.gravity * Time.deltaTime;
        }
        else
        {
            boss.verticalVelocity = boss.gravity;
        }
    }

    private void HandleStartState()
    {
        if (CheckAnimatorStateName("AbsorbStart", out float normalizedTime) && normalizedTime >= 0.95f)
        {
            CurrentState = AbsorbChildState.AbsorbIdle;
        }
        UpdateScale();
        HandleRotation();
    }
    private void HandleAbsorbIdle2AbsorbState()
    {
        if (CheckAnimatorStateName("AbsorbIdle2Absorb", out float normalizedTime) && normalizedTime >= 0.98f)
        {
            CurrentState = AbsorbChildState.Absorb;
        }
        UpdateScale();
        HandleRotation();
    }

    private void HandleAbsorb2AbsorbIdleState()
    {
        if (CheckAnimatorStateName("Absorb2AbsorbIdle", out float normalizedTime) && normalizedTime >= 0.98f)
        {
            CurrentState = AbsorbChildState.AbsorbIdle;
        }
        UpdateScale();
        HandleRotation();
    }



    private void HandleAbsorbState()
    {
        boss.Boss_Model.Animator.SetFloat("Absorb", shiftTransition);
        boss.Boss_Model.Animator.speed = shiftScale;
        CheckBeamArea();
        UpdateScale();
        HandleRotation();
        if (!Input.GetMouseButton(0))
        {
            eraseParticles.Stop();
            boss.strokeRecognizor.AutoRecognize(boss.roleID);
            if (transitionCoroutine != null)
            {
                boss.StopCoroutine(transitionCoroutine);
                transitionCoroutine = null; // 清除协程引用
            }
            transitionCoroutine = boss.StartCoroutine(TransitionAbsorb(0f));
            CurrentState = AbsorbChildState.Absorb2AbsorbIdle;
            return;
        }
    }
    private void AbsorbLogic()
    {
        boss.playersInRange.Clear();
        boss.enemiesInRange.Clear();
        boss.DetectObjectsInRange();
        //if (/*boss.playersInRange.Count > 0&&*/boss.enemiesInRange.Count > 0)
        //{
        //    GameObject targetEnemy = boss.enemiesInRange[0];
        //    EnemyAI enemyAI = targetEnemy.GetComponent<EnemyAI>();
        //    if (!(enemyAI.isDead))
        //    {
        //        enemyAI.ChangeState(new Dead(enemyAI));
        //        // ---- 新增：把这个敌人挂到 TornadoObject（风暴）下面 ----
        //        if (boss.TornadoObject != null)
        //        {
        //            // world-position 保持不变
        //            targetEnemy.transform.SetParent(boss.TornadoObject, true);
        //            
        //            // 如果你想让它“吸入”中心，可以把 localPosition 归零：
        //            // targetEnemy.transform.localPosition = Vector3.zero;
        //        }
        //    }
        //}
    }

    private void HandleAbsorbIdleState()
    {
        if (Input.GetMouseButton(0))
        {
            if (transitionCoroutine != null)
            {
                boss.StopCoroutine(transitionCoroutine);
                transitionCoroutine = null; // 清除协程引用
            }
            transitionCoroutine = boss.StartCoroutine(TransitionAbsorb(1f));
            eraseParticles.Play();
            UpdateScale();
            CurrentState = AbsorbChildState.AbsorbIdle2Absorb;
            return;
        }
        HandleRotation();
    }

    private void HandleEndState()
    {
        transitionCoroutine = boss.StartCoroutine(TransitionAbsorb(0f));
        if (CheckAnimatorStateName("AbsorbEnd", out float normalizedTime) && normalizedTime >= 0.95f)
        {
            boss.ChangeState(BossState.Idle);
        }
        UpdateScale();
        UpdateMaterials();

    }


    private void CheckBeamArea()
    {
        Vector3 position = boss.transform.position;
        Vector3 forward = boss.transform.forward;
        float beamLength = 2f;
        float beamWidth = 1f;
        
        Collider[] hitColliders = Physics.OverlapBox(
            position + forward * beamLength/2,
            new Vector3(beamWidth/2, 2f, beamLength/2),
            Quaternion.LookRotation(forward)
        );

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Flower"))
            {
                EventManager.Instance.DispatchEvent("OnBossBeamActive",
                    boss.gameObject,
                    forward,
                    beamLength,
                    beamWidth,
                    3f);
            }
        }
    }

    private void HandleRotation()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 input = new Vector3(h, 0, v);
        if (input.magnitude > 0) 
        {
            Quaternion targetRotation;
            float y = Camera.main.transform.rotation.eulerAngles.y;
            Vector3 targetDir = Quaternion.Euler(0, y, 0) * input;
            targetRotation= Quaternion.Slerp(boss.Boss_Model.transform.rotation, 
                Quaternion.LookRotation(targetDir), 
                Time.deltaTime * boss.rotateSpeed);

            if (Physics.Raycast(boss.Boss_Model.transform.position, Vector3.down, out RaycastHit hit))
            {
                targetRotation = Quaternion.Slerp(targetRotation, 
                    Quaternion.FromToRotation(boss.Boss_Model.transform.up, hit.normal) * targetRotation, 
                    5f * Time.deltaTime);
            }
            boss.Boss_Model.transform.rotation = targetRotation;
        }
    }

    private void OnAbsorbRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        float shiftScale = Input.GetKey(KeyCode.LeftShift) ? boss.shiftScale : 1f;
        // 处理控制位移
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 verticalMove = Vector3.up * boss.verticalVelocity * Time.deltaTime;
        Vector3 horizontalMove=new Vector3(0,0,0);

        Vector3 input = new Vector3(h, 0, v);
        if (input.magnitude > 0) 
        {
            horizontalMove = boss.Boss_Model.transform.forward * boss.absorbSpeed 
                * boss.currentSpeedMultiplier * shiftScale * Time.deltaTime;
        }
        boss.CharacterController.Move(horizontalMove + verticalMove);
    }

    public override void Exit()
    {
        eraseParticles.Stop();
        shiftTransition = 0f;
        shiftScale = 1f;
        boss.Boss_Model.Animator.speed = 1f;
        boss.Boss_Model.ClearRootMotionAction();
    }

    private void UpdateMaterials()
    {
        //Color color = Color.Lerp(Color.black, Color.white, boss.absorbtransition);
        //
        //// 更新风暴材质
        //boss.stormMaterial.SetColor("_TornadoColor", color);
        //boss.stormMaterial.SetColor("_FresnelColor", color);
        //boss.stormMaterial.SetFloat("_NumberofWaves", boss.absorbtransition);
        //boss.stormMaterial.SetFloat("_Emission", boss.absorbtransition);
    }

    private void UpdateScale()
    {
        if (boss.TornadoObject != null)
        {
            // 计算非线性缩放
            float scaleFactor = Mathf.Clamp( Mathf.Pow(boss.absorbtransition, boss.curveExponent), 0f, 1f) ;  // 曲线过渡效果

            boss.TornadoObject.localScale = Vector3.one * scaleFactor *boss.Enlargescale;  // 根据计算的比例调整目标物体的大小
        }
    }

    private IEnumerator TransitionAbsorb(float targetValue)
    {
        float startValue = boss.absorbtransition;
        float elapsedTime = 0f;

        while (elapsedTime < boss.transitionDuration)
        {
            boss.absorbtransition = Mathf.Lerp(startValue, targetValue, elapsedTime / boss.transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        boss.absorbtransition = targetValue;
    }

}