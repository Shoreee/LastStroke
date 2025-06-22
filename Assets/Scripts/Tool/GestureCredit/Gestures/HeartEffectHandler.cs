using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartEffectHandler : BaseGestureHandler
{
    public static HeartEffectHandler Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }

}
