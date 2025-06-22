using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Cinemachine;
using UnityEngine.UI;
using TMPro;

public class Boss_Controller : CharacterBase<Boss_Model>, IPunObservable
{
    [HideInInspector]
    public string roleID;    // 新增：存放角色 ID 的字符串
    #region 配置类的信息
    [Header("配置")]
    public float gravity = -6f;
    public float rotateSpeed;
    public float moveSpeed = 5f;
    public float moveSpeedForJump = 3f;
    public float shiftScale = 3f;
    public float walk2RunTransition = 5;
    [Header("跳跃设置")]
    public float verticalVelocity = 5f;
    public float jumpPower = 5f;         // 跳跃初速度

    public float absorbSpeed=5f;

    public float moveSpeedForAirDown;

    public int maxHealth = 5;
    int currentHealth;
    public int health { get { return currentHealth; } }

    [Header("能量系统")]
    [SerializeField] private float energy = 100f;
    public float Energy
    {
        get => energy;
        set
        {
            energy = Mathf.Clamp(value, 0, 100);
            UpdateEnergyUI();
        }
    }

    [Header("能量UI")]
    [SerializeField] private Image energyFillImage; // 需要绑定一个Image组件

    [Header("能量缓回")]
    [SerializeField] private float baseRegenRate = 2f;      // 每秒基础缓回速度
    private float regenBoostMultiplier = 1f;                // 当前缓回倍率
    private Coroutine regenBoostCoroutine;

    [Header("强化状态设置")]
    public bool isEnhanced = false; // 是否处于强化状态
    public float enhanceEnergyDrainRate = 10f; // 强化状态能量消耗速率
    public float wpoPowerEnhanced = 0.0006f; // 强化状态下的_wpo_power值
    public float outlineWidthEnhanced = 0.0001f; // 强化状态下的_outline_width值
    public List<Material> enhanceMaterials;

    private FreeLookCameraShake cameraShake;
    
    private Coroutine enhanceCoroutine;

    private Coroutine energyDrainCoroutine;

    [Header("鸟群加速参数")]
    public float speedBoostMultiplier = 3f; // 加速倍率
    public float accelerationDuration = 2f;   // 加速持续时间
    public float decelerationDuration = 1f;   // 减速持续时间

    [Header("粒子特效")]
    public ParticleSystem eraseParticles;

    public bool isErasePlaying = false;

    [Tooltip("正常移动时的发射率")]
    public float runnormalRate = 1f;
    [Tooltip("冲刺时的发射率")]
    public float runsprintRate = 10f;

    [Header("吸收狼和npc")]
    public float langAndNPCAbsorbRange = 5f;
    // 存储检测到的 Player 和 Enemy
    public List<GameObject> playersInRange = new List<GameObject>();
    public List<GameObject> enemiesInRange = new List<GameObject>();

    [Header("Storm")]
    public float transitionDuration = 0.5f;
    [Range(0, 1)] public float absorbtransition;
    public Material stormMaterial;
    public Transform TornadoObject;  // 目标物体
    public float curveExponent = 2f; // 曲线的指数

    public float Enlargescale =1f;

    // 完全复刻ParabolaControl的参数
    [Header("投掷设置")]
    private LineRenderer parabola; 
    public float throwAngle = 45f; // 投掷仰角（角度制）
    public float heightOffset = 1f; // 投掷点高度偏移
    public GameObject circle;  
    public float timespan = 0.02f; 
    public float speed = 20f; 
    public GameObject grenadePrefab;

    [Header("投掷能量消耗")]
    public float throwEnergyCost = 30f; // 新增投掷能量消耗量


    // 原ParabolaControl的核心参数
    private bool IsDown;
    private float Delay = 0.1f;
    private float LastDownTime;
    private Vector3 initialForward;

    [Header("蓄力设置")]
    public float maxChargeTime = 3f;      // 最大蓄力时间
    public float maxSpeedMultiplier = 1.5f; // 最大速度倍率
    public float maxAngleMultiplier = 1.5f; // 最大角度倍率
    private float chargeTime;             // 当前蓄力时间
    private bool isCharging;              // 是否正在蓄力



    #endregion
    public StrokeRecognizor strokeRecognizor;

