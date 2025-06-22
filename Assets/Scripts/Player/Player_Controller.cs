using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PDollarGestureRecognizer;
using Cinemachine;
using UnityEngine.UI;
using Photon.Pun;
using DG.Tweening;
using TMPro;
using UnityEditor.Rendering;
using Unity.Barracuda;
public class Player_Controller : CharacterBase<Player_Model>,IPunObservable
{
    [HideInInspector]
    public string roleID; 
    public static Player_Controller Instance { get; private set; }


    [Header("玩家基本信息")]
    public float gravity = -6f;
    public float rotateSpeed;
    public float moveSpeed = 1f;
    public float moveSpeedForJump = 3f;
    public float shiftScale = 3f;
    public float walk2RunTransition = 5;
    [Header("跳跃设置")]
    public float verticalVelocity = 5f;
    public float jumpPower = 5f;         // 跳跃初速度

    public float pencilMoveSpeed = 5f; // 铅笔移动速度

    public int maxHealth = 5;
    private int currentHealth;
    public int health => currentHealth; // 使用表达式属性

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

    [Header("能量吸收设置")]
    [SerializeField] private ParticleSystem energyAbsorptionParticle; // 能量吸收粒子
    [SerializeField] private float energyDrainRate = 20f;             // 每秒能量减少量
    private bool canBeAbsorb=true;
    public float absorbRadius = 3f;

    private bool isAbsorbing = false; 
     [Header("能量缓回")]
    [SerializeField] private float baseRegenRate = 2f;      // 每秒基础缓回速度
    private float regenBoostMultiplier = 1f;                // 当前缓回倍率
    private Coroutine regenBoostCoroutine;

    [Header("能量UI")]
    [SerializeField] private Image energyFillImage; // 需要绑定一个Image组件

    [Header("速度加成")]
    public float currentSpeedMultiplier = 1f; // 当前速度倍率
    public float accelerationDuration = 2f; // 加速持续时间
    public float decelerationDuration = 1f; // 减速持续时间

    [Header("减速效果")]
    [Tooltip("恢复到正常速度所需时间")]
    public float slowRecoveryDuration = 1f; // 恢复阶段持续时间

    private Coroutine slowEffectCoroutine;

    [Header("特效")]
    private FreeLookCameraShake cameraShake;
    public ParticleSystem inkParticle;
    public StrokeRecognizor strokeRecognizor;

    public bool isInkParticlePlaying = false;
    public bool isCollisionParticlePlaying = false;


    [Header("强化状态设置")]
    public bool isEnhanced = false; // 是否处于强化状态
    public float enhanceEnergyDrainRate = 10f; // 强化状态能量消耗速率
    public float wpoPowerEnhanced = 0.0006f; // 强化状态下的_wpo_power值
    public float outlineWidthEnhanced = 0.0001f; // 强化状态下的_outline_width值
    public List<Material> enhanceMaterials;
    private Coroutine enhanceCoroutine;

    [Header("强化状态颜色设置")]
    public Color enhancedColor = Color.magenta; // 强化颜色
    public Color defaultColor = Color.white;    // 默认颜色
    public ParticleSystem effectParticle;      // 需要改变颜色的粒子系统
    private Material particleMaterial;         // 粒子材质缓存
    private Color originalParticleColor;       // 原始颜色缓存

    [Header("绘制粒子控制")]
    public ParticleSystem collisionParticles;
    private DrawParticlesController drawParticlesController; // 粒子绘制控制器

    [Header("网络同步控制")]
    private CinemachineFreeLook virtualCamera;//获取FreeLook摄像机
    public Vector3 currentPos;
    public Quaternion currentRotation;
    public Vector3 currentScale;
    public string rpcPaintColorHex;


    [Header("死亡重生逻辑")]
    public float respawnTime = 1f;
    private bool isDead = false;

    private bool canControl = true; // 默认可控制
    public bool CanControl
    {
        get => canControl;
        set => canControl = value;
    }

    public bool isCollidingWithTornado = false;
    private Coroutine energyDrainCoroutine;
    private bool isKnockback;
    private Vector3 knockbackDirection;

