using UnityEngine;
using DG.Tweening;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
public class FlowerController : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    [Header("Attraction Settings")]
    public float destroyDistance = 0.5f;
    private Transform bossTransform;
    private float beamWidth;
    private float beamLength;
    private float moveSpeed;

    [Header("Collision Settings")]
    public float pushForce = 2f;
    public float returnSpeed = 2f;
    public float smoothness = 0.1f;

    private bool isPushed = false;
    private Vector3 originalPosition;
    private Vector3 pushDirection;
    private float pushDistance;

    private bool isCollidingWithPlayer = false;
    private bool isReturning = false;
    private bool isAttracted;
    private Vector3 targetPosition;


    private void OnEnable()
    {
        ResetState();
        StartCoroutine(InitAnimation());
    }

    private IEnumerator InitAnimation()
    {
        yield return null;
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack)
            .OnStart(() => {
                EventManager.Instance.Regist("OnBossBeamActive", OnBossBeamActive);
            });
    }

    private void ResetState()
    {
        isAttracted = false;
        isPushed = false;
        isReturning = false;
        isCollidingWithPlayer = false;
        bossTransform = null;
        transform.localScale = Vector3.one;
        StopAllCoroutines();
        DOTween.Kill(transform);
    }

    private void OnDisable()
    {
        EventManager.Instance.UnRegist("OnBossBeamActive", OnBossBeamActive);
        ResetState();
    }

    private void OnBossBeamActive(object[] args)
    {
        if (isAttracted) return;

        if (args[0] is GameObject)
        {
            GameObject bossObj = (GameObject)args[0];
            bossTransform = bossObj.transform;

            Vector3 bossForward = (Vector3)args[1];
            beamLength = (float)args[2];
            beamWidth = (float)args[3];
            moveSpeed = (float)args[4];

            if (IsInBeamArea(bossTransform.position, bossForward))
            {
                StartAttraction();
            }
        }
        else
        {
            Debug.LogError("Invalid Boss reference type: " + args[0].GetType());
        }
    }

    private bool IsInBeamArea(Vector3 bossPosition, Vector3 bossForward)
    {
        Vector3 toFlower = transform.position - bossPosition;
        float distanceAlongBeam = Vector3.Dot(toFlower, bossForward.normalized);
        float lateralDistance = Vector3.Cross(bossForward.normalized, toFlower).magnitude;

        return distanceAlongBeam > 0 &&
               distanceAlongBeam < beamLength &&
               lateralDistance < beamWidth / 2;
    }

    private void StartAttraction()
    {
        isAttracted = true;
        StartCoroutine(AttractRoutine());
    }

    private IEnumerator AttractRoutine()
    {
        while (bossTransform != null &&
               Vector3.Distance(transform.position, bossTransform.position) > destroyDistance)
        {
            Vector3 targetPos = bossTransform.position + bossTransform.forward * 1f;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.DOScale(Vector3.one * 0.8f, 1.0f) // 缩小幅度，延长持续时间
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
            });
    }

    private Vector3 RoundPosition(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x * 100) / 100,
            Mathf.Round(position.y * 100) / 100,
            Mathf.Round(position.z * 100) / 100
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!this.gameObject.activeSelf) return;

        if (true)
        {
            isCollidingWithPlayer = true;

            if (!isReturning)
            {
                originalPosition = transform.position;
                StopAllCoroutines();
                CalculatePushDirection(other.transform.position);
                StartPushEffectWithDOTween();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!this.gameObject.activeSelf) return;

        if (other.CompareTag("Player"))
        {
            isCollidingWithPlayer = false;

            if (!isReturning && !isPushed)
            {
                StartCoroutine(ReturnToOriginalPosition());
            }
        }
    }

    private void StartPushEffectWithDOTween()
    {
        if (!this.gameObject.activeSelf) return; // 确保对象是激活状态

        isPushed = true;

        // 计算目标位置
        Vector3 targetPos = originalPosition + pushDirection * (pushForce * pushDistance);
        targetPos.y = originalPosition.y;  // 锁定 Y 轴

        // 创建 DOTween 动画序列
        Sequence pushSequence = DOTween.Sequence();

        // 向下并弹性跳跃
        pushSequence.Append(transform.DOMoveY(originalPosition.y - 0.2f, smoothness / 0.3f)
                    .SetEase(Ease.InBounce));

        // 弹回并恢复
        pushSequence.Append(transform.DOMoveY(originalPosition.y, smoothness / 0.3f)
                    .SetEase(Ease.OutBounce));

        // 移动到目标位置
        pushSequence.Append(transform.DOMove(targetPos, smoothness)
                    .SetEase(Ease.OutQuad));

        // 挤出结束后自动开始返回
        pushSequence.OnComplete(() =>
        {
            if (this.gameObject.activeSelf) // 确保对象是激活状态
            {
                StartCoroutine(ReturnToOriginalPosition());
            }
        });
    }

    private IEnumerator ReturnToOriginalPosition()
    {
        if (!this.gameObject.activeSelf) yield break;

        isReturning = true;
        Vector3 startPos = transform.position;
        float t = 0;

        while (Vector3.Distance(transform.position, originalPosition) > 0.01f)
        {
            if (!this.gameObject.activeSelf) yield break;

            if (isCollidingWithPlayer)
            {
                yield return new WaitUntil(() => !isCollidingWithPlayer || !this.gameObject.activeSelf);
                if (!this.gameObject.activeSelf) yield break;

                startPos = transform.position;
                t = 0;
            }

            t += Time.deltaTime * returnSpeed;
            Vector3 newPosition = Vector3.Lerp(startPos, originalPosition, Mathf.SmoothStep(0, 1, t));
            newPosition.y = originalPosition.y;
            transform.position = newPosition;
            //foreach (var material in leafMaterials)
            //{
            //    material.SetVector("_Pos", newPosition);
            //}

            yield return null;
        }

        if (this.gameObject.activeSelf)
        {
            transform.position = originalPosition;
            isPushed = false;
            isReturning = false;
        }
    }

    private void CalculatePushDirection(Vector3 playerPosition)
    {
        Vector3 direction = transform.position - playerPosition;
        direction.z = 0;
        direction.x = 0;
        pushDirection = direction.normalized;
        pushDistance = direction.magnitude;
    }

    private void OnDestroy()
    {
        EventManager.Instance.UnRegist("OnBossBeamActive", OnBossBeamActive);
        DOTween.Kill(transform);
    }
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // 从 PhotonView 拿到 instantiationData
        object[] data = photonView.InstantiationData;
        if (data != null && data.Length >= 3)
        {
            // 解析三个 float
            float sx = (float)data[0];
            float sy = (float)data[1];
            float sz = (float)data[2];
            transform.localScale = new Vector3(sx, sy, sz);
        }
    }
}