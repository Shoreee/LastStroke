using UnityEngine;
using System.Collections.Generic;
using PDollarGestureRecognizer;
using Photon.Pun;
using NUnit.Framework;
using System.Collections;
using UnityEngine.Audio;   // �� ����
public abstract class BaseGestureHandler : MonoBehaviourPun
{
    [System.Serializable]
    public class GestureConfig
    {
        [Header("��������")]        
        public GameObject corePrefab;
        public float fixedScale = 1f;
        public float heightOffset = 0.5f;

        [Header("�ϲ�����")]
        public float mergeRadius = 3f;
        public bool allowMerge = false;
    }

    [Header("���Ƴ�������")]
    [Tooltip("�����Ĵ����ŵ�������Ч Prefab")]
    public GameObject gestureAppearPrefab;

    [Header("����ר������Ч (3D ��)")]
    [Tooltip("����ͬ���ƵĶ���Ч�ϵ�����")]
    public AudioClip gestureSound;
    [Tooltip("������0~1")]
    public float     gestureSoundVolume = 1f;

    [Header("����� Mixer Group")]
    [Tooltip("���� Master �µ� Character AudioMixerGroup")]
    public AudioMixerGroup characterMixerGroup;   // �� ����

    public GestureConfig config;
    protected List<BaseEffectCore> activeCores = new List<BaseEffectCore>();

    public virtual void HandleGesture(string gestureName, List<Point> points, float skillScale, bool isDirty)
    {
        // ��������λ�ò���Ӹ߶�ƫ��
        Vector3 spawnPos = GetAdjustedCentroid(points, config.heightOffset);

        // �����Ĵ����ų�������
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
                // ��������ʱ�䣺����ʱ�� + ���������
                var main = ps.main;
                float lifetime = main.duration + (main.startLifetime.constantMax);
                Destroy(appear, lifetime);
            }
            else
            {
                // ����ParticleSystem�����Ĭ����ʱ����
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
        // ���� ͬ���������� ���� 
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

        // ���� ��ָ�� Mixer Group ���� 3D ��Ч ���� 
        if (gestureSound != null)
        {
            // ������ʱ��Ϸ������� AudioSource
            GameObject sfxGO = new GameObject("GestureSFX");
            sfxGO.transform.position = position;
            var aSrc = sfxGO.AddComponent<AudioSource>();
            aSrc.clip = gestureSound;
            aSrc.volume = gestureSoundVolume;
            aSrc.spatialBlend = 0.5f;                       // ��ȫ 3D ��
            aSrc.outputAudioMixerGroup = characterMixerGroup; // ����� Character ���
            aSrc.Play();
            Destroy(sfxGO, gestureSound.length + 0.1f);
        }
    }
    
    private IEnumerator DestroyWithDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay); // �ȴ�ָ�����ӳ�ʱ��
        PhotonNetwork.Destroy(obj); // ʹ�� Photon �ķ������ٶ���
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
            CreateNewCore(position, 1f); // ������ʹ�û���scale
        }
    }

    protected virtual void HandleCoreMerge(BaseEffectCore existingCore)
    {
        // ����ʵ�ֺϲ��߼�������ǿЧ����
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

    #region ��������

    // �������Ƶ㼯�ļ�������
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

    // ������Ļ�㼯�������������µ�����λ��
    protected Vector3 GetWorldCentroid(List<Point> points)
    {
        Vector2 centroid = CalculateCentroid(points);
        Vector3 screenPosition = new Vector3(centroid.x, -centroid.y, GestureEffectManager.Instance.RecognizorCamera.nearClipPlane);
        Vector3 worldPosition = GestureEffectManager.Instance.RecognizorCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.y = GetCollisionYAtPosition(centroid);
        return worldPosition;
    }

    // ��ȡָ����Ļλ������ײ���ϵ� y ����
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

    // ����ָ���߶�ƫ�Ƶ�������λ��
    protected Vector3 GetAdjustedCentroid(List<Point> points, float heightOffset)
    {
        Vector3 position = GetWorldCentroid(points);
        position.y += heightOffset;
        return position;
    }

    #endregion
}
