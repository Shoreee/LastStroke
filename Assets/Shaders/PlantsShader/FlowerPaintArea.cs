using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FlowerPaintArea : MonoBehaviour
{
    public List<Vector3> flowerPositions = new List<Vector3>();
    public float gizmoSize = 0.2f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        foreach (var pos in flowerPositions)
        {
            Gizmos.DrawSphere(pos, gizmoSize);
        }
    }

    public void AddPoint(Vector3 worldPos)
    {
        if (!flowerPositions.Contains(worldPos))
        {
            flowerPositions.Add(worldPos);
        }
    }

    public void ClearAll()
    {
        flowerPositions.Clear();
    }
}
