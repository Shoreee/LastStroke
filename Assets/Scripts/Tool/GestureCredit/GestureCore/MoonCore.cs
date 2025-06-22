using Photon.Pun;
using UnityEngine;
/// <summary>
/// ��Ծ��ǿ Core��ʰȡ����������ϴ�����Ծ�ӳ�
/// </summary>
public class MoonCore : BaseEffectCore
{
    [Header("��Ծ��ǿ����")]
    public float jumpMultiplier = 2f;
    public float duration       = 8f;

    protected override bool NeedPhysics() => true;

    protected override void ApplyEffect(Player_Controller player)
    {
        if (player.photonView.IsMine)
            player.photonView.RPC(
                "RPC_ApplyJumpBoost", RpcTarget.All,
                jumpMultiplier, duration
            );
    }
    

    protected override void ApplyEffect(Boss_Controller boss)
    {
        if (boss.photonView.IsMine)
        {
            boss.photonView.RPC(
                "RPC_ApplyJumpBoost", RpcTarget.All,
                jumpMultiplier, duration);
        }
    }

    protected override void CheckProximity() { }
}