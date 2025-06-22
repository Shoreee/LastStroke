using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using PDollarGestureRecognizer;

public class EraseParticlesController : MonoBehaviourPun
{
    [Header("Paint Settings")]
    public Color paintColor;
    public float minRadius = 0.3f;
    public float maxRadius = 0.5f;
    public float strength = 1f;
    public float hardness = 1f;

    private ParticleSystem part;
    private StrokeRecognizor strokeRecognizor;
    private FlowerPainterRuntime _cachedPainter;
    private List<ParticleCollisionEvent> collisionEvents;
    private List<Vector3> erasePoints = new List<Vector3>();

    void Start()
    {
        part = GetComponent<ParticleSystem>();

        // ??? collisionEvents ??
        int safeSize = ParticlePhysicsExtensions.GetSafeCollisionEventSize(part); 
        collisionEvents = new List<ParticleCollisionEvent>(safeSize);

        strokeRecognizor = GameObject.Find("RecognizorCamera")
                                  .GetComponent<StrokeRecognizor>();
        _cachedPainter = FindObjectOfType<FlowerPainterRuntime>();
    }

    void OnParticleCollision(GameObject other)
    {
        // ????????
        int count = part.GetCollisionEvents(other, collisionEvents);
        if (count == 0) return;

        // ??????? Paintable
        erasePoints.Clear();
        bool hasPaint = other.TryGetComponent<Paintable>(out var paintable);
        float radius = Random.Range(minRadius, maxRadius);

        for (int i = 0; i < count; i++)
        {
            var pos = collisionEvents[i].intersection;
            erasePoints.Add(pos);

            if (hasPaint)
                PaintManager.instance.paint(
                    paintable, pos, radius, hardness, strength, paintColor
                );
        }

        // ?? RPC ??
        if (_cachedPainter != null)
        {
            _cachedPainter.photonView.RPC(
                "RPC_EraseFlowersBatch",
                RpcTarget.All,
                erasePoints.ToArray(), radius
            );
        }
        string roleID = PhotonNetwork.LocalPlayer.ActorNumber.ToString();

        // ????
        strokeRecognizor?.ErasePoints(erasePoints, maxRadius * 2f,roleID);
    }
}
