using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Player_Controller player = other.GetComponent<Player_Controller>();
        if (player != null)
        {
            Debug.Log("�ѳɹ����ø����");
            CheckpointManager.instance.SetCurrentCheckpoint(this);
        }
    }

    // �ṩһ����������ȡ����λ�ú���ת
    public Transform GetRespawnTransform()
    {
        return transform;
    }
}