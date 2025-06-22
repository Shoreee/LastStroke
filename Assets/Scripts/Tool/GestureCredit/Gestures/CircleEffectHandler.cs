using UnityEngine;
using System.Collections.Generic;
using PDollarGestureRecognizer;


public class CircleEffectHandler : BaseGestureHandler
{
    public static CircleEffectHandler Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }

}