    [Header("龙卷风吸收动画参数")]
    [SerializeField] private float moveDuration    = 2f;     // 半径从 max→min 所需时间
    [SerializeField] private float endShrinkScale  = 0.2f;   // 到达末端时的缩小比例
    [SerializeField] private float bounceScale     = 1.2f;   // 弹跳放大比例
    [SerializeField] private float bounceTime      = 0.2f;   // 弹跳时长
    [Header("Ease曲线")]
    [SerializeField] private Ease scaleEase       = Ease.OutQuad;
    [SerializeField] private Ease bounceEase      = Ease.OutBack;

    // —— 新增字段 —— 
    private Transform tornadoRoot;       // 缓存龙卷风根节点
    private float      initialRadius;    // 起始半径

    // Tween 序列与运行时变量
    private Sequence absorbSequence;

    // 在类变量区新增
    [Header("吸收运动参数")]
    [SerializeField] private float pullAcceleration = 5f;    // 朝向龙卷风的加  速度
    [SerializeField] private float orbitDeceleration = 2f;   // 横向速度衰减
    [SerializeField] private float minArrivalDistance = 0.5f;// 视为到达终点的  距离

    private Vector3 absorbVelocity;      // 当前吸收速度
    private bool isInAbsorption;         // 是否正在被吸收

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

    private void Awake()
    {
        photonview = GetComponent<PhotonView>();
        if (!PhotonNetwork.IsConnected)
        {
            photonView.enabled = false;
        }
        drawParticlesController = collisionParticles.GetComponent<DrawParticlesController>();
        // 1. 寻找模型所有 Renderer  
        var renderers = transform.Find("WolfFinal")
                                 .GetComponentsInChildren<Renderer>(true);

        // 2. 初始化列表
        enhanceMaterials = new List<Material>();

        foreach (var rend in renderers)
        {
            // 3. 强制实例化材质数组（每个 Renderer 会得到独立的 Material 实例）
            Material[] mats = rend.materials;  // 自动 Clone :contentReference[oaicite:5]{index=5}
            rend.materials = mats;             // 将 Clone 后的材质重新赋值回 Renderer

            // 4. 收集到 enhanceMaterials 中
            foreach (var mat in mats)
            {
                if (mat != null && !enhanceMaterials.Contains(mat))
                    enhanceMaterials.Add(mat);
            }
        }
    }
    void OnEnable()
    {
        StartCoroutine(EnergyRegenRoutine());
    }

    private void Start()
    {
       Player_Model.InitAudio(this);
        Init();
        ChangeState(PlayerState.Idle);
        currentHealth = maxHealth;
        strokeRecognizor = GameObject.Find("RecognizorCamera").GetComponent<StrokeRecognizor>();
        GameObject mainCamera = GameObject.Find("CMFreeLook");
        if (mainCamera != null)
        {
            cameraShake = mainCamera.GetComponent<FreeLookCameraShake>();
        }
        // 初始化粒子材质
        particleMaterial = effectParticle.  GetComponent<ParticleSystemRenderer>().material;
        originalParticleColor = particleMaterial.GetColor("_Color");
        InitializeEnergySystem();
        //同步位置
        currentPos = transform.position;
        currentRotation = transform.Find("WolfFinal").rotation;
        currentScale = transform.Find("WolfFinal").localScale;
        //摄像头位置
        if (photonView.IsMine)
        {
            roleID = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
            // canControl = true;
            virtualCamera = GameObject.Find("Cameras/CMFreeLook").GetComponent<CinemachineFreeLook>();
            // 切换相机目标到Player
            virtualCamera.Follow = transform;
            virtualCamera.LookAt = transform;
                        //修改名字
            //Transform canvasTf = transform.Find("CanvasUIWorld");
            //Transform name = canvasTf.transform.Find("name");
            //TMP_Text nameText = name.GetComponentInChildren<TMP_Text>();
            //nameText.text = PhotonNetwork.LocalPlayer.NickName;
            //photonView.RPC("RPC_UpdateName", RpcTarget.OthersBuffered, //PhotonNetwork.LocalPlayer.NickName);
            //photonView.RPC("RPC_StopEnhanceMode", RpcTarget.All);//刚开始回复到默认值
            //Invoke(nameof(Die), 10f);
            //rpcPaintColor = ColorUtility.ToHtmlStringRGB(drawParticlesController.paintColor);//defaultColor
        }
        else//不是本地玩家不能控制状态
        {
            roleID = photonView.Owner.ActorNumber.ToString();
            canControl = false;
        }
    }
    [PunRPC]
    public void RPC_UpdateName(string playerName)//同步名字
    {
        Transform canvasTf = transform.Find("CanvasUIWorld");
        TMP_Text nameText = canvasTf.GetComponentInChildren<TMP_Text>();
        nameText.text = playerName;
    }

