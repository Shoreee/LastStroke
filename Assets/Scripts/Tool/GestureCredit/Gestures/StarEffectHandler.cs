using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarEffectHandler : BaseGestureHandler
{
        public static StarEffectHandler Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }

}
