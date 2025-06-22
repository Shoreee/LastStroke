using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;

public class MoonEffectHandler : BaseGestureHandler
{
        public static MoonEffectHandler Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }

}