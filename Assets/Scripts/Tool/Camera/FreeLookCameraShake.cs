using UnityEngine;
using Cinemachine;

public class FreeLookCameraShake : MonoBehaviour
{
    [Header("震动参数")]
    public float shakeDuration = 0.5f;    // 单次震动持续时间
    public float shakeAmplitude = 0.5f;   // 震动幅度
    public float shakeFrequency = 2.0f;   // 震动频率
    
    private float shakeElapsedTime = 0f;
    private int shakeCount = 0;
    private int targetShakeCount = 0;
    
    // Cinemachine FreeLook的噪音设置
    private CinemachineFreeLook freeLookCamera;
    private CinemachineBasicMultiChannelPerlin[] noiseComponents;

    void Start()
    {
        // 获取FreeLook相机组件
        freeLookCamera = GetComponent<CinemachineFreeLook>();
        if (freeLookCamera == null)
        {
            Debug.LogError("没有找到CinemachineFreeLook组件!");
            return;
        }

        // 初始化噪音组件数组（FreeLook有三个轨道，每个轨道都需要设置）
        noiseComponents = new CinemachineBasicMultiChannelPerlin[3];
        for (int i = 0; i < 3; i++)
        {
            noiseComponents[i] = freeLookCamera.GetRig(i).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noiseComponents[i] == null)
            {
                Debug.LogError($"无法在第{i}个轨道上找到噪音组件!");
            }
        }
    }
    void Update()
    {
        // 处理震动逻辑
        if (shakeCount < targetShakeCount)
        {
            shakeElapsedTime += Time.deltaTime;

            // 如果当前震动时间结束
            if (shakeElapsedTime >= shakeDuration)
            {
                shakeElapsedTime = 0f;
                shakeCount++;
                
                // 如果不是最后一次震动，短暂停止
                if (shakeCount < targetShakeCount)
                {
                    SetNoise(0f, 0f); // 停止震动
                }
            }
            else
            {
                // 应用震动
                SetNoise(shakeAmplitude, shakeFrequency);
            }
        }
        else
        {
            // 震动结束，确保停止
            SetNoise(0f, 0f);
        }
    }


    // 开始震动
    public void StartShake(int count)
    {
        if (count <= 0) return;
        
        targetShakeCount = count;
        shakeCount = 0;
        shakeElapsedTime = 0f;
    }

    // 设置所有轨道的噪音参数
    private void SetNoise(float amplitude, float frequency)
    {
        if (noiseComponents == null) return;
        
        foreach (var noise in noiseComponents)
        {
            if (noise != null)
            {
                noise.m_AmplitudeGain = amplitude;
                noise.m_FrequencyGain = frequency;
            }
        }
    }
}