using UnityEngine;

[ExecuteAlways]
public class SimpleLeafSwing : MonoBehaviour
{
    public float swayAmplitude = 1f;      // 摆动幅度
    public float swayFrequency = 1f;      // 摆动频率
    public Vector3 swayAxis = new Vector3(0, 0, 1); // 摆动轴
    public float randomOffset = 0f;       // 每朵花的随机偏移

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
        randomOffset = Random.Range(0f, 100f); // 每朵花一个不同的相位偏移
    }

    void Update()
    {
        float sway = Mathf.Sin(Time.time * swayFrequency + randomOffset) * swayAmplitude;
        transform.localRotation = initialRotation * Quaternion.AngleAxis(sway, swayAxis.normalized);
    }
}
