using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;
using Photon.Pun;
using System.Linq;

public class GrowthController : MonoBehaviour
{
    public GameObject[] flowerPrefabs;

    [Header("强化状态用的花朵 Prefabs")]
    [SerializeField] 
    private GameObject[] enhancedFlowerPrefabs;
    public float minDistanceBetweenFlowers = 0.05f;

    public float minclusterSize =2f; 
    public float maxclusterSize =  5f;
    public float maxDistanceBetweenFlowers = 1.5f; // 最大距离
    public Vector2 flowerScaleRange = new Vector2(0.8f, 1.2f);
    private StrokeRecognizor strokeRecognizor;

    private GameObject[] defaultFlowerPrefabs;


    void Start()
    {
        strokeRecognizor = GameObject.Find("RecognizorCamera").GetComponent<StrokeRecognizor>();
        EventManager.Instance.Regist("clear_flowers", OnClearFlowers);

    }

    void Awake()
    {
        // 备份最初 inspector 里填的默认花朵
        defaultFlowerPrefabs = flowerPrefabs;
    }

    public void SetEnhancedFlowerPrefabs(bool enhanced)
    {
        if (enhanced && enhancedFlowerPrefabs != null && enhancedFlowerPrefabs.Length > 0)
            flowerPrefabs = enhancedFlowerPrefabs;
        else
            flowerPrefabs = defaultFlowerPrefabs;
    }

    void OnDestroy()
    {
        EventManager.Instance.UnRegist("clear_flowers", OnClearFlowers);
    }

    private void OnClearFlowers(object[] args)
    {
        ClearFlowers();
    }

