using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DrawParticlesController : MonoBehaviourPunCallbacks
{

    [Header("批量发送间隔（秒）")]
    [Tooltip("缓存碰撞点并每隔 sendInterval 批量发送一次")]
    public float sendInterval = 0.1f;

    public Color paintColor;
    public float minRadius = 0.05f;
    public float maxRadius = 0.2f;
    public float strength = 1;
    public float hardness = 1;

    public float revivalSpeed = 1f;

    // 重用的碰撞事件列表，避免 new
    private List<ParticleCollisionEvent> collisionEvents;
    // 按目标 ViewID 分组缓存的碰撞点列表
    private Dictionary<int, List<Vector3>> bufferByView;
    private float timer = 0f;

    // 当前是否强化状态，由外部调用 SetEnhancedState 设置
    private bool isEnhanced = false;

     [Space]
    ParticleSystem part;

    void Awake()
    {
        collisionEvents = new List<ParticleCollisionEvent>(16);
        bufferByView    = new Dictionary<int, List<Vector3>>(8);
    }
    void Start()
    {
        part = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// 设置强化状态（由外部调用，如 UI 按钮）
    /// </summary>
    public void SetEnhancedState(bool enhanced)
    {
        isEnhanced = enhanced;
    }

    /// <summary>
    /// 粒子与其他物体碰撞时 Unity 自动回调
    /// </summary>
    void OnParticleCollision(GameObject other)
    {
        // 复用 List，避免 GC Alloc:contentReference[oaicite:11]{index=11}
        collisionEvents.Clear();
        int numCollisions =part.GetCollisionEvents(other, collisionEvents);

        if (numCollisions == 0) return;

        // 获取碰撞目标的 ViewID（0 表示无 PhotonView）
        PhotonView otherPV = other.GetComponent<PhotonView>();
        int viewID = otherPV != null ? otherPV.ViewID : 0;

        // 按 viewID 取或新建子列表
        if (!bufferByView.TryGetValue(viewID, out var list))
        {
            list = new List<Vector3>(numCollisions);
            bufferByView[viewID] = list;
        }
        Paintable p = other.GetComponent<Paintable>();
        if (p != null)
        {
            List<Vector3> paintPath = new List<Vector3>();  // 存储涂色路径

            // 累积碰撞点
            for (int i = 0; i < numCollisions; i++)
            {
                Vector3 pos = collisionEvents[i].intersection;
                    paintPath.Add(pos);

                float radius = Random.Range(minRadius, maxRadius);
                    PaintManager.instance.paint(p, pos, radius, hardness, strength,     paintColor);
                list.Add(collisionEvents[i].intersection);
            }
        }
    }

    void Update()
    {
        if (bufferByView.Count == 0) return;

        timer += Time.deltaTime;
        if (timer < sendInterval) return;

        // 达到发送间隔，批量处理
        timer = 0f;

        foreach (var kv in bufferByView)
        {
            int viewID = kv.Key;
            var ptsList = kv.Value;
            if (ptsList.Count == 0) continue;

            // 复制并清空缓存，保证接下来马上可以继续累积
            Vector3[] ptsArray = ptsList.ToArray();
            ptsList.Clear();

            if (PhotonNetwork.IsMasterClient)
            {
                // 由主客户端直接广播给所有人（包括自己）:contentReference[oaicite:12]{index=12}
                var painterRuntime = FindObjectOfType<FlowerPainterRuntime>();
                painterRuntime.photonView.RPC(
                    "RPC_GenerateFlowers",
                    RpcTarget.AllBuffered,
                    ptsArray,
                    isEnhanced,
                    viewID
                );
            }
            else
            {
                // 非主客户端仅发给主客户端，主客户端收到后再统一广播
                var painterRuntime = FindObjectOfType<FlowerPainterRuntime>();
                painterRuntime.photonView.RPC(
                    "RPC_ForwardGenerate",
                    RpcTarget.MasterClient,
                    ptsArray,
                    isEnhanced,
                    viewID
                );
            }
        }
    }

}
