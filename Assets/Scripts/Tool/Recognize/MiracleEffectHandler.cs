using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;
using Photon.Pun;

public class MiracleEffectHandler : MonoBehaviourPun
{
    [Tooltip("StrokeRecognizor 实例，用于调用擦除 truePoints 的接口")]
    public StrokeRecognizor strokeRecognizor;

    [Tooltip("用于擦除时发射的粒子系统")]
    public ParticleSystem eraseParticleSystem;
    [Tooltip("首次擦除时额外发射的粒子系统")]
    public ParticleSystem initialEraseParticleSystem;

    [Tooltip("从点到质心所用的总时长（秒）")]
    public float durationPerPoint = 0.5f;
    [Tooltip("步步移动时，相邻两步最小距离")]
    public float minStepDistance = 4f;
    [Tooltip("初始擦除半径")]
    public float initialPaintEraseRadius = 0.5f;

    private const float kEmitProbability = 1.8f;

    struct EraseState { public Vector2 start, lastPos; public float elapsed; }
    struct EraseBatch
    {
        public EraseState[] states;
        public Vector2[] originalPoints;
        public Vector2 centroid;
        public PointCollection trueCollection;
        public float elapsed;
    }

    private List<EraseBatch> _batches = new List<EraseBatch>();
    private Dictionary<Vector2Int, Vector2> _cellMap;
    private RaycastHit[] _raycastResults = new RaycastHit[1];
    private int _paintableLayerMask;
    private FlowerPainterRuntime _cachedPainter;

    private void Awake()
    {
        _cellMap = new Dictionary<Vector2Int, Vector2>(256);
        _paintableLayerMask = LayerMask.GetMask("Paintable");
        _cachedPainter = FindObjectOfType<FlowerPainterRuntime>();
    }

    public void PlayEraseEffect(PointCollection enhancedCollection, PointCollection trueCollection)
    {
        if (enhancedCollection?.Points == null || enhancedCollection.Points.Count == 0)
            return;

        int count = enhancedCollection.Points.Count;
        Vector2[] orig = new Vector2[count];
        for (int i = 0; i < count; i++)
            orig[i] = new Vector2(enhancedCollection.Points[i].X, enhancedCollection.Points[i].Y);

        strokeRecognizor.RemoveEnhancedCollection(enhancedCollection);
        Vector2 centroid = CalculateCentroid(orig);

        var batch = new EraseBatch
        {
            states = new EraseState[count],
            originalPoints = orig,
            centroid = centroid,
            trueCollection = trueCollection,
            elapsed = 0f
        };
        for (int i = 0; i < count; i++)
        {
            batch.states[i] = new EraseState
            {
                start = orig[i],
                lastPos = centroid,
                elapsed = 0f
            };
        }

        _batches.Add(batch);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        for (int bi = _batches.Count - 1; bi >= 0; bi--)
        {
            var batch = _batches[bi];
            float maxDur = durationPerPoint;
            float cellSize = minStepDistance;
            int len = batch.states.Length;
            Vector2[] temp = new Vector2[len];
            int tempCount = 0;

            batch.elapsed += dt;
            float tNorm = Mathf.Min(batch.elapsed / maxDur, 1f);

            // 插值 & 收集
            for (int i = 0; i < len; i++)
            {
                var st = batch.states[i];
                if (st.elapsed < maxDur)
                {
                    st.elapsed += dt;
                    batch.states[i].elapsed = st.elapsed;
                    float t = Mathf.Min(st.elapsed / maxDur, 1f);
                    Vector2 cur = Vector2.Lerp(batch.centroid, st.start, t);
                    if ((cur - st.lastPos).sqrMagnitude >= minStepDistance * minStepDistance)
                    {
                        temp[tempCount++] = cur;
                        st.lastPos = cur;
                        batch.states[i].lastPos = cur;
                    }
                }
            }

            // 聚合
            _cellMap.Clear();
            for (int i = 0; i < tempCount; i++)
            {
                Vector2 p = temp[i];
                var key = new Vector2Int((int)(p.x / cellSize), (int)(p.y / cellSize));
                if (!_cellMap.TryGetValue(key, out _))
                    _cellMap[key] = p;
            }

            // 批量擦除 + 发粒子
            foreach (var p in _cellMap.Values)
            {
                if (batch.trueCollection != null)
                    strokeRecognizor.EraseTruePointsAt(p);
                EmitEraseParticle(p);
            }

            // 终结阶段
            if (tNorm >= 1f)
            {
                _cellMap.Clear();
                foreach (var p in batch.originalPoints)
                {
                    var key = new Vector2Int((int)(p.x / cellSize), (int)(p.y / cellSize));
                    if (!_cellMap.TryGetValue(key, out _))
                        _cellMap[key] = p;
                }
                foreach (var p in _cellMap.Values)
                    EmitInitialParticle(p);

                _batches.RemoveAt(bi);
            }
            else
            {
                _batches[bi] = batch;
            }
        }
    }

    // 只有“本机拥有”（IsMine）才负责发 RPC，其他客户端接收并执行本地粒子
private void EmitEraseParticle(Vector2 screenPos)
{
    if (Random.value > kEmitProbability * Time.deltaTime) return;

    Vector3 wp = strokeRecognizor.GetWorldPosition(screenPos);
    // RpcTarget.All 包括自己和其它所有人
    photonView.RPC(
        nameof(RPC_EmitEraseParticle),
        RpcTarget.All,
        wp
    );
}

private void EmitInitialParticle(Vector2 screenPos)
{
    Vector3 wp = strokeRecognizor.GetWorldPosition(screenPos);
    photonView.RPC(
        nameof(RPC_EmitInitialEraseParticle),
        RpcTarget.All,
        wp
    );
}


    [PunRPC]
    private void RPC_EmitEraseParticle(Vector3 wp)
        => eraseParticleSystem.Emit(new ParticleSystem.EmitParams { position = wp, applyShapeToPosition = true }, 1);

    [PunRPC]
    private void RPC_EmitInitialEraseParticle(Vector3 wp)
        => initialEraseParticleSystem.Emit(new ParticleSystem.EmitParams { position = wp, applyShapeToPosition = true }, 1);

    private Vector2 CalculateCentroid(Vector2[] pts)
    {
        float sx = 0f, sy = 0f;
        foreach (var p in pts) { sx += p.x; sy += p.y; }
        return new Vector2(sx / pts.Length, sy / pts.Length);
    }
}

//public static class Extensions
//{
//    // Utility to safely invoke if-not-null
//    public static void Let<T>(this T obj, System.Action<T> action) //where T : class
//    {
//        if (obj != null)
//            action(obj);
//    }
//}