    public float currentSpeedMultiplier = 1f; // 当前速度倍率


    private bool canControl = true; // 默认可以控制

    private CinemachineFreeLook virtualCamera;//获取FreeLook摄像机

    public Vector3 bossCurrentPos;
    public Quaternion bossCurrentRotation;
    
    [Header("Core 效果粒子预制体")]
    public ParticleSystem speedEffectPrefab;      // 加速时的环绕粒子
    public ParticleSystem jumpEffectPrefab;       // 跳跃增强时的环绕粒子
    public ParticleSystem noEnergyEffectPrefab;   // 免耗能时的环绕粒子

    public bool isspeedEffectPlaying = false;

    public bool isjumpEffectPlaying = false;

    public bool isnoEnergyEffectPlaying = false;

    private Coroutine speedBoostCoroutine;
    private Coroutine jumpBoostCoroutine;
    private Coroutine noEnergyCoroutine;
    
    public PhotonView photonview;

    private void Start()
    {
        photonview = GetComponent<PhotonView>();
        Boss_Model.InitAudio(this);
        Init();
        ChangeState(BossState.Idle);
        InitializeEnergySystem();
        //初始生命值
        currentHealth = maxHealth;
        currentHealth = 1;
        strokeRecognizor = GameObject.Find("RecognizorCamera").GetComponent<StrokeRecognizor>();
        //同步位置
        bossCurrentPos = transform.position;
        bossCurrentRotation = transform.Find("Test_Little_Red_Character_Model9").rotation;
        //摄像头位置
        if (photonView.IsMine)
        {
            roleID = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
            //    canControl = true;
            virtualCamera = GameObject.Find("Cameras/CMFreeLook").GetComponent<CinemachineFreeLook>();
            // 切换相机目标到Player
            virtualCamera.Follow = transform;
            virtualCamera.LookAt = transform;
            ////修改名字
            //Transform canvasTf = transform.Find("CanvasUIWorld");
            //Transform name = canvasTf.transform.Find("name");
            //TMP_Text nameText = name.GetComponentInChildren<TMP_Text>();
            //nameText.text = PhotonNetwork.LocalPlayer.NickName;
            //photonView.RPC("RPC_UpdateName", RpcTarget.OthersBuffered, PhotonNetwork.//LocalPlayer.NickName);
            //小红帽看不见引导线
            GameObject patternObject = GameObject.Find("图案");
            if (patternObject != null)
            {
                patternObject.SetActive(false);
            }
        }
        else//不是本地玩家不能控制状态
        {
            roleID = photonView.Owner.ActorNumber.ToString();
            canControl = false;
        }
        GameObject mainCamera = GameObject.Find("CMFreeLook");
        if (mainCamera != null)
        {
            cameraShake = mainCamera.GetComponent<FreeLookCameraShake>();
        }
        parabola = GameObject.Find("BombLine").GetComponent<LineRenderer>();
        parabola.enabled = false;
        circle.SetActive(false);
        isEnhanced = true;
        StopEnhanceMode();
    }