    private void InitializeEnergySystem()
    {
        if (energyFillImage != null)
        {
            energyFillImage.type = Image.Type.Filled;
            energyFillImage.fillMethod = Image.FillMethod.Vertical;
            UpdateEnergyUI();
        }

        if (energyAbsorptionParticle != null)
        {
            var mainModule = energyAbsorptionParticle.main;
            mainModule.loop = true;
            energyAbsorptionParticle.Stop();
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
    public void Die()
    {
        if (!photonView.IsMine || isDead) return;
        isDead = true;
    }

    public void ReduceEnergy(float amount){
        energy -= amount;
        energy = Mathf.Max(energy, 0); // 防止能量值小于 0
        Debug.Log($"Energy reduced. Current Energy: {energy}");
    }

    public bool IsCollidingWithTornado()
    {
        return isCollidingWithTornado;
    }
    public void PrintCurrentEnergy()
    {
        Debug.Log($"Current Energy: {energy}");
    }

    private void Update()
    {
        if (photonView.IsMine)//里面的部分是原有的代码
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (!isEnhanced && Energy > 0)
                {
                    cameraShake.StartShake(1); // 相机震动
                    //TopTextController.Instance.PlayWave();
                    // 进入强化状态
                    // 让 UI 高亮“强化”图标
                    StartEnhanceMode();
                }
            }
            UpdateAbsorbPosition();
            if (isDead)
            {
                if (GetComponent<Player_Controller>().enabled)
                {
                    GetComponent<Player_Controller>().enabled = false;
                }

                if (GetComponent<CharacterController>().enabled)
                {
                    GetComponent<CharacterController>().enabled = false;
                }

                if (transform.localScale != Vector3.zero)
                {
                    transform.localScale = Vector3.zero;
                }
            }
        }
        /*else
        {
            UpdateLogic();
        }*/
    }
    private void FixedUpdate()
    {
        if (photonView.IsMine)//里面的部分是原有的代码
        {
            // 主机更新播放粒子系统播放状态
            UpdateParticleStates();
            if (currentHealth <= 0) // 血量归零后模拟玩家失败
            {
                if (currentHealth <= 0)
                {
                    Die();
                }

                if (isCollidingWithTornado)
                {
                    float energyDecreasePerSecond = 5f;
                    ReduceEnergy((energyDecreasePerSecond * Time.fixedDeltaTime));
                }
            }
        }
        else
        {

            UpdateLogic();
        }

    }
    public void UpdateLogic()//更新不是本机玩家的位置
    {
        transform.position = Vector3.Lerp(transform.position, currentPos, Time.deltaTime * 10f);
        transform.Find("WolfFinal").rotation = Quaternion.Slerp(transform.Find("WolfFinal").rotation, currentRotation, Time.deltaTime * 10f);
        transform.Find("WolfFinal").localScale = Vector3.Lerp(transform.Find("WolfFinal").localScale, currentScale, Time.deltaTime * 10f);
    }
    private void UpdateParticleStates()//同步粒子动画
    {
        isInkParticlePlaying = inkParticle.isPlaying;
        isCollisionParticlePlaying = collisionParticles.isPlaying;
    }




    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine || isDead) return;
        if (!other.CompareTag("tornadoArea")) return;
        CheckEnemyNearby();
        if (!canBeAbsorb) return; StartEnergyDrain();
        if (energyAbsorptionParticle != null && !energyAbsorptionParticle.isPlaying)
            energyAbsorptionParticle.Play();
        if (Energy < 3f)
            TryStartAbsorption(other);
    }
    private void OnTriggerStay(Collider other)
    {
        if (!photonView.IsMine || isDead) return;
        if (!other.CompareTag("tornadoArea")) return;

        CheckEnemyNearby();
        if (!canBeAbsorb) return;
        if (Energy < 3f)
        {
            TryStartAbsorption(other);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (!photonView.IsMine || isDead)
            return;

        if (!other.CompareTag("tornadoArea"))
            return;

        StopEnergyDrain();
        if (energyAbsorptionParticle != null)
            energyAbsorptionParticle.Stop();

        if (isInAbsorption)
        {
            DOTween.Kill(transform);
            shrinkSeq?.Kill();
            vanishSeq?.Kill();

            OnAbsorbed();
            return;
        }

        isAbsorbing = false;
        isInAbsorption = false;

        shrinkSeq?.Kill();
        vanishSeq?.Kill();
        canBeAbsorb = true;
    }
    private void CheckEnemyNearby()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, absorbRadius);
        canBeAbsorb = true;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                canBeAbsorb = false;
                break;
            }
        }
    }
    private void TryStartAbsorption(Collider tornadoArea)
    {
        /*if (isDead) return;
        if (isAbsorbing) 
            return;             // 已经在吸收了，就不要再启动*/
        if (isDead || isAbsorbing || !canBeAbsorb) return;

        if (Energy >= 3f)    
            return;             // 能量还没到阈值，也不启动

        isAbsorbing = true;    // 标记：吸收已启动
        StartAbsorption(tornadoArea);
    }
    // 类成员变量
    private Sequence shrinkSeq;
    private Sequence vanishSeq;

    private void StartAbsorption(Collider tornadoArea)
    {
        // 仅本地玩家进入吸收流程
        if (!photonView.IsMine || isDead || isInAbsorption) return;

        // 缓存龙卷风中心
        tornadoRoot = GetTornadoRoot();
        if (tornadoRoot == null) return;

        Debug.Log("StartAbsorption 开始吸收流程");

        // 初始化状态
        initialRadius = Vector3.Distance(transform.position, tornadoRoot.position);
        absorbVelocity = Vector3.zero;
        isInAbsorption = true;
        canControl = false;

        // 杀掉旧的收缩动画
        shrinkSeq?.Kill();
        vanishSeq?.Kill();

        shrinkSeq = DOTween.Sequence();
        shrinkSeq
            // 第一段：持续缩小
            .Append(transform.DOScale(endShrinkScale, moveDuration)
                .SetEase(scaleEase)
                .OnUpdate(() =>
                {
                    float dist = Vector3.Distance(transform.position, tornadoRoot.position);
                    Debug.Log($"吸收中，距离中心：{dist:F2}");
                    if (dist <= minArrivalDistance)
                    {
                        Debug.Log("达到最小距离，启动弹跳消失阶段");
                        shrinkSeq.Kill();
                        BeginVanishSequence();
                    }
                })
            );
           // 当收缩动画到时（不论是否满足距离），都进入下一步
          /* .OnComplete(() =>
           {
            Debug.Log("shrinkSeq 到时触发 OnComplete，启动弹跳消失");
            BeginVanishSequence();
                   });*/
    }

    private void BeginVanishSequence()
    {
        // 立即停止吸附运动
        isInAbsorption = false;

        vanishSeq?.Kill();
        vanishSeq = DOTween.Sequence();
        vanishSeq
            .Append(transform.DOScale(bounceScale, bounceTime).SetEase(bounceEase))
            .Append(transform.DOScale(0f, bounceTime).SetEase(Ease.InBack))
            .OnStart(() => Debug.Log("BeginVanishSequence：弹跳消失开始"))
            .OnComplete(() =>
            {
                Debug.Log("BeginVanishSequence：弹跳消失完成，调用 OnAbsorbed");
                OnAbsorbed();
            });
    }
    private Transform GetTornadoRoot()
{
    Transform t = GameObject.Find("tornado").transform;
    while (t != null && !t.CompareTag("Tornado"))
    {
        t = t.parent;
    }
    return t;
}

