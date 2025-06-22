using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

[RequireComponent(typeof(PhotonView))]
public class FlowerPainterRuntime : MonoBehaviourPun
{
    [Header("必填引用")]
    public FlowerPaintArea paintArea;
    public GrowthController growthController;

    [Header("魔法阵粒子效果")]
    public ParticleSystem magicCirclePrefab;         // 魔法阵特效预制体
    public Paintable paintableTarget;               // 要涂色的 Paintable 对象（可留空自动检测）

    [Header("涂色参数 (仿 DrawParticlesController)")]
    public Color paintColor       = Color.white;    // 涂色颜色
    public float paintMinRadius   = 0.1f;          // 最小半径
    public float paintMaxRadius   = 0.2f;           // 最大半径
    public float paintStrength    = 1f;             // 强度
    public float paintHardness    = 1f;             // 硬度

    [Header("花朵生成参数")]
    public int   flowersPerSeed   = 100;
    public float maxGrowthRadius  = 5f;
    public float growthStepRadius = 0.5f;
    public float spawnSpeed       = 100f;           // 每秒生成多少株花

    private bool isGenerating = false;
    
    /// <summary>
    /// 非主客户端调用，将点集转给主客户端
    /// </summary>
    [PunRPC]
    void RPC_ForwardGenerate(Vector3[] paintPoints, bool enhanced, int parentViewID, PhotonMessageInfo info)
    {
        // 仅主客户端执行
        if (!PhotonNetwork.IsMasterClient || paintPoints == null || paintPoints.Length == 0)
            return;

        // ① 本地先直接调用本地生成（可选，也可等下面的 RPC_GenerateFlowers 处理）
        var runtime = FindObjectOfType<FlowerPainterRuntime>();
        if (runtime != null)
        {
            growthController.GrowFlowersLocally(
                new List<Vector3>(paintPoints),
                enhanced,
                PhotonView.Find(parentViewID)?.transform
            );
        }

        // ② 再由主客户端统一广播给其他客户端（不含自己，因为已绘制）:contentReference[oaicite:13]{index=13}
        photonView.RPC(
            "RPC_GenerateFlowers",
            RpcTarget.OthersBuffered,
            paintPoints,
            enhanced,
            parentViewID
        );
    }

    // FlowerPainterRuntime 中已有的 RPC，接收后在各端生成花朵
    // 两种重载都可用，这里用三参数版本
    // paintPoints: 点集列表
    // enhanced: 是否强化
    // parentViewID: 播放魔法阵或其他用途的父物体 ID
    [PunRPC]
    void RPC_GenerateFlowers(Vector3[] paintPoints, bool enhanced, int parentViewID)
    {
        var runtime = FindObjectOfType<FlowerPainterRuntime>();
        if (runtime != null)
        {
            // 调用已有方法，在本地生成花朵
           growthController. GrowFlowersLocally(
                new List<Vector3>(paintPoints),
                enhanced,
                PhotonView.Find(parentViewID)?.transform
            );
        }
    }

    public void GenerateALlFlowers()
    {
        if (!isGenerating)
        {
            StartCoroutine(GenerateFlowersGradually());
        }
    }
    // 接收绘制者发来的生成请求
    [PunRPC]
    void RPC_GenerateFlowers(Vector3[] paintPoints, bool isEnhanced)
    {
        // 直接在本地生成
        growthController.GrowFlowersLocally(paintPoints.ToList(), isEnhanced);
    }



    [PunRPC]
    public void RPC_EraseFlowers(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (var col in hits)
        {
            var flower = col.GetComponent<FlowerController>()?.gameObject;
            if (flower != null && flower.activeSelf)
            {
                // 1) 标记：禁用碰撞体，防止下一次 OverlapSphere 又检测到同一朵
                Collider c = flower.GetComponent<Collider>();
                if (c != null) c.enabled = false;

                // 2) 启动销毁协程
                StartCoroutine(ScaleDownAndRelease(flower));
            }
        }
    }

    [PunRPC]
    private void RPC_EraseFlowersBatch(Vector3[] positions, float radius)
    {
        foreach (var p in positions)
        {
            // 具体服务器端擦除逻辑
            NoRPC_EraseFlowers(p, radius);
        }
    }

