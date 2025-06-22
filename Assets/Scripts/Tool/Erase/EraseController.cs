using System.Collections.Generic;
using UnityEngine;

public class EraseController : MonoBehaviour
{
    public float detectionAngle = 60f;  // 扇形的角度
    public float detectionDistance = 10f;  // 扇形的检测距离
    public string eventName = "OnFlowersBlown";  // 事件名称

    // 使用传入的擦除路径点发出事件
    public void EraseFlowersAtPath(List<Vector3> erasePath)
    {
        // 计算扇形区域的方向：获取第一个路径点到当前位置的向量
        if (erasePath.Count == 0) return;

        Vector3 forwardDirection = erasePath[0] - transform.position;
        forwardDirection.Normalize(); // 计算出扇形的朝向

        List<Vector3> affectedPositions = new List<Vector3>();

        foreach (var position in erasePath)
        {
            // 判断路径点是否在扇形范围内
            Vector3 directionToFlower = position - transform.position;
            float angle = Vector3.Angle(forwardDirection, directionToFlower);

            if (angle <= detectionAngle / 2)
            {
                // 如果路径点在检测范围内，添加到受影响的位置列表
                affectedPositions.Add(position);
            }
        }

        // 如果有花朵在范围内，触发事件
        if (affectedPositions.Count > 0)
        {
            EventManager.Instance.DispatchEvent(eventName, affectedPositions.ToArray());
        }
    }
}