    [PunRPC]
    public void RPC_UpdateName(string playerName)//同步名字
    {
        Transform canvasTf = transform.Find("CanvasUIWorld");
        TMP_Text nameText = canvasTf.GetComponentInChildren<TMP_Text>();
        nameText.text = playerName;
    }
    //---------------------------------------------------------------------
    public void ChangeHealth(int amount)//生命值改变
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        Debug.Log(currentHealth + "/" + maxHealth);
    }
    // 当玩家死亡时调用的方法
    public void Die()
    {

    }

    //重生方法
    public void Respawn()
    {
        // 恢复生命值
        currentHealth = maxHealth;
        Debug.Log(currentHealth + "/" + maxHealth);
    }
    void Update()
    {
        if (photonView.IsMine)//里面的部分是原有的代码
        {
            HandleGrenadeInput();
            if (Energy > 0)
                UpdateParabola();
            else
                ResetThrowState();
            if (isCharging)
            {
                chargeTime = Mathf.Clamp(chargeTime + Time.deltaTime, 0, maxChargeTime);
            }
        }
    }

    void HandleGrenadeInput()
    {
        // 能量不足时直接返回
        if (Energy < throwEnergyCost) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!isEnhanced && Energy > 0)
            {
                // 进入强化状态
                StartEnhanceMode();
            }
            StartCharging();
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            if (isCharging)
            {
                isCharging = false;
                //TopTextController.Instance.PlayWave(1);
                cameraShake.StartShake(1); // 相机震动
                Buttonup();
                Energy -= throwEnergyCost;
                StopEnhanceMode();
            }
        }
    }
    private void InitializeEnergySystem()
    {
        if (energyFillImage != null)
        {
            energyFillImage.type = Image.Type.Filled;
            energyFillImage.fillMethod = Image.FillMethod.Vertical;
            UpdateEnergyUI();
        }
    }
        private void UpdateEnergyUI()
    {
        if (energyFillImage != null)
        {
            energyFillImage.fillAmount = Energy / 100f;
        }
    }
        private IEnumerator EnergyRegenRoutine()
    {
        while (true)
        {
            if (Energy < 100f)
            {
                // Mathf.Clamp 已在 setter 中处理上限
                Energy += baseRegenRate * regenBoostMultiplier * Time.deltaTime;
            }
            yield return null;
        }
    }
    public void ApplyRegenBoost(float boostMultiplier, float duration)
    {
        if (regenBoostCoroutine != null)
            StopCoroutine(regenBoostCoroutine);
        regenBoostCoroutine = StartCoroutine(RegenBoostRoutine(boostMultiplier, duration));
    }
    [PunRPC]
    public void RPC_ApplyRegenBoost(float boostMultiplier, float duration)
    {
        ApplyRegenBoost(boostMultiplier, duration);
    }
    private IEnumerator RegenBoostRoutine(float boost, float duration)
    {
        regenBoostMultiplier = boost;
        yield return new WaitForSeconds(duration);
        regenBoostMultiplier = 1f;
        regenBoostCoroutine = null;
    }



    void UpdateParabola()
{
    if (!IsDown) return;
    
    // 即使未达到Delay也立即显示
    List<Vector3> points = GetVector3s();
    parabola.positionCount = points.Count;
    parabola.SetPositions(points.ToArray());
    circle.transform.position = points[points.Count - 1];
    
    parabola.enabled = true;
    circle.SetActive(true);
    
    // 仅更新时间戳用于其他逻辑
    if (Time.time - LastDownTime >= Delay)
    {
        LastDownTime = Time.time;
    }
}
    void StartCharging()
    {
        // 能量不足时直接返回
        if (Energy < throwEnergyCost) return;
    
        isCharging = true;
        chargeTime = 0f;
        Buttondown();
    }

    public void Buttondown()
    {
        IsDown = true;
        LastDownTime = Time.time;
    }

// 修改Buttonup方法（使用统一方向向量）
public void Buttonup()
{
    if (!IsDown) return;

    if (Energy < throwEnergyCost)
    {
        ResetThrowState();
        return;
    }

    // 计算蓄力比例
    float chargeRatio = Mathf.Clamp01(chargeTime / maxChargeTime);
    
    // 应用蓄力参数
    float currentSpeed = speed * Mathf.Lerp(1f, maxSpeedMultiplier, chargeRatio);
    float currentAngle = throwAngle * Mathf.Lerp(1f, maxAngleMultiplier, chargeRatio);
    // 原始的前向向量（单位化）
    Vector3 forward = Boss_Model.transform.forward.normalized;

    // 旋转轴：模型的右方向（绕这个轴旋转就会在俯仰平面里抬头／低头）
    Vector3 rightAxis = Boss_Model.transform.right;

    // 构造一个绕 rightAxis 旋转 throwAngle 度的四元数
    Quaternion pitch = Quaternion.AngleAxis(currentAngle, rightAxis);

    // 用这个四元数去旋转 forward，就得到了抬头 throwAngle 度后的方向
    Vector3 throwDirection = pitch * forward;

    // 生成手雷
    Vector3 throwPos = Boss_Model.transform.position + Vector3.up * heightOffset;
    GameObject grenade = PhotonNetwork.Instantiate(
        grenadePrefab.name,  
        throwPos,            
        Quaternion.identity 
    );


    // 计算初速度（保持方向一致性）
    Vector3 initialVelocity = throwDirection * currentSpeed;

    Rigidbody rb = grenade.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.velocity = initialVelocity;
    }
    else
    {
        Debug.LogError("手雷预制体缺少Rigidbody组件！");
    }
    
    ResetThrowState();
    
}

