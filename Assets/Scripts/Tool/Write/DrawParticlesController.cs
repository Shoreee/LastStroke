using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DrawParticlesController : MonoBehaviourPunCallbacks
{

    [Header("�������ͼ�����룩")]
    [Tooltip("������ײ�㲢ÿ�� sendInterval ��������һ��")]
    public float sendInterval = 0.1f;

    public Color paintColor;
    public float minRadius = 0.05f;
    public float maxRadius = 0.2f;
    public float strength = 1;
    public float hardness = 1;

    public float revivalSpeed = 1f;

    // ���õ���ײ�¼��б����� new
    private List<ParticleCollisionEvent> collisionEvents;
    // ��Ŀ�� ViewID ���黺�����ײ���б�
    private Dictionary<int, List<Vector3>> bufferByView;
    private float timer = 0f;

    // ��ǰ�Ƿ�ǿ��״̬�����ⲿ���� SetEnhancedState ����
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
    /// ����ǿ��״̬�����ⲿ���ã��� UI ��ť��
    /// </summary>
    public void SetEnhancedState(bool enhanced)
    {
        isEnhanced = enhanced;
    }

    /// <summary>
    /// ����������������ײʱ Unity �Զ��ص�
    /// </summary>
    void OnParticleCollision(GameObject other)
    {
        // ���� List������ GC Alloc:contentReference[oaicite:11]{index=11}
        collisionEvents.Clear();
        int numCollisions =part.GetCollisionEvents(other, collisionEvents);

        if (numCollisions == 0) return;

        // ��ȡ��ײĿ��� ViewID��0 ��ʾ�� PhotonView��
        PhotonView otherPV = other.GetComponent<PhotonView>();
        int viewID = otherPV != null ? otherPV.ViewID : 0;

        // �� viewID ȡ���½����б�
        if (!bufferByView.TryGetValue(viewID, out var list))
        {
            list = new List<Vector3>(numCollisions);
            bufferByView[viewID] = list;
        }
        Paintable p = other.GetComponent<Paintable>();
        if (p != null)
        {
            List<Vector3> paintPath = new List<Vector3>();  // �洢Ϳɫ·��

            // �ۻ���ײ��
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

        // �ﵽ���ͼ������������
        timer = 0f;

        foreach (var kv in bufferByView)
        {
            int viewID = kv.Key;
            var ptsList = kv.Value;
            if (ptsList.Count == 0) continue;

            // ���Ʋ���ջ��棬��֤���������Ͽ��Լ����ۻ�
            Vector3[] ptsArray = ptsList.ToArray();
            ptsList.Clear();

            if (PhotonNetwork.IsMasterClient)
            {
                // �����ͻ���ֱ�ӹ㲥�������ˣ������Լ���:contentReference[oaicite:12]{index=12}
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
                // �����ͻ��˽��������ͻ��ˣ����ͻ����յ�����ͳһ�㲥
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
