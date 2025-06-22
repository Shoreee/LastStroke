using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// PoisonAppleCore.cs
public class PoisonAppleCore : BaseEffectCore, IPunObservable
{
    [Header("爆炸设置")]
    public float explodeDelay = 5f;
    public float explosionRadius = 5f;
    public float explosionForce = 10f;
    public GameObject explosionPrefab; // 独立预制体
    public float explosionLifetime = 2f;
    public Color warningColor = Color.red;

    [Header("减速效果配置")]
    [Tooltip("减速到原速度的多少倍")]
    public float slowFactor = 0.5f;
    [Tooltip("保持减速状态的时间（秒）")]
    public float slowDuration = 2f;

    private Material material;
    private Color originalColor;
    protected override void ApplyEffect(Player_Controller player) { }
    protected override void ApplyEffect(Boss_Controller bossr) { }

    protected override bool NeedPhysics() => false;

    protected override void CheckProximity() { }

    public override bool CanBePickedUp => false;

    private void Start()
    {
        material = GetComponent<Renderer>().material;
        originalColor = material.GetColor("_Base_Diffuse1");
        if (photonView.IsMine)
        {
            StartCoroutine(ExplosionCountdown());
        }
    }

    private IEnumerator ExplosionCountdown()
    {
        float timer = 0f;
        // 定义最慢和最快的闪烁速度（循环次数／秒）
        float minBlinkSpeed = 1f;
        float maxBlinkSpeed = 10f;
    
        while (timer < explodeDelay)
        {
            // 进度 0→1
            float t = timer / explodeDelay;
    
            // 根据进度从 min→max 插值出当前闪烁速度
            float currentSpeed = Mathf.Lerp(minBlinkSpeed, maxBlinkSpeed, t);
    
            // 用 timer * currentSpeed 来驱动 PingPong，timer 从 0→explodeDelay
            float lerp = Mathf.PingPong(timer * currentSpeed, 1f);
    
            // 最终颜色在原色和警告色之间来回
            Color lerpedColor = Color.Lerp(originalColor, warningColor, lerp);
            material.SetColor("_Base_Diffuse1", lerpedColor);
    
            timer += Time.deltaTime;
            yield return null;
        }

        //Explode();
        // 修改为网络同步爆炸
        photonView.RPC("RPC_Explode", RpcTarget.All);
        yield break;
    }

    // 修改毒苹果的爆炸调用方式
/*private void Explode()
{
    // 创建独立粒子实例
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
        
        // 播放并自动销毁
        ps.Play();
        Destroy(explosion, ps.main.duration > 0 ? ps.main.duration : explosionLifetime);

    Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
    foreach (var col in colliders)
    {
        Player_Controller player = col.GetComponent<Player_Controller>();
        if (player != null)
        {
            player.ApplySlowEffect(slowFactor, slowDuration);
            
            // 计算击退方向
            Vector3 dir = (player.transform.position - transform.position).normalized;
            player.ApplyKnockback(dir, explosionForce, 0.5f);
        }
    }
    Dispose();
}*/
    [PunRPC]
    private void RPC_Explode()
    {
        // 所有客户端都执行爆炸效果
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            ps.Play();
            Destroy(explosion, ps.main.duration > 0 ? ps.main.duration : explosionLifetime);

        // 只有主机处理伤害和击退逻辑
        //if (PhotonNetwork.IsMasterClient)
        //{
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var col in colliders)
            {
                Player_Controller player = col.GetComponent<Player_Controller>();
                if (player != null)
                {
                    player.photonView.RPC("RPC_ApplySlowEffect", RpcTarget.All, slowFactor, slowDuration);

                    Vector3 dir = (player.transform.position - transform.position).normalized;
                    player.photonView.RPC("RPC_ApplyKnockback", RpcTarget.All, dir, explosionForce, 0.5f);
                }
            }
        //}

        // 销毁苹果（同步处理）
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
    }
}

