using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;
public interface IGestureHandler
{
    void HandleGesture(string gestureName, List<Point> points, float mappedScale);
}