    private IEnumerator GrowFlowerOverTime(Transform flowerTransform, float targetScale, float duration)
    {
        float elapsed = 0f;
        Vector3 initialScale = Vector3.zero;
        Vector3 finalScale = new Vector3(targetScale, targetScale, targetScale);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 使用 ease-out 插值函数（先快后慢）
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            if ( flowerTransform != null)
            {
                flowerTransform.localScale = Vector3.Lerp(initialScale, finalScale, easedT);
            }
            yield return null;
        }
        if ( flowerTransform != null)
        {
            flowerTransform.localScale = finalScale;
        }
    }




    private void GenerateFlowerCluster(Vector3 centerPosition)
    {
        float clusterSize = Random.Range(minclusterSize, maxclusterSize); 


        GameObject selectedPrefab = flowerPrefabs[Random.Range(0, flowerPrefabs.Length)]; // 随机选择一个 Prefab
        if (selectedPrefab == null)
        {
            Debug.LogError("Selected flower prefab is null!");
            return;
        }

        for (int i = 0; i < clusterSize; i++)
        {
            // Debug.Log(clusterSize);
            float angle = Random.Range(0, 360); // 随机角度

            // 使用指数分布来控制距离，使得花朵更靠近中心
            float distance = maxDistanceBetweenFlowers * Mathf.Pow(Random.Range(0f, 1f), 2); // 指数分布，靠近中心的概率更高
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance; // 计算偏移
            Vector3 flowerPosition = centerPosition + offset; // 计算花朵位置
            Vector3 roundedPosition = flowerPosition; // 四舍五入位置

            if (!FlowerDataManager.Instance.IsPositionOccupied(roundedPosition, minDistanceBetweenFlowers))
            {
                object[] initData = new object[]{ 0f, 0f, 0f };
                GameObject flower = PhotonNetwork.Instantiate(selectedPrefab.name, flowerPosition, Quaternion.identity,0, initData);
                if (flower == null)
                {
                    Debug.LogError("Failed to spawn flower from pool!");
                    continue;
                }

                // 随机缩放
                float scale = Random.Range(flowerScaleRange.x, flowerScaleRange.y);


                // 替换这一段
                // flower.transform.localScale = new Vector3(scale, scale, scale);

                // 改为初始缩放为0
                flower.transform.localScale = Vector3.zero;

                // 启动协程实现缓慢缩放
                StartCoroutine(GrowFlowerOverTime(flower.transform, scale, 1f)); // 0.5秒内完成


                // 随机旋转
                flower.transform.Rotate(0, Random.Range(0, 360), 0);
                //MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

                FlowerDataManager.Instance.AddFlower(roundedPosition, flower, Time.time, false);
            }
        }
    }

    public void GrowFlowersLocally(List<Vector3> paintPath, bool isEnhanced)
    {
        GameObject[] prefabList = (isEnhanced && enhancedFlowerPrefabs.Length > 0)
            ? enhancedFlowerPrefabs
            : flowerPrefabs;


        float grassCheckRadius = 0.5f;
        float heightAdjustment = 0.7f;

        HashSet<Vector2> newFlowerPositions = new HashSet<Vector2>(); // 本次新增花朵位置记录（XZ）

        foreach (Vector3 centerPosition in paintPath)
        {
            Vector3 roundedCenter = RoundPosition(centerPosition);
            string roleID = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
            strokeRecognizor.UpdatePoint(centerPosition, isEnhanced, roleID);

            float clusterSize = Random.Range(minclusterSize, maxclusterSize);
            for (int i = 0; i < clusterSize; i++)
            {
                float angle = Random.Range(0, 360);
                float distance = maxDistanceBetweenFlowers * Mathf.Pow(Random.Range(0f, 1f), 2);
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
                Vector3 flowerPos = centerPosition + offset;

                // 检查是否在草上，调整高度
                //bool isOnGrass = grassPositions.Any(grassPos =>
                //    Vector2.Distance(
                //        new Vector2(flowerPos.x, flowerPos.z),
                //        new Vector2(grassPos.x, grassPos.z)
                //    ) < grassCheckRadius
                //);
//
                //if (isOnGrass)
                //{
                //    flowerPos.y += heightAdjustment;
                //}

                Vector3 roundedPos = RoundPosition(flowerPos);
                Vector2 flowerPos2D = new Vector2(flowerPos.x, flowerPos.z);

                // 高度调整后，检测周围是否已有花
                bool occupiedInManager = FlowerDataManager.Instance.IsPositionOccupied(roundedPos, minDistanceBetweenFlowers);
                bool occupiedInCurrent = newFlowerPositions.Any(p => Vector2.Distance(p, flowerPos2D) < minDistanceBetweenFlowers);

                if (!occupiedInManager && !occupiedInCurrent)
                {
                    GameObject selPrefab = prefabList[Random.Range(0, prefabList.Length)];
                    GameObject flower = Instantiate(
                        selPrefab,
                        flowerPos,
                        Quaternion.Euler(0, Random.Range(0, 360), 0),
                        transform
                    );

                    float targetScale = Random.Range(flowerScaleRange.x, flowerScaleRange.y);
                    flower.transform.localScale = Vector3.zero;
                    StartCoroutine(GrowFlowerOverTime(flower.transform, targetScale, 1f));

                    FlowerDataManager.Instance.AddFlower(roundedPos, flower, Time.time, isEnhanced);
                    newFlowerPositions.Add(flowerPos2D); // 记录新生长花的位置
                }
            }
        }
    }

    public void GrowFlowersLocally(List<Vector3> paintPath, bool isEnhanced, Transform parent = null)
    {
        GameObject[] prefabList = (isEnhanced && enhancedFlowerPrefabs.Length > 0)
            ? enhancedFlowerPrefabs
            : flowerPrefabs;

        float grassCheckRadius = 0.5f;
        float heightAdjustment = 0.7f;

        HashSet<Vector2> newFlowerPositions = new HashSet<Vector2>(); // 本次新增花朵位置记录（XZ）

        foreach (Vector3 centerPosition in paintPath)
        {
            Vector3 roundedCenter = RoundPosition(centerPosition);
            string roleID = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
            strokeRecognizor.UpdatePoint(centerPosition, isEnhanced, roleID);

            float clusterSize = Random.Range(minclusterSize, maxclusterSize);
            for (int i = 0; i < clusterSize; i++)
            {
                float angle = Random.Range(0, 360);
                float distance = maxDistanceBetweenFlowers * Mathf.Pow(Random.Range(0f, 1f), 2);
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
                Vector3 flowerPos = centerPosition + offset;
                Vector3 roundedPos = RoundPosition(flowerPos);
                Vector2 flowerPos2D = new Vector2(flowerPos.x, flowerPos.z);

                // 高度调整后，检测周围是否已有花
                bool occupiedInManager = FlowerDataManager.Instance.IsPositionOccupied(roundedPos, minDistanceBetweenFlowers);
                bool occupiedInCurrent = newFlowerPositions.Any(p => Vector2.Distance(p, flowerPos2D) < minDistanceBetweenFlowers);

                if (!occupiedInManager && !occupiedInCurrent)
                {
                    GameObject selPrefab = prefabList[Random.Range(0, prefabList.Length)];
                    GameObject flower = Instantiate(
                        selPrefab,
                        flowerPos,
                        Quaternion.Euler(0, Random.Range(0, 360), 0),
                        transform
                    );
                    Transform spawnParent = parent ?? this.transform;
                    float parentScaleX = spawnParent.lossyScale.x; 
                    float targetScale = Random.Range(flowerScaleRange.x, flowerScaleRange.y);
                    float localScaleFactor = targetScale / parentScaleX;
                    flower.transform.localScale = Vector3.zero;
                    StartCoroutine(GrowFlowerOverTime(flower.transform, localScaleFactor, 1f));

                    flower.transform.SetParent(spawnParent, true);  

                    FlowerDataManager.Instance.AddFlower(roundedPos, flower, Time.time, isEnhanced);
                    newFlowerPositions.Add(flowerPos2D); // 记录新生长花的位置
                }
            }
        }
    }



    private Vector3 RoundPosition(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x * 100) / 100,
            Mathf.Round(position.y * 100) / 100,
            Mathf.Round(position.z * 100) / 100
        );
    }


    public void ClearFlowers()
    {
        FlowerDataManager.Instance.ClearAllFlowers();
    }
}