private void FinishAbsorption()
{
        if (isDead) return;
        isInAbsorption = false;
    absorbVelocity = Vector3.zero;
    
    // 强制对齐到龙卷风中心
    if (tornadoRoot != null)
    {
        transform.position = tornadoRoot.position;
    }
    
    // 触发弹跳动画
    absorbSequence?.Play();
}

private void UpdateAbsorbPosition()
{
    if (!isInAbsorption || tornadoRoot == null||isDead)
    {
       absorbVelocity = Vector3.zero;
       return;
    }
        if (characterController == null || !characterController.enabled)
        {
            return;
        }

        // 实时获取龙卷风位置
        Vector3 tornadoPos = tornadoRoot.position;
    Vector3 toTornado = tornadoPos - transform.position;
    
    // 计算加速度方向（朝向龙卷风）
    Vector3 acceleration = toTornado.normalized * pullAcceleration;
    
    // 计算横向速度衰减
    Vector3 lateralVelocity = Vector3.ProjectOnPlane(absorbVelocity, toTornado.normalized);
    absorbVelocity -= lateralVelocity * orbitDeceleration * Time.deltaTime;
    
    // 应用加速度
    absorbVelocity += acceleration * Time.deltaTime;
    
    // 移动角色
    characterController.Move(absorbVelocity * Time.deltaTime);
    
    // 面向龙卷风
    transform.rotation = Quaternion.LookRotation(-toTornado.normalized);
    
}


    private void OnAbsorbed()
    {
        // 只在本地玩家且未重复进入
        if (!photonView.IsMine || isDead)
            return;

        Debug.Log("OnAbsorbed：吸收动画完成，进入 OnAbsorbed");

        // 立即停掉所有与 transform 相关的 Tween
        DOTween.Kill(transform);
        shrinkSeq?.Kill();
        vanishSeq?.Kill();

        // 清理吸附运动
        absorbVelocity = Vector3.zero;
        StopAllCoroutines();

        // 对齐到龙卷风中心下方 0.5m 处
        if (tornadoRoot != null)
        {
            transform.position = new Vector3(
                tornadoRoot.position.x,
                0.5f,
                tornadoRoot.position.z
            );
        }

        // 重置朝向
        transform.rotation = Quaternion.identity;

        // 停掉吸收粒子
        if (energyAbsorptionParticle != null)
            energyAbsorptionParticle.Stop();


        // 延迟一帧，再调用 Die 进入真正的死亡逻辑
        StartCoroutine(DelayedDie());
    }
    private IEnumerator DelayedDie()
    {
        yield return null;
        Die();
    }
    private void StartEnergyDrain()
    {
        isCollidingWithTornado = true;
        if (energyDrainCoroutine == null)
        {
            energyDrainCoroutine = StartCoroutine(EnergyDrainRoutine());
        }
    }

    private void StopEnergyDrain()
    {
        isCollidingWithTornado = false;
        if (energyDrainCoroutine != null)
        {
            StopCoroutine(energyDrainCoroutine);
            energyDrainCoroutine = null;
        }
    }

    private IEnumerator EnergyDrainRoutine()
    {
        while (isCollidingWithTornado && Energy > 0)
        {
            Energy -= energyDrainRate * Time.deltaTime;
            
            // 当能量耗尽时触发额外逻辑
            if (Energy <= 2f)
            {
                HandleEnergyDepletion();
            }
            
            yield return null;
        }
    }
    private void HandleEnergyDepletion()
    {
        // 触发能量耗尽事件（例如减速/特殊效果）
        StopEnhanceMode();
        Debug.Log("Energy depleted!");

    }


    public void ChangeState(PlayerState playerState)
    {
        if (!canControl)
        {
            stateMachine.ChangeState<Player_IdleState>();
            return;
        }

        switch (playerState)
        {
            case PlayerState.Idle:
                stateMachine.ChangeState<Player_IdleState>();
                break;
            case PlayerState.Move:
                stateMachine.ChangeState<Player_MoveState>();
                break;
            case PlayerState.Draw:
                stateMachine.ChangeState<Player_DrawState>();
                break;
            case PlayerState.Jump:
                stateMachine.ChangeState<Player_JumpState>();
                break;
            case PlayerState.AirDown:
                stateMachine.ChangeState<Player_AirDownState>();
                break;
        }
    }
    
    private IEnumerator SpeedBoostEffect(float multiplier, float duration)
    {
        float elapsedTime = 0f;
        float startMultiplier = currentSpeedMultiplier;
        float targetMultiplier = multiplier;

        // 加速阶段
        while (elapsedTime < accelerationDuration)
        {
            currentSpeedMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, elapsedTime / accelerationDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // 确保使用 yield return null 而不是其他类型
        }

        currentSpeedMultiplier = targetMultiplier;

        // 维持速度加成一段时间
        yield return new WaitForSeconds(duration);

        // 减速阶段
        elapsedTime = 0f;
        startMultiplier = currentSpeedMultiplier;
        targetMultiplier = 1f;
        while (elapsedTime < decelerationDuration)
        {
            currentSpeedMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, elapsedTime / decelerationDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // 确保使用 yield return null
        }

        currentSpeedMultiplier = targetMultiplier;
    }

     /// <summary>
    /// 对外调用：立即将速度倍率降到 slowMultiplier，然后保持 slowDuration 秒，
    /// 最后在 slowRecoveryDuration 秒内缓慢恢复到 1f。
    /// </summary>
    //public void ApplySlowEffect(float slowMultiplier, float slowDuration)
    //{
    //    // 如果已有未结束的减速效果，先停止它
    //    if (slowEffectCoroutine != null)
    //        StopCoroutine(slowEffectCoroutine);
