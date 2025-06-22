using System.Collections.Generic;
using UnityEngine;

public class EraseController : MonoBehaviour
{
    public float detectionAngle = 60f;  // ���εĽǶ�
    public float detectionDistance = 10f;  // ���εļ�����
    public string eventName = "OnFlowersBlown";  // �¼�����

    // ʹ�ô���Ĳ���·���㷢���¼�
    public void EraseFlowersAtPath(List<Vector3> erasePath)
    {
        // ������������ķ��򣺻�ȡ��һ��·���㵽��ǰλ�õ�����
        if (erasePath.Count == 0) return;

        Vector3 forwardDirection = erasePath[0] - transform.position;
        forwardDirection.Normalize(); // ��������εĳ���

        List<Vector3> affectedPositions = new List<Vector3>();

        foreach (var position in erasePath)
        {
            // �ж�·�����Ƿ������η�Χ��
            Vector3 directionToFlower = position - transform.position;
            float angle = Vector3.Angle(forwardDirection, directionToFlower);

            if (angle <= detectionAngle / 2)
            {
                // ���·�����ڼ�ⷶΧ�ڣ���ӵ���Ӱ���λ���б�
                affectedPositions.Add(position);
            }
        }

        // ����л����ڷ�Χ�ڣ������¼�
        if (affectedPositions.Count > 0)
        {
            EventManager.Instance.DispatchEvent(eventName, affectedPositions.ToArray());
        }
    }
}
