using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareEffectHandler : BaseGestureHandler
{
    public static SquareEffectHandler Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }

}
