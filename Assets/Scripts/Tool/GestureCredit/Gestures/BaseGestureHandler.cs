using UnityEngine;
using System.Collections.Generic;
using PDollarGestureRecognizer;
using Photon.Pun;
using NUnit.Framework;
using System.Collections;
using UnityEngine.Audio;   // ← 新增
public abstract class BaseGestureHandler : MonoBehaviourPun
{
    [System.Serializable]
    public class GestureConfig
    {
        [Header("生成设置")]        
        public GameObject corePrefab;
        public float fixedScale = 1f;
        public float heightOffset = 0.5f;

        [Header("合并设置")]
        public float mergeRadius = 3f;
        public bool allowMerge = false;
    }

    [Header("手势出现粒子")]
    [Tooltip("在质心处播放的粒子特效 Prefab")]
    public GameObject gestureAppearPrefab;

    [Header("手势专属短音效 (3D 声)")]
    [Tooltip("将不同手势的短音效拖到这里")]
    public AudioClip gestureSound;
    [Tooltip("音量，0~1")]
    public float     gestureSoundVolume = 1f;

    [Header("输出到 Mixer Group")]
    [Tooltip("拖入 Master 下的 Character AudioMixerGroup")]
    public AudioMixerGroup characterMixerGroup;   // ← 新增

    public GestureConfig config;
    protected List<BaseEffectCore> activeCores = new List<BaseEffectCore>();

    public virtual void HandleGesture(string gestureName, List<Point> points, float skillScale, bool isDirty)
    {
        // 计算质心位置并添加高度偏移
        Vector3 spawnPos = GetAdjustedCentroid(points, config.heightOffset);

        // 在质心处播放出现粒子
        if (gestureAppearPrefab != null)
        {
            photonView.RPC("RPC_PlayGestureParticle", RpcTarget.All, spawnPos);
        }
        /*if (gestureAppearPrefab != null)
        {
            GameObject appear = Instantiate(gestureAppearPrefab, spawnPos, Quaternion.identity);
            ParticleSystem ps = appear.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                // 计算销毁时间：持续时间 + 最大生命期
                var main = ps.main;
                float lifetime = main.duration + (main.startLifetime.constantMax);
                Destroy(appear, lifetime);
            }
            else
            {
                // 若无ParticleSystem组件，默认延时销毁
                Destroy(appear, 2f);
            }
        }*/

        if (isDirty)
        {
            if (config.allowMerge) TryMergeExistingCores(spawnPos);
            else CreateNewCore(spawnPos, skillScale);
        }
    }
    [PunRPC]
    public void RPC_PlayGestureParticle(Vector3 position)
    {
        // —— 同步播放粒子 —— 
        GameObject appear = PhotonNetwork.Instantiate(
            gestureAppearPrefab.name,
            position,
            Quaternion.identity
        );
        var ps = appear.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(DestroyWithDelay(appear, lifetime));
        }
        else
        {
            StartCoroutine(DestroyWithDelay(appear, 2f));
        }

        // —— 在指定 Mixer Group 播放 3D 音效 —— 
        if (gestureSound != null)
        {
            // 创建临时游戏对象承载 AudioSource
            GameObject sfxGO = new GameObject("GestureSFX");
            sfxGO.transform.position = position;
            var aSrc = sfxGO.AddComponent<AudioSource>();
            aSrc.clip = gestureSound;
            aSrc.volume = gestureSoundVolume;
            aSrc.spatialBlend = 0.5f;                       // 完全 3D 声
            aSrc.outputAudioMixerGroup = characterMixerGroup; // 输出到 Character 轨道
            aSrc.Play();
            Destroy(sfxGO, gestureSound.length + 0.1f);
        }
    }
    
    private IEnumerator DestroyWithDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay); // 等待指定的延迟时间
        PhotonNetwork.Destroy(obj); // 使用 Photon 的方法销毁对象
    }

    protected virtual void TryMergeExistingCores(Vector3 position)
    {
        BaseEffectCore existingCore = activeCores.Find(c => 
            Vector3.Distance(c.transform.position, position) < config.mergeRadius);

        if (existingCore != null)
        {
            HandleCoreMerge(existingCore);
        }
        else
        {
            CreateNewCore(position, 1f); // 新生成使用基础scale
        }
    }

    protected virtual void HandleCoreMerge(BaseEffectCore existingCore)
    {
        // 子类实现合并逻辑（如增强效果）
        existingCore.Dispose();
        activeCores.Remove(existingCore);
    }

    protected virtual void CreateNewCore(Vector3 position, float skillScale)
    {
        //GameObject newCore = Instantiate(config.corePrefab, position, Quaternion.identity);
        GameObject newCore = PhotonNetwork.Instantiate(
        config.corePrefab.name,
        position,
        Quaternion.identity
    );
        BaseEffectCore coreComponent = newCore.GetComponent<BaseEffectCore>();
        coreComponent.Initialize(skillScale);
        activeCores.Add(coreComponent);
    }

    #region 辅助方法

    // 计算手势点集的几何中心
    protected Vector3 CalculateCentroid(List<Point> points)
    {
        Vector2 sum = Vector2.zero;
        foreach (Point point in points)
        {
            sum.x += point.X;
            sum.y += point.Y;
        }
        return sum / points.Count;
    }

    // 根据屏幕点集计算世界坐标下的中心位置
    protected Vector3 GetWorldCentroid(List<Point> points)
    {
        Vector2 centroid = CalculateCentroid(points);
        Vector3 screenPosition = new Vector3(centroid.x, -centroid.y, GestureEffectManager.Instance.RecognizorCamera.nearClipPlane);
        Vector3 worldPosition = GestureEffectManager.Instance.RecognizorCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.y = GetCollisionYAtPosition(centroid);
        return worldPosition;
    }

    // 获取指定屏幕位置在碰撞层上的 y 坐标
    public float GetCollisionYAtPosition(Vector2 position)
    {
        Vector3 worldPosition = GestureEffectManager.Instance.RecognizorCamera.ScreenToWorldPoint(
            new Vector3(position.x, -position.y, GestureEffectManager.Instance.RecognizorCamera.nearClipPlane));
        Ray ray = new Ray(worldPosition, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, GestureEffectManager.Instance.paintableLayerMask))
            return hit.point.y;
        return 0f;
    }

    // 根据指定高度偏移调整中心位置
    protected Vector3 GetAdjustedCentroid(List<Point> points, float heightOffset)
    {
        Vector3 position = GetWorldCentroid(points);
        position.y += heightOffset;
        return position;
    }

    #endregion
}