    public void NoRPC_EraseFlowers(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (var col in hits)
        {
            var flower = col.GetComponent<FlowerController>()?.gameObject;
            if (flower != null && flower.activeSelf)
            {
                // 1) 标记：禁用碰撞体，防止下一次 OverlapSphere 又检测到同一朵
                Collider c = flower.GetComponent<Collider>();
                if (c != null) c.enabled = false;

                // 2) 启动销毁协程
                StartCoroutine(ScaleDownAndRelease(flower));
            }
        }
    }

    private IEnumerator ScaleDownAndRelease(GameObject go)
    {
        // 缓存 Transform，少一次 null 合并判断
        Transform tf = go?.transform;
        if (go == null || tf == null) yield break;

        Vector3 initScale = tf.localScale;
        float duration = 0.5f, elapsed = 0f;

        // 3) 缩放循环，每次都检查 go 是否已被 Destroy
        while (elapsed < duration)
        {
            if (go == null) yield break;

            tf.localScale = Vector3.Lerp(initScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4) 结束时再一次检查
        if (go != null)
        {
            tf.localScale = Vector3.zero;
            Destroy(go);
        }
    }

    IEnumerator GenerateFlowersGradually()
    {
        if (paintArea == null || growthController == null)
        {
            Debug.LogWarning("未设置 paintArea 或 growthController 引用！");
            yield break;
        }

        isGenerating = true;

        List<Vector3> seedPoints = paintArea.flowerPositions;
        List<Vector3> buffer     = new List<Vector3>();
        Vector3 candidate;

        int layerCount = Mathf.CeilToInt(maxGrowthRadius / growthStepRadius);
        foreach (var seed in seedPoints)
        {
                candidate = new Vector3(seed.x, seed.y+ 2f, seed.z);
                if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit1, 5f))
                {
                    // 1) 检测 Paintable 并播放魔法阵特效
                    Paintable p = paintableTarget != null 
                                        ? paintableTarget 
                                        : hit1.collider.GetComponent<Paintable>();
                    if (p != null)
                    {
                            if (magicCirclePrefab != null)
                            {
                                // 在地面法线方向上实例化并播放特效
                                var ps = Instantiate(magicCirclePrefab, hit1.point, Quaternion.LookRotation(hit1.normal));
                                ps.Play();
                                Destroy(ps.gameObject, ps.main.duration);
                            }

                    }
                }
        }

        for (int layer = 1; layer <= layerCount; layer++)
        {
            float r1 = (layer - 1) * growthStepRadius;
            float r2 = layer * growthStepRadius;
            float ringArea  = Mathf.PI * (r2 * r2 - r1 * r1);
            float totalArea = Mathf.PI * maxGrowthRadius * maxGrowthRadius;
            float layerRatio = ringArea / totalArea;
            int flowersThisLayer = Mathf.RoundToInt(flowersPerSeed * layerRatio);

            foreach (var seed in seedPoints)
            {
                for (int i = 0; i < flowersThisLayer; i++)
                {
                    Vector2 offset2D = Random.insideUnitCircle.normalized * Random.Range(r1, r2);
                    candidate = new Vector3(seed.x + offset2D.x, seed.y + 2f, seed.z + offset2D.y);

                    if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 5f))
                    {
                        // 1) 检测 Paintable 并播放魔法阵特效
                        Paintable p = paintableTarget != null 
                                        ? paintableTarget 
                                        : hit.collider.GetComponent<Paintable>();
                        if (p != null)
                        {
                            // 2) 对地面进行涂色
                            float radius = Random.Range(paintMinRadius, paintMaxRadius);
                            PaintManager.instance.paint(
                                p, 
                                hit.point, 
                                radius, 
                                paintHardness, 
                                paintStrength, 
                                paintColor
                            );
                        }

                        // 3) 缓存点位，待批量生成
                        buffer.Add(hit.point);

                        // 批量生成时，同步清空并 Yield
                        if (buffer.Count >= spawnSpeed * Time.deltaTime)
                        {
                            growthController.GrowFlowersLocally(new List<Vector3>(buffer), false);
                            buffer.Clear();
                            yield return null;
                        }
                    }
                }
            }

            yield return null;
        }

        // 最后一批
        if (buffer.Count > 0)
        {
            growthController.GrowFlowersLocally(buffer, false);
            buffer.Clear();
        }

        Debug.Log($"[花朵生成完成] 种子点数: {seedPoints.Count}，每点花数: {flowersPerSeed}，最大半径: {maxGrowthRadius}");
        isGenerating = false;
    }

    private Vector3 RoundPosition(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x * 100) / 100,
            Mathf.Round(position.y * 100) / 100,
            Mathf.Round(position.z * 100) / 100
        );
    }
}
