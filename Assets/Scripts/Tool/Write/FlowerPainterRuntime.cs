using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

[RequireComponent(typeof(PhotonView))]
public class FlowerPainterRuntime : MonoBehaviourPun
{
    [Header("��������")]
    public FlowerPaintArea paintArea;
    public GrowthController growthController;

    [Header("ħ��������Ч��")]
    public ParticleSystem magicCirclePrefab;         // ħ������ЧԤ����
    public Paintable paintableTarget;               // ҪͿɫ�� Paintable ���󣨿������Զ���⣩

    [Header("Ϳɫ���� (�� DrawParticlesController)")]
    public Color paintColor       = Color.white;    // Ϳɫ��ɫ
    public float paintMinRadius   = 0.1f;          // ��С�뾶
    public float paintMaxRadius   = 0.2f;           // ���뾶
    public float paintStrength    = 1f;             // ǿ��
    public float paintHardness    = 1f;             // Ӳ��

    [Header("�������ɲ���")]
    public int   flowersPerSeed   = 100;
    public float maxGrowthRadius  = 5f;
    public float growthStepRadius = 0.5f;
    public float spawnSpeed       = 100f;           // ÿ�����ɶ����껨

    private bool isGenerating = false;
    
    /// <summary>
    /// �����ͻ��˵��ã����㼯ת�����ͻ���
    /// </summary>
    [PunRPC]
    void RPC_ForwardGenerate(Vector3[] paintPoints, bool enhanced, int parentViewID, PhotonMessageInfo info)
    {
        // �����ͻ���ִ��
        if (!PhotonNetwork.IsMasterClient || paintPoints == null || paintPoints.Length == 0)
            return;

        // �� ������ֱ�ӵ��ñ������ɣ���ѡ��Ҳ�ɵ������ RPC_GenerateFlowers ����
        var runtime = FindObjectOfType<FlowerPainterRuntime>();
        if (runtime != null)
        {
            growthController.GrowFlowersLocally(
                new List<Vector3>(paintPoints),
                enhanced,
                PhotonView.Find(parentViewID)?.transform
            );
        }

        // �� �������ͻ���ͳһ�㲥�������ͻ��ˣ������Լ�����Ϊ�ѻ��ƣ�:contentReference[oaicite:13]{index=13}
        photonView.RPC(
            "RPC_GenerateFlowers",
            RpcTarget.OthersBuffered,
            paintPoints,
            enhanced,
            parentViewID
        );
    }

    // FlowerPainterRuntime �����е� RPC�����պ��ڸ������ɻ���
    // �������ض����ã��������������汾
    // paintPoints: �㼯�б�
    // enhanced: �Ƿ�ǿ��
    // parentViewID: ����ħ�����������;�ĸ����� ID
    [PunRPC]
    void RPC_GenerateFlowers(Vector3[] paintPoints, bool enhanced, int parentViewID)
    {
        var runtime = FindObjectOfType<FlowerPainterRuntime>();
        if (runtime != null)
        {
            // �������з������ڱ������ɻ���
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
    // ���ջ����߷�������������
    [PunRPC]
    void RPC_GenerateFlowers(Vector3[] paintPoints, bool isEnhanced)
    {
        // ֱ���ڱ�������
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
                // 1) ��ǣ�������ײ�壬��ֹ��һ�� OverlapSphere �ּ�⵽ͬһ��
                Collider c = flower.GetComponent<Collider>();
                if (c != null) c.enabled = false;

                // 2) ��������Э��
                StartCoroutine(ScaleDownAndRelease(flower));
            }
        }
    }

    [PunRPC]
    private void RPC_EraseFlowersBatch(Vector3[] positions, float radius)
    {
        foreach (var p in positions)
        {
            // ����������˲����߼�
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
                // 1) ��ǣ�������ײ�壬��ֹ��һ�� OverlapSphere �ּ�⵽ͬһ��
                Collider c = flower.GetComponent<Collider>();
                if (c != null) c.enabled = false;

                // 2) ��������Э��
                StartCoroutine(ScaleDownAndRelease(flower));
            }
        }
    }

    private IEnumerator ScaleDownAndRelease(GameObject go)
    {
        // ���� Transform����һ�� null �ϲ��ж�
        Transform tf = go?.transform;
        if (go == null || tf == null) yield break;

        Vector3 initScale = tf.localScale;
        float duration = 0.5f, elapsed = 0f;

        // 3) ����ѭ����ÿ�ζ���� go �Ƿ��ѱ� Destroy
        while (elapsed < duration)
        {
            if (go == null) yield break;

            tf.localScale = Vector3.Lerp(initScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4) ����ʱ��һ�μ��
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
            Debug.LogWarning("δ���� paintArea �� growthController ���ã�");
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
                    // 1) ��� Paintable ������ħ������Ч
                    Paintable p = paintableTarget != null 
                                        ? paintableTarget 
                                        : hit1.collider.GetComponent<Paintable>();
                    if (p != null)
                    {
                            if (magicCirclePrefab != null)
                            {
                                // �ڵ��淨�߷�����ʵ������������Ч
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
                        // 1) ��� Paintable ������ħ������Ч
                        Paintable p = paintableTarget != null 
                                        ? paintableTarget 
                                        : hit.collider.GetComponent<Paintable>();
                        if (p != null)
                        {
                            // 2) �Ե������Ϳɫ
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

                        // 3) �����λ������������
                        buffer.Add(hit.point);

                        // ��������ʱ��ͬ����ղ� Yield
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

        // ���һ��
        if (buffer.Count > 0)
        {
            growthController.GrowFlowersLocally(buffer, false);
            buffer.Clear();
        }

        Debug.Log($"[�����������] ���ӵ���: {seedPoints.Count}��ÿ�㻨��: {flowersPerSeed}�����뾶: {maxGrowthRadius}");
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
