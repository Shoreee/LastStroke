using UnityEngine;
using PDollarGestureRecognizer;

// 继承 BaseGestureHandler 后，TriangleEffectHandler 可以完全依赖基类的实现
// 如果不需要额外操作，可保持空壳
public class TriangleEffectHandler : BaseGestureHandler
{
    public static TriangleEffectHandler Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }

}