//
    //    slowEffectCoroutine = StartCoroutine(SlowEffectRoutine(slowMultiplier, slowDuration));
    //}

    [PunRPC]
    public void RPC_ApplySlowEffect(float slowMultiplier, float slowDuration)
    {
        // 只有本地玩家才执行效果
        if (photonView.IsMine)
        {
            if (slowEffectCoroutine != null)
                StopCoroutine(slowEffectCoroutine);

            slowEffectCoroutine = StartCoroutine(SlowEffectRoutine(slowMultiplier, slowDuration));
        }
    }

    private IEnumerator SlowEffectRoutine(float slowMultiplier, float slowDuration)
    {
        // 1. 立刻减速到目标倍率
        float originalMultiplier = currentSpeedMultiplier;
        currentSpeedMultiplier = slowMultiplier;
        // TODO: 如果需要，可以在这里播放减速特效，比如 slowParticles.Play();

        // 2. 保持减速一段时间
        yield return new WaitForSeconds(slowDuration);

        // 3. 缓慢恢复到正常速度
        float elapsed = 0f;
        while (elapsed < slowRecoveryDuration)
        {
            currentSpeedMultiplier = Mathf.Lerp(slowMultiplier, 1f, elapsed / slowRecoveryDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 最终确保回到 1f
        currentSpeedMultiplier = 1f;
        // TODO: 如果有减速特效，可以在这里停止 slowParticles.Stop();

        slowEffectCoroutine = null;
    }


     // 进入强化状态
    private void StartEnhanceMode()
    {
        if (isEnhanced || Energy <= 2f) return;
        if (photonView.IsMine)  // 只有本地玩家才能触发 RPC
        {
            photonView.RPC("RPC_StartEnhanceMode", RpcTarget.All);
        }
    }
    [PunRPC]
    private void RPC_StartEnhanceMode()
    {
        isEnhanced = true;
        collisionParticles.GetComponent<GrowthController>().SetEnhancedFlowerPrefabs(true);


        // 改变粒子颜色
        if (particleMaterial != null)
        {
            particleMaterial.SetColor("_Color", enhancedColor);
        }

        // 设置绘制粒子颜色
        if (drawParticlesController != null)
        {
            drawParticlesController.SetEnhancedState(true);
            drawParticlesController.paintColor = enhancedColor;
        }

        // 启动能量消耗协程（仅本地玩家执行）
        if (photonView.IsMine)
        {
            if (energyDrainCoroutine != null) StopCoroutine(energyDrainCoroutine);
            energyDrainCoroutine = StartCoroutine(EnhanceEnergyDrain());
        }
        // 启动材质过渡协程
        if (enhanceCoroutine != null) StopCoroutine(enhanceCoroutine);
        enhanceCoroutine = StartCoroutine(TransitionMaterialProperties(wpoPowerEnhanced, outlineWidthEnhanced));
    }


    // 退出强化状态
    private void StopEnhanceMode()
    {
        if (!isEnhanced) return;
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_StopEnhanceMode", RpcTarget.All);
        }
    }
    [PunRPC]
    private void RPC_StopEnhanceMode()
    {
        isEnhanced = false;
        collisionParticles.GetComponent<GrowthController>().SetEnhancedFlowerPrefabs(false);

        // 恢复粒子颜色
        if (particleMaterial != null)
        {
            particleMaterial.SetColor("_Color", originalParticleColor);
        }

        // 恢复绘制粒子设置
        if (drawParticlesController != null)
        {
            drawParticlesController.SetEnhancedState(false);
            drawParticlesController.paintColor = defaultColor;
        }

        // 停止能量消耗协程（仅本地玩家执行）
        if (photonView.IsMine && energyDrainCoroutine != null)
        {
            StopCoroutine(energyDrainCoroutine);
            energyDrainCoroutine = null;
        }
        // 启动材质过渡协程（恢复默认值）
        if (enhanceCoroutine != null) StopCoroutine(enhanceCoroutine);
        enhanceCoroutine = StartCoroutine(TransitionMaterialProperties(0f, 0f));
    }

    // 能量消耗协程
    private IEnumerator EnhanceEnergyDrain()
    {
        while (isEnhanced && Energy > 2f)
        {
            Energy -= enhanceEnergyDrainRate * Time.deltaTime;
            yield return null;
        }

        // 能量耗尽自动退出强化状态
        if (Energy <= 2f && photonView.IsMine)
        {
            photonView.RPC("RPC_StopEnhanceMode", RpcTarget.All);
        }
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
            stream.SendNext(transform.Find("WolfFinal").rotation);
            stream.SendNext(transform.Find("WolfFinal").localScale);
            stream.SendNext(isInkParticlePlaying);
            stream.SendNext(isCollisionParticlePlaying);
            stream.SendNext(Energy);
            rpcPaintColorHex = ColorUtility.ToHtmlStringRGB(drawParticlesController.paintColor);
            stream.SendNext(rpcPaintColorHex);
            stream.SendNext(isEnhanced);
        }
        else
        {
            currentPos = (Vector3)stream.ReceiveNext();
            currentRotation = (Quaternion)stream.ReceiveNext();
            currentScale = (Vector3)stream.ReceiveNext();
            isInkParticlePlaying= (bool)stream.ReceiveNext();
            isCollisionParticlePlaying= (bool)stream.ReceiveNext();
            Energy = (float)stream.ReceiveNext();
            string receivedColorHex = (string)stream.ReceiveNext();
            if (ColorUtility.TryParseHtmlString("#" + receivedColorHex, out Color newColor))
            {
                drawParticlesController.paintColor = newColor;
                rpcPaintColorHex = receivedColorHex;
            }
            isEnhanced=(bool)stream.ReceiveNext();
            // 应用接收到的状态
            ApplyParticleState(inkParticle, isInkParticlePlaying);
            ApplyParticleState(collisionParticles, isCollisionParticlePlaying);

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
        float originalDrain = energyDrainRate;
        energyDrainRate = 0f;
        noEnergyEffectPrefab.Play();

        yield return new WaitForSeconds(duration);

        energyDrainRate = originalDrain;
        noEnergyEffectPrefab.Stop();
    }

}