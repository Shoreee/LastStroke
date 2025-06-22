using Photon.Pun;
using UnityEngine;

/// <summary>
/// 加速 Core：拾取后在玩家身上触发速度加成
/// </summary>
public class StarCore : BaseEffectCore
{
    [Header("加速设置")]
    public float speedMultiplier = 2f;
    public float duration       = 8f;

    protected override bool NeedPhysics() => true;

    protected override void ApplyEffect(Player_Controller player)
    {
        if (player.photonView.IsMine)
            player.photonView.RPC(
                "RPC_ApplySpeedBoost", RpcTarget.All,
                speedMultiplier, duration
            );
    }

    protected override void ApplyEffect(Boss_Controller boss)
    {
        if (boss.photonView.IsMine)
        {
            boss.photonView.RPC(
                "RPC_ApplySpeedBoost", RpcTarget.All,
                speedMultiplier, duration);
        }
    }
    protected override void CheckProximity() { }
}