// 修改GetVector3s方法（同步物理计算）
List<Vector3> GetVector3s()
{
    List<Vector3> list = new List<Vector3>();
    Vector3 startPos = Boss_Model.transform.position + Vector3.up * heightOffset;
    // 计算蓄力比例（0-1）
    float chargeRatio = Mathf.Clamp01(chargeTime / maxChargeTime);
    
    // 应用蓄力参数
    float currentSpeed = speed * Mathf.Lerp(1f, maxSpeedMultiplier, chargeRatio);
    float currentAngle = throwAngle * Mathf.Lerp(1f, maxAngleMultiplier, chargeRatio);
    
        // 原始的前向向量（单位化）
    Vector3 forward = Boss_Model.transform.forward.normalized;

    // 旋转轴：模型的右方向（绕这个轴旋转就会在俯仰平面里抬头／低头）
    Vector3 rightAxis = Boss_Model.transform.right;

    // 构造一个绕 rightAxis 旋转 throwAngle 度的四元数
    Quaternion pitch = Quaternion.AngleAxis(currentAngle, rightAxis);

    // 用这个四元数去旋转 forward，就得到了抬头 throwAngle 度后的方向
    Vector3 throwDirection = pitch * forward;
    
    // 计算初速度
    Vector3 initialVelocity = throwDirection * currentSpeed;

    for (int i = 0; i < 1000; i++)
    {
        float t = timespan * i;
        Vector3 position = startPos + 
                          initialVelocity * t + 
                          Vector3.up * (0.5f * Physics.gravity.y * t * t);
        
        list.Add(position);

        // 碰撞检测（保持原逻辑）
        if(i > 0)
        {
            RaycastHit hit;
            Vector3 rearDir = list[list.Count - 1] - list[list.Count - 2];
            if(Physics.Raycast(list[list.Count - 2], rearDir, out hit, rearDir.magnitude))
            {
                list[list.Count - 1] = hit.point;
                break;
            }
        }
    }
    return list;
}

    // 新增状态重置方法
void ResetThrowState()
{
    parabola.enabled = false;
    circle.SetActive(false);
    IsDown = false;
    isCharging = false;
    chargeTime = 0f;
}

    public void FixedUpdate()//该函数的最后是在每一帧的最后执行
    {
        if (photonView.IsMine)//里面的部分是原有的代码
        {
            // 主机更新播放粒子系统播放状态
            UpdateParticleStates();
            if (currentHealth <= 0) // 血量归零后模拟玩家失败
            {
                //添加复活逻辑
                Die();
            }
        }
        else
        {
            UpdateLogic();
        }

    }
    public void UpdateLogic()//更新不是本机玩家的位置
    {
        transform.position = Vector3.Lerp(transform.position, bossCurrentPos, Time.deltaTime * 10);
        transform.Find("Test_Little_Red_Character_Model9").rotation = Quaternion.Slerp(transform.Find("Test_Little_Red_Character_Model9").rotation, bossCurrentRotation, Time.deltaTime * 10);
    }

    private void UpdateParticleStates()//同步粒子动画
    {
        isErasePlaying = eraseParticles.isPlaying;
    }
    public  void DetectObjectsInRange()//同时检测范围内的狼和NPC
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, langAndNPCAbsorbRange);
        foreach (var collider in colliders)
        {
            GameObject obj = collider.gameObject;

            // 检查对象的标签
            if (obj.CompareTag("Player"))
            {
                playersInRange.Add(obj);
            }
            else if (obj.CompareTag("Enemy"))
            {
                enemiesInRange.Add(obj);
            }
        }
    }
    //---------------------------------------------------------------------

    public void ChangeState(BossState bossState)
    {
        if (!canControl)
        {
            stateMachine.ChangeState<Boss_IdleState>();
            return; // 如果不能控制，就不做任何状态切换
        }
        switch (bossState)
        {
            case BossState.Idle:
                stateMachine.ChangeState<Boss_IdleState>();
                break;
            case BossState.Move:
                stateMachine.ChangeState<Boss_MoveState>();
                break;
            case BossState.Absorb:
                stateMachine.ChangeState<Boss_AbsorbState>();
                break;
            case BossState.AirDown:
                stateMachine.ChangeState<Boss_AirDownState>();
                break;
            case BossState.Jump:
                stateMachine.ChangeState<Boss_JumpState>();
                break;
        }
    }


    private void OnEnable()
{
    // 注册事件：当玩家碰撞到鸟时触发加速
    EventManager.Instance.Regist("BirdSpeedBoost", OnBirdSpeedBoost);
        StartCoroutine(EnergyRegenRoutine());
    }

