using UnityEngine;
using PDollarGestureRecognizer;

// �̳� BaseGestureHandler ��TriangleEffectHandler ������ȫ���������ʵ��
// �������Ҫ����������ɱ��ֿտ�
public class TriangleEffectHandler : BaseGestureHandler
{
    public static TriangleEffectHandler Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
    }

}
