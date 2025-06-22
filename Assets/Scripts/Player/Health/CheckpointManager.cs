using UnityEngine;
using System.Collections;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    private Checkpoint currentCheckpoint;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 设置当前检查点
    public void SetCurrentCheckpoint(Checkpoint checkpoint)
    {
        currentCheckpoint = checkpoint;
    }

    // 玩家重生的方法
/*    public void RespawnPlayerAtCheckpoint(Player_Controller player)
    {
        if (currentCheckpoint != null)
        {
            Transform respawnTransform = currentCheckpoint.GetRespawnTransform();
            player.transform.position = respawnTransform.position;
            player.transform.rotation = respawnTransform.rotation;
            player.Respawn(); // 用于恢复生命值等
        }
        else
        {
            Debug.LogWarning("没有找到复活点");
        }
    }
*/    // 定义一个协程
    IEnumerator WaitForSeconds(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
    }

}