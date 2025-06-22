using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEditor.Rendering;
using TMPro;
using Photon.Realtime;
using Unity.Netcode;

public class TeachManager : MonoBehaviourPunCallbacks
{
    private GameObject currentPlayer;
    public Transform LangSpawnPoint;
    public Transform HongSpawnPoint;
    public static TeachManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("LocalRoom", new RoomOptions { MaxPlayers = 1 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Vector3 pos;
        Transform pointTf = GameObject.Find("PlayerPoint").transform;
        pos = pointTf.GetChild(Random.Range(0, pointTf.childCount)).position;
        currentPlayer = PhotonNetwork.Instantiate("Player", pos, Quaternion.identity);
    }
    public void DestroyAndRespawnHong()
    {
        PhotonNetwork.Destroy(currentPlayer);
        currentPlayer = null;
        Vector3 spawnPos = HongSpawnPoint.position;
        Quaternion spawnRotation = HongSpawnPoint.rotation;
        currentPlayer = PhotonNetwork.Instantiate("Boss", spawnPos, spawnRotation);
    }
    public void DestroyAndRespawnLang()
    {
        PhotonNetwork.Destroy(currentPlayer);
        currentPlayer = null;
        //GameObject target = GameObject.Find("Test_Little_Red_Character_Model9 (1)");
        //Destroy(target);
        Vector3 spawnPos = LangSpawnPoint.position;
        Quaternion spawnRotation = LangSpawnPoint.rotation;
        currentPlayer = PhotonNetwork.Instantiate("Player", spawnPos, spawnRotation);
    }

}
