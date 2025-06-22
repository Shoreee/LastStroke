using UnityEngine;
using DG.Tweening;
using System.Collections;
using Photon.Pun;

public abstract class BaseEffectCore : MonoBehaviourPun
{
    [Header("Base Settings")]
    public float fixedScale    = 1f;
    public float hoverHeight   = 0.2f;
    public float hoverSpeed    = 1f;
    public float rotateSpeed   = 30f;
    [Header("Spawn Settings")]
    public float spawnScaleDuration = 0.5f;       // 缩放到 fixedScale 的时长
    public float spawnRaycastDist   = 1f;         // 地面检测射线长度

    [Header("Interaction Settings")]
    public float proximityCheckInterval = 0.5f;
    [Header("Tornado Settings")]
    public float tornadoPullSpeed = 2f;
    private GameObject currentTornadoTarget; // 当前作用的龙卷风目标
    public float tornadoPullDuration = 3f;
    private Coroutine currentTornadoPullCoroutine;
    private bool isInTornadoArea = false;





    protected bool isGrounded;
    protected Sequence activeSequence;
    protected Coroutine effectCoroutine;
    protected float skillScale;

    public virtual bool CanBePickedUp => true;

    protected bool isBeingCollected = false;

    private bool initialized = false;

    #region 生命周期

        // 新增：缓存物理组件
    private Rigidbody _rb;
    private Collider _col;

    // 在一开始就把 scale 设为 0
    public virtual void Initialize(float skillScale)
    {
        initialized = true;
        this.skillScale = skillScale;
        transform.localScale = Vector3.zero;

        // —— 临时禁用物理模拟 ——  
        // 物体生成初期不参与物理，避免注册开销
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        if (_rb != null)   _rb.isKinematic = true;
        if (_col != null)  _col.enabled       = false;

        // 缩放弹出
        transform
            .DOScale(Vector3.one * fixedScale, spawnScaleDuration)
            .SetEase(Ease.OutBack)            // 带弹性的 Back 缓动
            .OnComplete(OnSpawnComplete);

        // 物理检测或立即落地
        if (NeedPhysics())
            StartCoroutine(GroundCheckRoutine());
        else
            ImmediateSpawn();

        StartCoroutine(ProximityCheckRoutine());
    }

    protected virtual void Awake()
    {
        // 缓存物理组件（防止 Initialize 未调用时也能拿到）
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        // 如果脚本放在场景里而没有手动调用 Initialize，就在这里启动
        if (!initialized)
        {
            // 直接设为固定大小
            transform.localScale = Vector3.one * fixedScale;
            // 视为已着地
            if (NeedPhysics())
            {
                isGrounded = true;
                StartHoverEffect();
            }
        }
    }

    protected virtual void ImmediateSpawn()
    {
        // 直接视为着地
        OnGroundHit();
    }

    // 缩放完成后，如果已经在地面，就启动悬浮自转
    protected virtual void OnSpawnComplete()
    {
        // 缩放完成后，若已经着地就启动悬浮并恢复物理
        if (isGrounded)
        {
            StartHoverEffect();
            RestorePhysics();
        }
    }

    /// <summary>
    /// 恢复物理模拟：开启刚体和碰撞体
    /// </summary>
    private void RestorePhysics()
    {
        if (NeedPhysics())
            if (_rb != null)   
                _rb.isKinematic = false;
        if (_col != null)  _col.enabled       = true;
    }

    #endregion

    #region 地面检测

    protected virtual IEnumerator GroundCheckRoutine()
    {
        while (!isGrounded)
        {
            yield return new WaitForFixedUpdate();
            // 增大射线长度，防止物体稍微离地就检测不到
            if (Physics.Raycast(transform.position, Vector3.down, spawnRaycastDist))
            {
                OnGroundHit();
                yield break;
            }
        }
    }

    protected virtual void OnGroundHit()
    {
        isGrounded = true;
        // 如果缩放弹出已完成，就开始悬浮；否则等 OnSpawnComplete
        if (transform.localScale.x >= fixedScale - 0.01f)
            StartHoverEffect();
    }

    #endregion

    #region 视觉效果

    protected virtual void StartHoverEffect()
    {
        activeSequence?.Kill();

        float randomRange = 0.1f; // 悬浮幅度的随机变化范围
        float speedRandomRange = 0.3f; // 悬浮速度的随机变化范围

        activeSequence = DOTween.Sequence()
            .Append(transform.DOMoveY(
                transform.position.y + hoverHeight + Random.Range(-randomRange, randomRange),
                hoverSpeed + Random.Range(-speedRandomRange, speedRandomRange))
            .SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Yoyo);


        // 持续自转
        transform
            .DORotate(new Vector3(0, 360, 0), rotateSpeed, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1);
    }

    #endregion

    #region 交互系统

