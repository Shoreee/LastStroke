using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Player_Controller player = other.GetComponent<Player_Controller>();
        if (player != null)
        {
            Debug.Log("已成功设置复活点");
            CheckpointManager.instance.SetCurrentCheckpoint(this);
        }
    }

    // 提供一个方法来获取重生位置和旋转
    public Transform GetRespawnTransform()
    {
        return transform;
    }
}