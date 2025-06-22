using Photon.Pun;
using UnityEngine;
/// <summary>
/// 免耗能 Core：拾取后在玩家身上触发免能耗效果
/// </summary>
public class HeartCore : BaseEffectCore
{
    [Header("免耗能持续时间")]
    public float duration = 8f;

    protected override bool NeedPhysics() => true;

    protected override void ApplyEffect(Player_Controller player)
    {
        if (player.photonView.IsMine)
            player.photonView.RPC(
                "RPC_StartNoEnergyConsumption", RpcTarget.All,
                duration
            );
    }

    protected override void ApplyEffect(Boss_Controller boss)
    {
        if (boss.photonView.IsMine)
        {
            boss.photonView.RPC(
                "RPC_StartNoEnergyConsumption", RpcTarget.All,
                duration);
        }
    }

    protected override void CheckProximity() { }
}