    protected virtual IEnumerator ProximityCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(proximityCheckInterval);
            if (isGrounded)
                CheckProximity();
        }
    }

    protected virtual void CheckProximity() { }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanBePickedUp || isBeingCollected) return;

        // 处理玩家拾取（原有逻辑）
        var player = other.GetComponent<Player_Controller>();
        if (player != null)
        {
            isBeingCollected = true;
            DisableCollider();
            StartCoroutine(PickupProcess(player));
            return;
        }

        if (other.CompareTag("tornadoArea"))
        {
            Debug.Log("进入龙卷风区域");
            isInTornadoArea = true;
            Transform tornadoParent = FindTornadoParent(other.transform);
            if (tornadoParent != null)
            {
                currentTornadoTarget = tornadoParent.gameObject;
            }
            return;
        }

        // 新增Boss处理
        if (other.CompareTag("Boss"))
        {
            var boss = other.GetComponent<Boss_Controller>();
            if (boss != null)
            {
                isBeingCollected = true;
                DisableCollider();
                StartCoroutine(PickupProcess(boss));
                return;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!CanBePickedUp || isBeingCollected) return;

        if (other.CompareTag("tornadoArea") && isInTornadoArea)
        {
            if (currentTornadoTarget != null)
            {
                ApplyTornadoPull();
            }
            return;
        }
    }

    // 新增龙卷风牵引协程
    private IEnumerator ApplyTornadoPull(GameObject tornadoTarget)
    {
        float elapsedTime = 0f;
        Rigidbody targetRb = tornadoTarget.GetComponent<Rigidbody>();

        while (elapsedTime < tornadoPullDuration)
        {
            if (targetRb != null)
            {
                // 计算朝向龙卷风的方向
                Vector3 direction = (transform.position - tornadoTarget.transform.position).normalized;
                targetRb.velocity = direction * tornadoPullSpeed;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 停止后重置速度
        if (targetRb != null)
        {
            targetRb.velocity = Vector3.zero;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("tornadoArea"))
        {
            Debug.Log("离开龙卷风区域");
            isInTornadoArea = false;
            currentTornadoTarget = null;
        }
    }
    private Transform FindTornadoParent(Transform child)
    {
        Transform parent = child;
        while (parent != null && !parent.CompareTag("Tornado"))
        {
            parent = parent.parent;
        }
        return parent;
    }

    private void ApplyTornadoPull()
    {
        if (currentTornadoTarget == null) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 direction = (currentTornadoTarget.transform.position - transform.position).normalized;
        rb.velocity = direction * tornadoPullSpeed;
    }


private void DisableCollider()
{
    var collider = GetComponent<Collider>();
    if (collider != null) collider.enabled = false;
}

    protected virtual IEnumerator PickupProcess(Player_Controller player)
{
    activeSequence?.Kill();

    // —— 1. 弹跳阶段 ——
    float bounceH = 1f, bounceDur = 0.2f;
    // WaitForCompletion 可以让 coroutine 等到 Tween 完成再继续 :contentReference[oaicite:1]{index=1}
    yield return transform
        .DOMoveY(transform.position.y + bounceH, bounceDur)
        .SetEase(Ease.OutQuad)
        .SetLoops(2, LoopType.Yoyo)
        .WaitForCompletion();

    // —— 2. 参数配置 ——
    float preMoveTime    = 0.3f;   // 从 startRadius→orbitRadius
    float totalOrbitTime = 0.7f;   // 恒定半径绕转总时长（含下段）
    float spiralTime     = 0.5f;   // 渐变缩小绕转时长
    float constantTime   = totalOrbitTime - spiralTime;
    float orbitSpeed     = 240f;   // 度/秒
    float heightOffset   = 1f;     // 玩家中心偏移
    float orbitRadius    = 0.8f;   // 绕转半径
    float startScale     = fixedScale;

    // 缓存中心点和起始半径
    Vector3 center     = player.transform.position + Vector3.up * heightOffset;
    float startRadius  = Vector3.Distance(transform.position, center);

    // —— 3. 预绕转：半径从 startRadius→orbitRadius —— 
    float t0 = 0f;
    while (t0 < preMoveTime)
    {
        center = player.transform.position + Vector3.up * heightOffset;
        float p = t0 / preMoveTime;
        float currR = Mathf.Lerp(startRadius, orbitRadius, p);

        // 每帧绕转一小段 :contentReference[oaicite:2]{index=2}
        transform.RotateAround(center, Vector3.up, orbitSpeed * Time.deltaTime);
        Vector3 dir = (transform.position - center).normalized;
        transform.position = center + dir * currR;

        t0 += Time.deltaTime;
        yield return null;
    }

    // 对齐半径
    center = player.transform.position + Vector3.up * heightOffset;
    Vector3 finalDir = (transform.position - center).normalized;
    transform.position = center + finalDir * orbitRadius;

    // —— 4. 恒定半径绕转 —— 
    float t1 = 0f;
    while (t1 < constantTime)
    {
        center = player.transform.position + Vector3.up * heightOffset;
        transform.RotateAround(center, Vector3.up, orbitSpeed * Time.deltaTime);
        Vector3 dir = (transform.position - center).normalized;
        transform.position = center + dir * orbitRadius;

        t1 += Time.deltaTime;
        yield return null;
    }

    // —— 5. 渐变缩小绕转 —— 
    float t2 = 0f;
    while (t2 < spiralTime)
    {
        center = player.transform.position + Vector3.up * heightOffset;
        float p = t2 / spiralTime;
        // 缩放从 startScale→0
        float s = Mathf.Lerp(startScale, 0f, p);
        transform.localScale = Vector3.one * s;

        transform.RotateAround(center, Vector3.up, orbitSpeed * Time.deltaTime);
        Vector3 dir2 = (transform.position - center).normalized;
        transform.position = center + dir2 * orbitRadius;

        t2 += Time.deltaTime;
        yield return null;
    }

    // 直接触发效果并销毁
    ApplyEffect(player);
    Dispose();
}
        protected virtual IEnumerator PickupProcess(Boss_Controller player)
{
    activeSequence?.Kill();

    // —— 1. 弹跳阶段 ——
    float bounceH = 1f, bounceDur = 0.2f;
    // WaitForCompletion 可以让 coroutine 等到 Tween 完成再继续 :contentReference[oaicite:1]{index=1}
    yield return transform
        .DOMoveY(transform.position.y + bounceH, bounceDur)
        .SetEase(Ease.OutQuad)
        .SetLoops(2, LoopType.Yoyo)
        .WaitForCompletion();

    // —— 2. 参数配置 ——
    float preMoveTime    = 0.3f;   // 从 startRadius→orbitRadius
    float totalOrbitTime = 0.7f;   // 恒定半径绕转总时长（含下段）
    float spiralTime     = 0.5f;   // 渐变缩小绕转时长
    float constantTime   = totalOrbitTime - spiralTime;
    float orbitSpeed     = 240f;   // 度/秒
    float heightOffset   = 1f;     // 玩家中心偏移
    float orbitRadius    = 0.8f;   // 绕转半径
    float startScale     = fixedScale;

    // 缓存中心点和起始半径
    Vector3 center     = player.transform.position + Vector3.up * heightOffset;
    float startRadius  = Vector3.Distance(transform.position, center);

    // —— 3. 预绕转：半径从 startRadius→orbitRadius —— 
    float t0 = 0f;
    while (t0 < preMoveTime)
    {
        center = player.transform.position + Vector3.up * heightOffset;
        float p = t0 / preMoveTime;
        float currR = Mathf.Lerp(startRadius, orbitRadius, p);

        // 每帧绕转一小段 :contentReference[oaicite:2]{index=2}
        transform.RotateAround(center, Vector3.up, orbitSpeed * Time.deltaTime);
        Vector3 dir = (transform.position - center).normalized;
        transform.position = center + dir * currR;

        t0 += Time.deltaTime;
        yield return null;
    }

    // 对齐半径
    center = player.transform.position + Vector3.up * heightOffset;
    Vector3 finalDir = (transform.position - center).normalized;
    transform.position = center + finalDir * orbitRadius;

    // —— 4. 恒定半径绕转 —— 
    float t1 = 0f;
    while (t1 < constantTime)
    {
        center = player.transform.position + Vector3.up * heightOffset;
        transform.RotateAround(center, Vector3.up, orbitSpeed * Time.deltaTime);
        Vector3 dir = (transform.position - center).normalized;
        transform.position = center + dir * orbitRadius;

        t1 += Time.deltaTime;
        yield return null;
    }

    // —— 5. 渐变缩小绕转 —— 
    float t2 = 0f;
    while (t2 < spiralTime)
    {
        center = player.transform.position + Vector3.up * heightOffset;
        float p = t2 / spiralTime;
        // 缩放从 startScale→0
        float s = Mathf.Lerp(startScale, 0f, p);
        transform.localScale = Vector3.one * s;

        transform.RotateAround(center, Vector3.up, orbitSpeed * Time.deltaTime);
        Vector3 dir2 = (transform.position - center).normalized;
        transform.position = center + dir2 * orbitRadius;

        t2 += Time.deltaTime;
        yield return null;
    }

    // 直接触发效果并销毁
    ApplyEffect(player);
    Dispose();
}



    #endregion

    #region 抽象方法

    protected abstract void ApplyEffect(Player_Controller player);
    protected abstract void ApplyEffect(Boss_Controller boss);
    protected abstract bool NeedPhysics();

    #endregion

    public virtual void Dispose()
    {
        StopAllCoroutines();
        activeSequence?.Kill();
        Destroy(gameObject);
    }
}
