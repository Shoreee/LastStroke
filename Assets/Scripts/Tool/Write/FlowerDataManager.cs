using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class FlowerDataManager : MonoBehaviourPunCallbacks
{
    public static FlowerDataManager Instance { get; private set; }

    private Dictionary<Vector3, (GameObject flower, float timestamp, bool isVine)> flowerData = new Dictionary<Vector3, (GameObject, float, bool)>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddFlower(Vector3 position, GameObject flower, float timestamp, bool isVine)
    {
        flowerData[position] = (flower, timestamp, isVine);
        // 同步到其他玩家
        //photonView.RPC("RPC_AddFlower", RpcTarget.OthersBuffered, position, flower.GetComponent<PhotonView>().ViewID, Time.time, isVine);
    }
    [PunRPC]
    private void RPC_AddFlower(Vector3 position, int viewID, float timestamp, bool isVine)
    {
        GameObject flower = PhotonView.Find(viewID)?.gameObject;
        if (flower != null)
        {
            flowerData[position] = (flower, timestamp, isVine);
        }
    }


    //public void RemoveFlower(Vector3 position)
    //{
    //    /* if (flowerData.ContainsKey(position))
    //     {
    //         flowerData.Remove(position);
    //     }*/
    //    if (flowerData.TryGetValue(position, out var flowerInfo))
    //    {
    //        flowerData.Remove(position);
//
    //        // 通知其他玩家移除
    //        photonView.RPC("RPC_RemoveFlower", RpcTarget.OthersBuffered, position);
//
    //        // 销毁花朵对象
    //        if (flowerInfo.flower != null)
    //        {
    //            PhotonNetwork.Destroy(flowerInfo.flower);
    //        }
    //    }
    //}
    //[PunRPC]
    //private void RPC_RemoveFlower(Vector3 position)
    //{
    //    if (flowerData.TryGetValue(position, out var flowerInfo))
    //    {
    //        flowerData.Remove(position);
    //        if (flowerInfo.flower != null)
    //        {
    //            PhotonNetwork.Destroy(flowerInfo.flower);
    //        }
    //    }
    //}

    public bool IsPositionOccupied(Vector3 position, float minDistance)
    {
        foreach (var entry in flowerData)
        {
            if (Vector3.Distance(position, entry.Key) < minDistance)
            {
                return true;
            }
        }
        return false;
    }

    public void ClearAllFlowers()
    {
        flowerData.Clear();
    }
}