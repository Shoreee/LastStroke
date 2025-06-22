using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TriangleCore : BaseEffectCore
{
    [Header("恢复设置")]
    public float energyRecovery = 30f;

    protected override bool NeedPhysics() => true;

    protected override void ApplyEffect(Player_Controller player)
    {
        if (player.photonView.IsMine)
        {
            player.photonView.RPC("RPC_AddEnergy", RpcTarget.All, energyRecovery);
        }
        //player.Energy += energyRecovery;
    }

    protected override void ApplyEffect(Boss_Controller boss)
    {
        if (boss.photonView.IsMine)
        {
            boss.photonView.RPC("RPC_AddEnergy", RpcTarget.All, energyRecovery);
        }
    }

    protected override void CheckProximity() { }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }
}