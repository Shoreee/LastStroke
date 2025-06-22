using UnityEngine;
using System.Collections.Generic;
using PDollarGestureRecognizer;


public class NullEffectHandler : BaseGestureHandler
{
    public static NullEffectHandler Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void HandleGesture(string gestureName, List<Point> points, float skillScale, bool isDirty)
    {
        // 计算质心位置并添加高度偏移
        Vector3 spawnPos = GetAdjustedCentroid(points, config.heightOffset);

        // 只播放出现粒子，不生成任何 core
        if (gestureAppearPrefab != null)
        {
            GameObject appear = Instantiate(gestureAppearPrefab, spawnPos, Quaternion.identity);
            ParticleSystem ps = appear.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                var main = ps.main;
                float lifetime = main.duration + (main.startLifetime.constantMax);
                Destroy(appear, lifetime);
            }
            else
            {
                Destroy(appear, 2f);
            }
        }
        // 不调用 base.HandleGesture，也不生成 core
    }
}
