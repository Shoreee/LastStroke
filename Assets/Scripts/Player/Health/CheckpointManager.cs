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

    // ���õ�ǰ����
    public void SetCurrentCheckpoint(Checkpoint checkpoint)
    {
        currentCheckpoint = checkpoint;
    }

    // ��������ķ���
/*    public void RespawnPlayerAtCheckpoint(Player_Controller player)
    {
        if (currentCheckpoint != null)
        {
            Transform respawnTransform = currentCheckpoint.GetRespawnTransform();
            player.transform.position = respawnTransform.position;
            player.transform.rotation = respawnTransform.rotation;
            player.Respawn(); // ���ڻָ�����ֵ��
        }
        else
        {
            Debug.LogWarning("û���ҵ������");
        }
    }
*/    // ����һ��Э��
    IEnumerator WaitForSeconds(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
    }

}