private void OnDisable()
{
    // 注销事件
    EventManager.Instance.UnRegist("BirdSpeedBoost", OnBirdSpeedBoost);
}

// 事件回调：触发加速
private void OnBirdSpeedBoost(object[] args)
{
    // 如果有正在进行的加速协程，先停止它
    if (speedBoostCoroutine != null)
    {
        StopCoroutine(speedBoostCoroutine);
    }
    // 启动新的加速协程
    speedBoostCoroutine = StartCoroutine(SpeedBoostEffect());
}

// 协程：速度渐变效果
private IEnumerator SpeedBoostEffect()
{
    float elapsedTime = 0f;
    float startMultiplier = currentSpeedMultiplier;
    float targetMultiplier = speedBoostMultiplier;
    // 加速阶段
    while (elapsedTime < accelerationDuration)
    {
        currentSpeedMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, elapsedTime / accelerationDuration);
        elapsedTime += Time.deltaTime;
        yield return null;
    }
    currentSpeedMultiplier = targetMultiplier;


    // 保持最高速度（可选）
    yield return new WaitForSeconds(1f);

    // 减速阶段
    elapsedTime = 0f;
    startMultiplier = currentSpeedMultiplier;
    targetMultiplier = 1f;
    while (elapsedTime < decelerationDuration)
    {
        currentSpeedMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, elapsedTime / decelerationDuration);
        elapsedTime += Time.deltaTime;
        yield return null;
    }
    currentSpeedMultiplier = targetMultiplier;
}   


     // 进入强化状态
    private void StartEnhanceMode()
    {
        if (isEnhanced || Energy <= 0) return;
        
        isEnhanced = true;

        // 启动能量消耗协程
        if (energyDrainCoroutine != null) StopCoroutine(energyDrainCoroutine);
        energyDrainCoroutine = StartCoroutine(EnhanceEnergyDrain());
        
        // 启动强化状态检测协程
        if (enhanceCoroutine != null) StopCoroutine(enhanceCoroutine);
            enhanceCoroutine = StartCoroutine(TransitionMaterialProperties(wpoPowerEnhanced, outlineWidthEnhanced));
        
        Debug.Log("进入强化状态");
    }

    // 退出强化状态
    private void StopEnhanceMode()
    {
        if (!isEnhanced) return;
        ResetThrowState();
        
        isEnhanced = false;
        // 停止协程
        if (energyDrainCoroutine != null) 
        {
            StopCoroutine(energyDrainCoroutine);
            energyDrainCoroutine = null;
        }
        if (enhanceCoroutine != null) StopCoroutine(enhanceCoroutine);
        enhanceCoroutine = StartCoroutine(TransitionMaterialProperties(0f, 0f));
        
        Debug.Log("退出强化状态");
    }

    // 能量消耗协程
    private IEnumerator EnhanceEnergyDrain()
    {
        while (isEnhanced && Energy > 0)
        {
            Energy -= enhanceEnergyDrainRate * Time.deltaTime;
            yield return null;
        }
        
        // 能量耗尽自动退出强化状态
        if (Energy <= 0) StopEnhanceMode();
    }

    // 平滑过渡版本
    private IEnumerator TransitionMaterialProperties(float targetWpo, float targetOutline, float duration = 0.5f)
    {
        float elapsed = 0f;
        
        // 记录所有材质的初始值（实时获取当前值）
        List<float> initialWpoValues = new List<float>();
        List<float> initialOutlineValues = new List<float>();
        
        foreach (var mat in enhanceMaterials)
        {
            if (mat != null)
            {
                initialWpoValues.Add(mat.HasProperty("_wpo_power") ? mat.GetFloat("_wpo_power") : 0f);
                initialOutlineValues.Add(mat.HasProperty("_outline_width") ? mat.GetFloat("_outline_width") : 0f);
            }
        }
    
        // 判断是否为进入状态（目标值非零）
        bool isEntering = targetWpo > 0f || targetOutline > 0f;
    
        // 时间分配参数
        float phase1Duration = isEntering ? duration * 0.3f : 0f; // 进入状态时才需要第一阶段
        float phase2Duration = duration - phase1Duration;
    
        // 过冲参数（仅进入状态时有效）
        float overshootWpo = isEntering ? targetWpo * 2f : 0f;
        float overshootOutline = isEntering ? targetOutline * 2f : 0f;
    
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
    
            for (int i = 0; i < enhanceMaterials.Count; i++)
            {
                var mat = enhanceMaterials[i];
                if (mat == null) continue;
    
                float currentWpo = initialWpoValues[i];
                float currentOutline = initialOutlineValues[i];
    
                // 处理_wpo_power
                if (mat.HasProperty("_wpo_power"))
                {
                    if (isEntering)
                    {
                        // 进入状态的两阶段处理
                        if (elapsed < phase1Duration)
                        {
                            // 第一阶段：快速上升到过冲值
                            float t = Mathf.Clamp01(elapsed / phase1Duration);
                            mat.SetFloat("_wpo_power", Mathf.Lerp(currentWpo, overshootWpo, t));
                        }
                        else
                        {
                            // 第二阶段：缓慢回落到目标值
                            float t = Mathf.Clamp01((elapsed - phase1Duration) / phase2Duration);
                            mat.SetFloat("_wpo_power", Mathf.Lerp(overshootWpo, targetWpo, t));
                        }
                    }
                    else
                    {
                        // 退出状态的单阶段处理
                        float t = Mathf.Clamp01(elapsed / duration);
                        mat.SetFloat("_wpo_power", Mathf.Lerp(currentWpo, targetWpo, t));
                    }
                }
    
                // 处理_outline_width（逻辑同上）
                if (mat.HasProperty("_outline_width"))
                {
                    if (isEntering)
                    {
                        if (elapsed < phase1Duration)
                        {
                            float t = Mathf.Clamp01(elapsed / phase1Duration);
                            mat.SetFloat("_outline_width", Mathf.Lerp(currentOutline, overshootOutline, t));
                        }
                        else
                        {
                            float t = Mathf.Clamp01((elapsed - phase1Duration) / phase2Duration);
                            mat.SetFloat("_outline_width", Mathf.Lerp(overshootOutline, targetOutline, t));
                        }
                    }
                    else
                    {
                        float t = Mathf.Clamp01(elapsed / duration);
                        mat.SetFloat("_outline_width", Mathf.Lerp(currentOutline, targetOutline, t));
                    }
                }
            }
            yield return null;
        }
    
        // 确保最终值准确
        foreach (var mat in enhanceMaterials)
        {
            if (mat != null)
            {
                if (mat.HasProperty("_wpo_power")) mat.SetFloat("_wpo_power", targetWpo);
                if (mat.HasProperty("_outline_width")) mat.SetFloat("_outline_width", targetOutline);
            }
        }
    }

    private BossState GetCurrentBossState()// 根据当前状态机的状态返回对应的 PlayerState
    {
        if (stateMachine.CurrentState is Boss_IdleState)
        {
            Debug.Log("Idle");
            return BossState.Idle;
        }
        else if (stateMachine.CurrentState is Boss_MoveState)
        {
            return BossState.Move;
        }
        else if (stateMachine.CurrentState is Boss_AbsorbState)
        {
            return BossState.Absorb;
        }
        else if (stateMachine.CurrentState is Boss_AirDownState)
        {
            return BossState.AirDown;
        }
        return BossState.Idle; // 默认返回 Idle 状态
    }
    //收到同步的粒子系统后改变
    private void ApplyParticleState(ParticleSystem particle, bool isPlaying)
    {
        if (isPlaying && !particle.isPlaying)
        {
            particle.Play();
        }
        else if (!isPlaying && particle.isPlaying)
        {
            particle.Stop();
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.Find("Test_Little_Red_Character_Model9").rotation);
            stream.SendNext(isErasePlaying);
            stream.SendNext(Energy);
            stream.SendNext(isEnhanced);
        }
        else
        {
            bossCurrentPos = (Vector3)stream.ReceiveNext();
            bossCurrentRotation = (Quaternion)stream.ReceiveNext();
            // 远程客户端接收特效数据
            isErasePlaying = (bool)stream.ReceiveNext();
            Energy = (float)stream.ReceiveNext();
            isEnhanced = (bool)stream.ReceiveNext();
            // 应用接收到的状态
            ApplyParticleState(eraseParticles, isErasePlaying);

            UpdateEnergyUI();
        }
    }


    [PunRPC]
    public void RPC_AddEnergy(float amount)
    {
        if (photonView.IsMine)
        {
            Energy += amount;
            Energy = Mathf.Clamp(Energy, 0, 100);
        }
    }

    private bool isKnockback;

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (isKnockback) return;

        StartCoroutine(KnockbackRoutine(direction.normalized * force, duration));
    }

    [PunRPC]
    public void RPC_ApplyKnockback(Vector3 direction, float force, float duration)
    {
        // 只有本地玩家才执行物理击退
        if (photonView.IsMine)
        {
            if (isKnockback) return;
            StartCoroutine(KnockbackRoutine(direction.normalized * force, duration));
        }
    }

    private IEnumerator KnockbackRoutine(Vector3 force, float duration)
    {
        isKnockback = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 使用CharacterController移动
            characterController.Move(force * Time.deltaTime);
            // 或者直接修改位置
            // transform.position += force * Time.deltaTime;

            // 衰减力度
            force = Vector3.Lerp(force, Vector3.zero, elapsed/duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        isKnockback = false;
    }

    // —— 加速效果 —— 
    [PunRPC]
    public void RPC_ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostCoroutine != null)
            StopCoroutine(speedBoostCoroutine);
        speedBoostCoroutine = StartCoroutine(
            SpeedBoostCoroutine(multiplier, duration)
        );
    }

    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        // 临时提升速度
        float original = currentSpeedMultiplier;
        currentSpeedMultiplier = multiplier;

        speedEffectPrefab.Play();

        yield return new WaitForSeconds(duration);

        // 恢复
        currentSpeedMultiplier = original;
        speedEffectPrefab.Stop();
    }

    // —— 跳跃增强 —— 
    [PunRPC]
    public void RPC_ApplyJumpBoost(float multiplier, float duration)
    {
        if (jumpBoostCoroutine != null)
            StopCoroutine(jumpBoostCoroutine);
        jumpBoostCoroutine = StartCoroutine(
            JumpBoostCoroutine(multiplier, duration)
        );
    }

    private IEnumerator JumpBoostCoroutine(float multiplier, float duration)
    {
        float originalJump = jumpPower;
        jumpPower *= multiplier;

        jumpEffectPrefab.Play();

        yield return new WaitForSeconds(duration);

        jumpPower = originalJump;
        jumpEffectPrefab.Stop();

    }

    // —— 免耗能 —— 
    [PunRPC]
    public void RPC_StartNoEnergyConsumption(float duration)
    {
        if (noEnergyCoroutine != null)
            StopCoroutine(noEnergyCoroutine);
        noEnergyCoroutine = StartCoroutine(
            NoEnergyConsumptionCoroutine(duration)
        );
    }

    private IEnumerator NoEnergyConsumptionCoroutine(float duration)
    {
        float originalDrain = enhanceEnergyDrainRate;
        float originalthrowEnergyCost = throwEnergyCost;
        enhanceEnergyDrainRate = 0f;
        throwEnergyCost = 20f;
        noEnergyEffectPrefab.Play();

        yield return new WaitForSeconds(duration);

        enhanceEnergyDrainRate = originalDrain;
        throwEnergyCost = originalthrowEnergyCost;
        noEnergyEffectPrefab.Stop();
    }


    


}
