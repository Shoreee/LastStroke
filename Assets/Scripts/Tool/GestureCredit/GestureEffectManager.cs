using UnityEngine;
using System.Collections.Generic;
using PDollarGestureRecognizer; // 假设 Point 类型在此定义

public class GestureEffectManager : MonoBehaviour
{
    private static GestureEffectManager _instance;
    public static GestureEffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GestureEffectManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("GestureEffectManager");
                    _instance = obj.AddComponent<GestureEffectManager>();
                }
            }
            return _instance;
        }
    }

    [Header("References")]
    [SerializeField] private Camera _recognizorCamera;
    public Camera RecognizorCamera => _recognizorCamera;

    [Header("Collision Settings")]
    public LayerMask paintableLayerMask;
    public TriangleEffectHandler triangleHandler;
    public CircleEffectHandler circleHandler;
    public HeartEffectHandler heartHandler;
    public MoonEffectHandler moonHandler;
    public StarEffectHandler starHandler;
    public SquareEffectHandler squareHandler;
    public NullEffectHandler nullHandler;

    [Header("Scale Mapping")]
    public float mappedScaleMin = 0.5f;
    public float mappedScaleMax = 2.0f;
    public float originalScaleMin = 0.1f;
    public float originalScaleMax = 1.0f;

    public void HandleGestureWithScale(string gestureName, List<Point> points, bool isDirty,float confidence)
    {
        float rawScale = CalculateRawScale(points);
        float mappedScale = MapScaleToRange(rawScale, mappedScaleMin, mappedScaleMax);
        Debug.Log("识别"+gestureName+"为"+confidence);

        // 根据识别结果调用对应 Handler 的处理
        if(confidence<0.4)
        {
            nullHandler.HandleGesture(gestureName, points, mappedScale, isDirty);
        }
        else
        {
            switch (gestureName)
            {
                case "triangle":
                    triangleHandler.HandleGesture(gestureName, points, mappedScale, isDirty);
                    break;
                case "square":
                    squareHandler.HandleGesture(gestureName, points, mappedScale, isDirty);
                    break;
                case "circle":
                    circleHandler.HandleGesture(gestureName, points, mappedScale, isDirty);
                    break;
                case "heart":
                    heartHandler.HandleGesture(gestureName, points, mappedScale, isDirty);
                    break;
                case "moon":
                    moonHandler.HandleGesture(gestureName, points, mappedScale, isDirty);
                    break;
                case "stars":
                    starHandler.HandleGesture(gestureName, points, mappedScale, isDirty);
                    break;
                default:
                    nullHandler.HandleGesture(gestureName, points, mappedScale, isDirty);
                    break;
            }
        }
    }

    private float CalculateRawScale(List<Point> points)
    {
        if (points == null || points.Count == 0)
            return 0f;
        
        float minx = float.MaxValue, miny = float.MaxValue;
        float maxx = float.MinValue, maxy = float.MinValue;
        
        foreach (var point in points)
        {
            if (point.X < minx) minx = point.X;
            if (point.Y < miny) miny = point.Y;
            if (point.X > maxx) maxx = point.X;
            if (point.Y > maxy) maxy = point.Y;
        }
        return Mathf.Max(maxx - minx, maxy - miny);
    }

    private float MapScaleToRange(float scale, float newMin, float newMax)
    {
        float mapped = newMin + (scale - originalScaleMin) * (newMax - newMin) / (originalScaleMax - originalScaleMin);
        return Mathf.Clamp(mapped, newMin, newMax);
    }

    public void RequestRemovePoints(Vector3 position, float radius)
    {
        Vector3 screenPos = RecognizorCamera.WorldToScreenPoint(position);
        Point erasePoint = new Point(screenPos.x, -screenPos.y, -1);
        
        FindObjectOfType<StrokeRecognizor>()?.ErasePoints(
            new List<Vector3>{ position },
            radius,"DefaultRole"
        );
    }
}
